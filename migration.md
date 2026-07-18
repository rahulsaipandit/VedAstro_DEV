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

### Phase 1+2 (combined) — Decouple `Library` and move API off Azure Functions, per endpoint

Rather than fully splitting `Library` before touching the API host, migrate
one `API/FrontDesk/*API.cs` endpoint at a time, doing both steps together
for each:
- **Data access**: for the tables that endpoint touches, replace
  `AzureTable.cs` / `AzureCache.cs` calls with an interface (e.g.
  `IPersonRepository`, `IChartImageCache`) backed by a Postgres/EF Core (or
  Dapper) implementation. No Azure implementation kept behind the
  interface — going straight to Postgres since the target is a clean
  self-hosted setup, not a staged dual-backend rollout.
- **Host**: move that endpoint's `[Function("...")]` method into an
  ASP.NET Core minimal API endpoint or controller action (swap
  `[Function]`/`HttpRequestData` for `[HttpGet]`/`HttpContext` or minimal
  API lambdas).
- Replace chart-image blob caching with local disk storage.
- Replace `Azure.AI.OpenAI` client with the plain OpenAI SDK (or whichever
  provider is used) — likely a small change since `Azure.AI.OpenAI` is
  already just a thin wrapper.
- Replace `Azure.Communication.Email` with a standard SMTP client or a
  non-Azure transactional email provider.
- Drop Azurite entirely — local dev now points at a local Postgres
  instance (native install on the self-hosted machine), no emulator
  needed.

As endpoints migrate, the remaining pure-calculation code in `Library`
naturally ends up Azure-free (the "Core" split falls out of this process
rather than being done as an upfront separate pass).

### Status: Phase 1+2 — done and verified

- **`Data/VedAstro.Data.csproj`**: `AppDbContext` (Npgsql) with 28 tables via a
  generic `ConfigureKeyedTable<T>` helper (composite `partition_key`/`row_key`
  columns, preserving Azure Table Storage's key shape exactly). Generic
  `IKeyedRepository<T>`/`EfKeyedRepository<T>` (backed by
  `IDbContextFactory<AppDbContext>` — a fresh short-lived context per
  operation, not a shared instance, since `DbContext` isn't thread-safe and a
  shared one gets corrupted after any failed save) plus ~28 named repository
  interfaces. `IChartImageCache`/`LocalDiskChartImageCache` replaces
  `AzureCache.cs`'s blob operations. Three migrations applied to the real
  local Postgres 18 instance: `InitialCreate` (18 core tables),
  `AddGeoLocationCacheTables` (7 tables), `AddMatchMLDatasetTables` (3
  tables).
- **`Library`**: Fully decoupled from Azure (`Azure.Data.Tables`,
  `Azure.Storage.Blobs`, `Azure.AI.OpenAI`,
  `Microsoft.Azure.Functions.Worker.Core` all removed). Static classes
  (`CallTracker`, `Tools`, `ApiStatistic`, `UserData`, `LocationManager`) go
  through a `Repositories` static locator (`Library/Logic/Repositories.cs`)
  instead. `LocationManager.cs`'s geolocation cache tier is now fully wired
  to Postgres (not stubbed). `ChatAPI.cs` had its dead Azure OpenAI helpers
  removed and its `LOCAL_LLM_BASE_URL` routing no longer requires a Debug
  build, so Chat is testable against a local LM Studio-style server.
- **`API`**: Converted from Azure Functions Worker (net7) to ASP.NET Core
  minimal API (net8), all 9 `FrontDesk/*.cs` files ported with identical
  routes/verbs. `Program.cs` is `WebApplicationFactory`-testable.
- **Testing (no browser needed)**: `Data/VedAstro.Data.Tests` —
  159/159 passing against a real Testcontainers Postgres.
  `API/API.IntegrationTests` — 17/17 passing, 3 skipped (Chat tests, unless
  LM Studio is running). Documented in `CLAUDE.md`.
- **`MatchMLPipeline`** (offline ML tooling, not part of the live
  API/Website): also migrated off Azure Table Storage onto the same
  repository pattern, since it shared entities with the API.

### Known remaining items (not silently dropped)

- **`LibraryTests`** — left broken by explicit decision: ~30+ tests
  reference astrology calculation methods (Chara Dasa, several Ashtakavarga
  yogas, eclipses, Ishta/Kashta scores, etc.) that don't exist in `Library`,
  tracing back to old abandoned "WIP" commits. Unrelated to this migration;
  implementing them would mean fabricating unverified domain math.
- **Chat message history** (`ChatMessage`/`PresetQuestionEmbeddings`
  persistence in `ChatAPI.cs`) stays an intentional no-op stub — out of
  scope per an earlier decision. `HoroscopeFollowUpChat`/
  `HoroscopeChatFeedback` can't fully succeed even with LM Studio running.
- **`PersonShareList`** ported read-only (matches production — no write
  path exists anywhere in the codebase today).
- **Email** (`APITools.SendEmail`) stays a no-op/console-log stub — never
  had real SMTP wiring, by decision.
- **`Website_Mobile`** — untouched, explicitly out of scope.
- **`MigrateGeoLocationData`** — given a minimal direct `Azure.Data.Tables`
  reference to keep compiling; not migrated to Postgres (a one-off,
  already-run data-cleanup script, low priority).
- Nothing on this branch has been committed to git yet.

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

## Decisions

- **Database choice**: Postgres.
- **Object storage**: local disk (chart image cache).
- **Hosting target**: self-hosted on local computer.
- **Migration order for Phase 1 vs 2**: done together, per API endpoint —
  migrate one endpoint plus its data access at a time, rather than
  splitting `Library` fully first before touching the API host.
