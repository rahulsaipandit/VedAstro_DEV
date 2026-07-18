# Migration Plan: Off Azure / Azurite / Blazor

## Goal

Remove the Azure dependency entirely (Functions, Table Storage, Blob Storage,
Azurite) and replace the Blazor front end with a mainstream JS framework,
while keeping the ASP.NET Core backend and reusing the existing astrology
calculation engine.

## Target architecture

```
React Native (Expo) + TypeScript — web (react-native-web), iOS, Android
from one codebase
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

- **`LibraryTests`** — now compiles (0 errors, down from 112). The ~30
  missing astrology methods it referenced (Chara Dasa, 7 Ashtakavarga yogas,
  eclipses, Ishta/Kashta scores, Fortuna/Destiny points, Tajika date, Pancha
  Pakshi, etc.) traced back to old abandoned "WIP" commits, unrelated to this
  migration - each was implemented best-effort in `Library/Logic/Calculate/CalculateJaimini.cs`
  and `Library/Logic/Calculate/CoreMiscExtra.cs` (plus 7 Ashtakavarga yogas in
  `CalculateHoroscope.cs`), every one documented inline as "missing prior to
  this fix, best-effort, verify against a second source".

  Also found and fixed a real, pre-existing, broadly-impactful bug this
  effort surfaced: `Calculate.LongitudeToLMTOffset` (`Library/Logic/Calculate/CoreTime.cs`)
  computed `TimeSpan.FromHours(longitudeDeg / 15.0)`, which is fractional to
  the sub-second for almost any real-world longitude (e.g. Bangalore's
  77.5946° -> 5h 10m 22.69s) - every caller feeds that straight into
  `DateTimeOffset.ToOffset(...)`/its constructor (`Time.StdToLmt`,
  `CoreTime.LmtToStd`, etc.), which requires whole-minute offsets, so this
  threw `"Offset must be specified in whole minutes"` for essentially any
  birth location, not a test-data edge case (`GeoLocation.Bangalore`, the
  fixture nearly every test in this project already used, triggers it too).
  It went unnoticed because (a) `LibraryTests` never compiled, so its tests
  never ran, and (b) in the live API, `OpenAPI.cs`'s reflection dispatcher
  catches all exceptions into a generic `Fail` JSON envelope
  (`APITools.FailMessageJson`), so any endpoint touching `SunriseTime`
  (Muhurtha timing, `BirthYama`, `TajikaDateForYear`, etc.) for a non-"nice"
  longitude would just silently fail rather than crash visibly. Fixed by
  rounding the offset to the nearest whole minute at the source (the
  sub-minute precision was never actually observable anyway - LMT is only
  ever displayed via the `"zzz"` format, hh:mm, no seconds).

  `dotnet test` after both fixes: **52/85 passing** (up from 46/85 pre-LMT-fix).
  Of the 33 remaining failures:
  - 6 were unblocked by the LMT fix but still fail on their own logic/values
    once actually exercised for the first time (`LMTToSTDTest`,
    `SunriseTimeTest` - pre-existing methods; `SunAshtakavargaYoga3Test`,
    `AbstractActivityTest`, `MainActivityTest` - new best-effort logic/output
    formatting needs work, e.g. `AbstractActivity`/`MainActivity` return the
    full `BirdActivity` enum name ("Eating") where the test expects a
    classical single-letter Pancha Pakshi code ("O"); `TajikaDateForYearTest`
    - the test's own expected value is the literal placeholder string `"xx"`,
      never finished, not a real assertion).
  - The remaining ~27 are a mix of newly-implemented-method domain-math gaps
    and pre-existing methods/tests that simply never ran before this pass
    (the file never compiled) - not chased further this session, flagged
    here rather than silently left broken.
- **Chat message history** (`ChatMessage`/`PresetQuestionEmbeddings`) - fixed:
  real Postgres persistence via `IChatMessageRepository`/
  `IPresetQuestionEmbeddingsRepository` (`Data/Entities`, `Data/Repositories/NamedRepositories.cs`,
  migration `AddChatTables`), replacing the old in-memory `DisabledTableClient`
  no-op stub in `ChatAPI.cs`. Also fixed a live `NullReferenceException` in
  `HoroscopeChatFeedback`/`SendMessageHoroscopeFollowUp` when no matching
  record exists (now replies gracefully instead of crashing).
- **`PersonShareList`** ported read-only (matches production — no write
  path exists anywhere in the codebase today).
- **Email** (`APITools.SendEmail`) stays a no-op/console-log stub — never
  had real SMTP wiring, by decision.
- **`Website_Mobile`** — untouched, explicitly out of scope.
- **`MigrateGeoLocationData`** — fixed: `ProgramCleanPersonList.cs` no longer
  references `Azure.Data.Tables` directly. It now goes through the same
  `IDbContextFactory<AppDbContext>` / `PersonRepository` (`Data/Repositories`)
  pattern as the rest of the app, reading `ConnectionStrings:Postgres` from an
  optional `appsettings.json` (gitignored, not committed - same treatment as
  `API/appsettings.Development.json`) or environment variables. `Program.cs`
  and `ProgramTimezone.cs` remain fully commented out (dead already-run
  scripts, untouched).
- Nothing on this branch has been committed to git yet.

### Phase 3 — Replace Blazor WASM with React Native (Expo) + TypeScript

**Scope, confirmed by a full component survey** (see below) — 128 `.razor`
files total: 70 in `Website/` (65 routed pages + layout/router shell), 58 in
`ViewComponents/` (54 shared widgets/chart components + 3 page-level
components + imports). All routing goes through one file,
`ViewComponents\Code\Managers\PageRoute.cs` (route-string constants, no
`@page` directives anywhere), giving a clean 1:1 source for the new route
table. API access is not scattered inline — a real client layer already
exists (`VedAstroAPI` facade with `.Match`/`.Person`/`.EventsChart`
sub-clients, plus `WebsiteTools.cs` helpers and `URL`/`PageRoute` constant
classes) that the new TS client can mirror method-for-method. Global state
today is one static class, `AppData` (`ViewComponents\Code\Objects\AppData.cs`),
with `localStorage`-backed persistence via a custom JS-interop
`Interop.getProperty`/`setProperty` wrapper (this is what backs the
`DebugMode` local-API toggle from `CLAUDE.md`/`Localhost_Setup.md`).

**Why React Native (Expo), not plain React + Vite** — decided over the
original plan: the goal is one codebase serving mobile web *and* native
iOS/Android, not a web-only rewrite. This has real consequences beyond
"different component library":
- No CSS at all — RN uses `StyleSheet.create` objects, not Bootstrap,
  Tailwind, or CSS modules. Every ported component is redesigned in a
  mobile-friendly responsive style using RN's layout primitives
  (`View`/`Text`/`Pressable`/Flexbox), not a straight markup port.
- Several JS-interop pieces found in the survey have no RN equivalent and
  need cross-platform library replacements, not native-only forks:
  SweetAlert2 (`Swal.fire`/`.close`/`.showLoading`, ~heaviest use in
  `EventsChartViewer.razor`) → a cross-platform modal/toast library;
  tippy.js tooltips → a custom `Pressable` + popover component; the
  Google/Facebook sign-in JS glue in `SignInButton.razor` (7 interop calls)
  → `expo-auth-session` (or equivalent) so the same code path runs on
  web/iOS/Android.
- Two chart components need **no interop work at all** — `IndianChart` and
  `SkyChartViewer` just point an `<img>` (→ RN `<Image>`) at a
  server-rendered chart URL. The real interop lift is `StrengthChart`
  (`JS.DrawPlanetStrengthChart`/`DrawHouseStrengthChart`, hand-rolled
  canvas/SVG JS, no charting library) and `EventsChartViewer`.
- The local-API-endpoint toggle (`DebugMode`) must keep working across all
  three targets — replace the `localStorage`-via-JS-interop mechanism with
  `expo-secure-store`/`AsyncStorage` behind one storage interface so the
  toggle behaves identically on web and native.

**Porting order**: highest-traffic first — chart generation
(`Calculator/Horoscope`, `FamilyChart`, `Match/*`) and sign-in/account flows
before lower-traffic pages (Blog, legal pages, Donate, FAQ). Matches the
original plan; confirmed rather than revisited.

**State management**: Zustand — replaces the static `AppData` class (current
user, cached person list, dark mode, last-used location, etc.) with a real
store; storage-backed slices (person cache, debug-mode toggle) persist via
the cross-platform storage interface above instead of the old
`Interop.getProperty`/`setProperty` calls.

**API standardization**: the API currently has two parallel serialization
conventions — legacy XML (`ServerManager.WriteToServerXmlReply`) and newer
JSON (`Tools.WriteServer`/`Tools.ReadServerRaw`). Before/during the port,
add JSON equivalents for any endpoint still XML-only so the new TS client
speaks JSON exclusively — no XML parsing carried into the new frontend.

**Cutover strategy**: run old (Blazor) and new (React Native Web) frontends
side by side during migration, routed by path, so the cutover isn't
all-or-nothing.

### Status: Phase 3 — in progress

New project `WebsiteNative/` (Expo SDK 57, expo-router, TypeScript, sibling to
`Website/` — old and new frontends run side by side per the cutover strategy
above). Structure: `src/app/` (routed screens, expo-router file-based —
folder path matches `PageRoute.cs` strings, e.g. `Account/Login.tsx` ↔
`PageRoute.Login = "Account/Login"`), `src/components/`, `src/constants/`
(`routes.ts`/`urls.ts` mirroring `PageRoute.cs`/`URL.cs`), `src/store/`
(Zustand), `src/lib/` (`api/`, `auth/`, `firebase/`).

**Home page** (`Website/Pages/Index.razor` → `src/app/index.tsx`) — hero
banner + shuffled quick-links card grid, rebuilt with RN `View`/`Pressable`/
`StyleSheet` (no CSS/Bootstrap), real images copied from `wwwroot`.

**Login/Account** (`Account/Login.razor` → `src/app/Account/Login.tsx`,
`SignInButton.razor` → `src/components/SignInButton.tsx`, `InfoBox.razor` →
`src/components/InfoBox.tsx`):
- **Auth architecture** ended up different from (and more involved than) the
  original Phase 3 plan's "expo-auth-session for sign-in" note, per a
  decision made mid-implementation: `expo-auth-session` still drives the
  Google/Facebook OAuth popup/redirect (`src/lib/auth/useGoogleSignIn.ts`/
  `useFacebookSignIn.ts`, generic `AuthRequest` primitives since the
  built-in provider helpers are deprecated), but the resulting raw
  token is then exchanged for a **Firebase** user via
  `signInWithCredential` (`src/lib/firebase/firebaseSignIn.ts`), and it's
  *that* Firebase ID token that gets verified server-side — not the raw
  Google/Facebook token directly.
- **API**: new `/api/SignInFirebase/Token/{token}` endpoint
  (`API/FrontDesk/SignInAPI.cs`), verified via the `FirebaseAdmin` NuGet
  package (`FirebaseAuth.VerifyIdTokenAsync`), writing to the same
  `UserData` table via the existing `AddOrUpdateUserData`. `Program.cs`
  initializes `FirebaseApp` from a service-account key path at startup,
  degrading gracefully (console warning, not a crash) if unconfigured.
- The old Blazor site's `/api/SignInGoogle`/`/api/SignInFacebook` endpoints
  (direct Google/Facebook token verification, already JSON) are **left
  alone, not dead code** — `ViewComponents/Components/SignInButton.razor`
  still calls them directly and continues to work unchanged.

**Guest mode** — sign-in is optional everywhere, not gated per-page:
- `useAppStore`'s guest ID is the real sentinel `"101"` shared with the
  existing backend (`Library/Data/UserData.cs`'s `UserData.Guest`,
  `PersonAPI.cs`'s ownerId-swap logic, `MatchAPI.cs`'s `"101"` userId) —
  parity with the system, not a new local convention.
- `visitorId` (per-device anonymous ID, persisted) +
  `effectiveOwnerId()` (visitor ID when guest, real ID when signed in)
  mirror `PersonTools.cs`'s `ownerId = UserId == "101" ? VisitorID : UserId`
  exactly, so a guest's person list is private to their device and
  migrates onto their account automatically on login (server-side
  `SwapUserId`, unchanged).
- Login screen has an explicit "Continue as Guest" link.
- **Bug found and fixed along the way**: generating the visitor ID inside a
  Zustand selector (called during render) broke `expo export`'s static
  prerendering — the persisted store's `set()` reaches for
  `window`/`localStorage` in the Node prerender environment, where neither
  exists. Fixed by making `effectiveOwnerId()` a pure read and moving
  visitor-ID creation into `_layout.tsx`'s mount `useEffect` instead. Worth
  remembering as a general rule for the rest of the port: **no store
  mutations during render**, only in effects/handlers.

**Match** (`Calculator/Match/Index.razor` → `src/app/Match/index.tsx`,
`PersonSelectorBox.razor` → `src/components/PersonSelector.tsx`,
`Match/Report.razor` → `src/app/Match/Report/[maleId]/[femaleId].tsx`):
- `PersonSelector` is a real port (own list + public/example list from
  `GetPersonList`/`OwnerId/101`, search, selection) — not a stub.
- Match index: two person selectors, Calculate button with the same
  validation chain as the original (missing selection, same-person
  confirm, reversed-gender confirm — via a small `confirm()` promise
  wrapper around RN `Alert`, replacing the old SweetAlert2 confirm calls
  for *this* flow only, see dead/deferred code below), InfoBox links,
  static article text.
- **Backend gap found and closed just enough to make this real**: the old
  Blazor site's one-on-one match report had *no* ported backend endpoint —
  Phase 1+2 only carried over `FindMatch` (global search), not
  `GetMatchReport` (direct two-person report). Added
  `/api/GetMatchReport/MaleId/{id}/FemaleId/{id}` to `MatchAPI.cs`
  (live-computed via the existing `MatchReportFactory`, JSON, not
  persisted), covered by a new integration test.
- `MatchReportListViewer.razor` (the "saved reports" example list on the
  old Match/Index page) was **not** ported — `GetMatchReportList`/
  `SaveMatchReport` (XML-only) were never carried into this ASP.NET Core
  API in Phase 1+2 and have no Postgres persistence backing them at all;
  porting the viewer meaningfully would mean designing and building that
  persistence from scratch, which is out of scope for a page port. Flagged
  as an open question below, not silently dropped.

**Verification methodology used throughout** (same bar as Phase 1+2): `tsc
--noEmit` after every change, `npx expo export --platform web` to confirm
actual bundling/static-rendering (not just typechecking) of every new
route, `dotnet build`/`dotnet test` for every API-side change, all API
integration tests re-run full-suite before considering a slice done.

### Dead/unreachable/deferred code left alone during Phase 3 so far

- **Icon system**: the old app uses Iconify (`data-icon="..."` spans) for
  essentially every icon (`Icon`/`IconButton`/`IconTitle`/`InfoBox`/
  `PersonSelectorBox`, etc.). No RN icon library has been chosen yet —
  `InfoBox`/`PersonSelector` ports so far are text-only, icons dropped
  rather than faked. Needs a decision (`@expo/vector-icons` is the
  Expo-bundled default candidate) before porting most other pages, since
  nearly every remaining component uses one.
- **SweetAlert2/tippy.js general replacement**: migration.md's Phase 3 plan
  called for picking one cross-platform library per concern up front. That
  hasn't happened yet — only a narrow, local `confirm()` helper
  (`src/lib/confirm.ts`, wraps RN `Alert`) exists, built for Match's
  same-person/reversed-gender confirms specifically. `EventsChartViewer`'s
  heavier SweetAlert2 usage (calendar export, SVG export, clipboard,
  bookmarks — see the original Phase 3 survey) is untouched.
- **AddPerson / Person.Editor flow**: not ported. `PersonSelector`'s
  "Add New Person" button shows an honest "coming soon" alert instead of
  navigating anywhere. This caps how useful Match/Horoscope/etc. actually
  are right now — a guest or user can only pick from example people until
  this exists.
- **`Match/Finder`, `Match/SavedReports`, `Match/Profile`**: not ported
  (only `Match/Index` and a minimal `Match/Report`). Links to
  `PageRoute.MatchFinder` from both the Home page and Match/Index exist
  but currently 404 in `WebsiteNative` — expected/incremental, same as any
  not-yet-ported route linked from an already-ported page.
- Old Blazor endpoints/components (`SignInGoogle`/`SignInFacebook` API
  routes, `SignInButton.razor` itself, `MatchReportListViewer.razor`, the
  XML `WebsiteTools.GetSavedMatchList`) are **not dead** — the Blazor site
  still runs and still uses them. Nothing on the API side was removed or
  changed in a way that could break it; only additive endpoints
  (`SignInFirebase`, `GetMatchReport`) were introduced.

### Open questions for the rest of Phase 3

- **Icon library choice** — blocks a clean port of most remaining
  components (see above).
- **Firebase service-account key** — `/api/SignInFirebase` degrades
  gracefully without one, but sign-in can't actually complete end-to-end
  until a real key is generated (Firebase Console → Project Settings →
  Service Accounts) and placed at `API/firebase-service-account.json`
  (gitignored).
- **OAuth redirect URI registration** — the Google/Facebook app
  registrations reused from the Blazor site only allow `vedastro.org`'s
  origin today; `WebsiteNative`'s dev/native origins need adding in each
  console before sign-in completes for real, separately from the Firebase
  key above.
- **Saved match reports** — no decision yet on whether to design real
  Postgres persistence for `GetMatchReportList`/`SaveMatchReport` (a
  genuinely new feature relative to what Phase 1+2 ported) or leave
  "saved reports" as a feature the new app simply doesn't have.
- **SweetAlert2/tippy.js replacement libraries** — still an open pick, not
  yet needed broadly since so few components are ported, but will block
  `EventsChartViewer` and any page with tooltips once those come up.

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
- **Phase 3 frontend framework**: React Native (Expo) + TypeScript, not
  plain React — one codebase for mobile web (via react-native-web) and
  native iOS/Android, redesigned mobile-friendly/responsive rather than
  ported as-is. No Bootstrap/Tailwind/CSS modules; styling is RN's
  `StyleSheet` API.
- **Phase 3 API serialization**: standardize on JSON — add JSON endpoints
  for anything still XML-only, so the new TS client never parses XML.
- **Phase 3 page-porting order**: highest-traffic first (chart generation,
  sign-in/account, Match), lower-traffic pages (Blog, legal, Donate) last.
- **Phase 3 state management**: Zustand, replacing the static `AppData`
  class.
- **Phase 3 native-only JS interop replacements** (SweetAlert2, tippy.js,
  Google/Facebook sign-in): cross-platform RN libraries picked once per
  concern (e.g. `expo-auth-session` for sign-in), not per-platform forks.
