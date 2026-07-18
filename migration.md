# Migration Plan: Off Azure / Azurite / Blazor

## Goal

Remove the Azure dependency entirely (Functions, Table Storage, Blob Storage,
Azurite) and replace the Blazor front end with a mainstream JS framework,
while keeping the ASP.NET Core backend and reusing the existing astrology
calculation engine.

## Target architecture

```
React + TypeScript (Vite)
        │  REST/JSON over HTTP
ASP.NET Core Web API (Kestrel, self-hosted or containerized)
        │
   Data access layer (interface-based)
        │
Non-Azure database (Postgres recommended) + local disk/S3-compatible
object storage for chart image cache
```

No Azurite layer — local dev runs against a real local Postgres instance
(or SQLite for the lightest possible dev loop) instead of an emulator.

## Current-state findings (informs the plan below)

- **API**: Azure Functions v4, isolated worker model, **HTTP-trigger only**
  (19 triggers across 8 files in `API/FrontDesk/` — `GeneralAPI`, `OpenAPI`,
  `BirthTimeFinderAPI`, `EventsChartAPI`, `MatchAPI`, `SignInAPI`,
  `SkyChartAPI`, `SubscriptionAPI`, `WebsiteLoggerAPI`). No Timer/Blob/Queue
  triggers. This is the easy part: each `[Function("...")]` method is
  already a thin HTTP handler and maps 1:1 onto an ASP.NET Core minimal API
  endpoint or controller action.
- **Storage**: Azure Table Storage (`Azure.Data.Tables`) for structured data
  — tables `PersonList`, `SubscriberCallRecords`, `AnonymousIpCallRecords`,
  `UserDataList`, `LifeEventList`, `OpenAPIErrorBook`, `CallTracker`,
  `WebsiteErrorLog`, `WebsiteDebugLog`, `CallInfoStatistic`,
  `PersonShareList` — plus Azure Blob Storage for cached chart images
  (`Library/Logic/AzureCache.cs`). No queues in use.
- **Website**: Blazor **WebAssembly**, not Blazor Server — confirmed no
  SignalR/`HubConnection` usage. This is good news: there's no server-side
  render loop or persistent connection to replace, "just" a component
  rewrite from Razor to JSX, already talking to the API over plain HTTP.
  ~65 `.razor` files in `Website/`, ~59 more in the `ViewComponents`
  project — roughly 120 components total to port.
- **Library**: Holds the actual calculation engine (planetary positions via
  SwissEph, dasa/yoga/kuta logic, chart rendering) but is **not** currently
  Azure-free — it directly references `Azure.Data.Tables`,
  `Azure.Storage.Blobs`, `Azure.AI.OpenAI`, and
  `Microsoft.Azure.Functions.Worker.Core`, and contains data-access code
  (`AzureTable.cs`, `AzureCache.cs`) alongside pure calculation logic. This
  needs to be split before it can be called "framework-agnostic core."
- **Other Azure surface**: no Application Insights, no Key Vault. Just
  Table/Blob storage, Functions Worker SDK, `Azure.AI.OpenAI` (chat), and
  `Azure.Communication.Email` (transactional email) — a small, well-bounded
  set of things to replace.

## Phased plan

### Phase 1 — Decouple `Library` from Azure

Split `Library/` into:
- **Core** (pure C#, no Azure refs): calculation engine, chart rendering,
  reference data. This is reused unchanged in the new architecture.
- **Data access**: replace `AzureTable.cs` / `AzureCache.cs` with an
  interface (e.g. `IPersonRepository`, `IChartImageCache`) so the storage
  backend becomes swappable. Keep Azure implementations temporarily behind
  the interface if useful for a staged rollout; add Postgres/EF Core (or
  Dapper) implementations alongside.

This phase can happen **before** touching the frontend or Functions host,
and de-risks everything downstream.

### Phase 2 — Move API off Azure Functions onto ASP.NET Core

- Because every trigger is HTTP-only, each `API/FrontDesk/*API.cs` class's
  `[Function("...")]` methods can be moved into ASP.NET Core minimal API
  endpoints or controllers largely mechanically (swap
  `[Function]`/`HttpRequestData` for `[HttpGet]`/`HttpContext` or minimal
  API lambdas).
- Swap `Azure.Data.Tables` calls for the new repository interfaces from
  Phase 1.
- Replace chart-image blob caching with local disk or an S3-compatible
  store (e.g. MinIO locally, any S3-compatible bucket in prod).
- Replace `Azure.AI.OpenAI` client with the plain OpenAI SDK (or whichever
  provider is used) — likely a small change since `Azure.AI.OpenAI` is
  already just a thin wrapper.
- Replace `Azure.Communication.Email` with a standard SMTP client or a
  non-Azure transactional email provider.
- Drop Azurite entirely — local dev now points at a local Postgres
  instance (`docker run postgres` or native install), no emulator needed.

### Phase 3 — Replace Blazor WASM with React + TypeScript

- Since the Website is already WASM calling the API over HTTP (no
  server-side Blazor plumbing), this is a contained frontend rewrite: pick
  React + TypeScript + Vite, port the ~120 Razor components page by page,
  starting with the highest-traffic pages (chart generation, sign-in) and
  leaving lower-traffic pages for last.
- Consider running old (Blazor) and new (React) frontends side by side
  during migration, routed by path, so the cutover isn't all-or-nothing.

### Phase 4 — Cutover and cleanup

- Point DNS/hosting at the new ASP.NET Core + React stack.
- Remove Blazor project, Azure SDK package references, Azurite setup docs,
  and any remaining Azure Functions scaffolding once nothing depends on
  them.
- Update `CLAUDE.md` local-dev instructions to describe the new (non-Azure)
  setup.

## Open questions to settle before starting

- **Database choice**: Postgres is the default recommendation (mature EF
  Core support, good fit for the relational-ish data currently modeled as
  Table Storage entities). SQLite is an option if minimizing local-dev
  setup matters more than production parity.
- **Object storage**: local disk is simplest for a single-server deploy;
  S3-compatible storage (MinIO locally, real S3/R2 in prod) if multi-instance
  or cloud deployment is expected.
- **Hosting target**: self-hosted VM/container vs. another cloud's PaaS —
  affects how much of Phase 2's swap-in code needs to be
  provider-agnostic vs. tailored to one platform.
- **Migration order for Phase 1 vs 2**: could also do them together per
  API endpoint (migrate one endpoint + its data access at a time) instead
  of splitting Library fully first — worth deciding based on how much
  parallel work is wanted.
