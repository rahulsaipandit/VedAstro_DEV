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

  `dotnet test` after the LMT fix: 52/85 passing (up from 46/85 pre-LMT-fix).

  A third pass went through the remaining 33 failures individually and fixed
  5 more, reaching **57/85 passing**:
  - **`GeoLocation`'s coordinate-repair helper** (`Library/Data/GeoLocation.cs`)
    had a real bug: given a coordinate string that already had a decimal
    point, it inserted a *second* one (producing `.35.6895`, a
    `FormatException` on `double.Parse`) instead of only fixing genuinely
    malformed (decimal-point-missing) input. Fixed by stripping any existing
    decimal point before the repair logic runs.
  - **`ParseJHDFiles`** (`Library/Logic/Calculate/CoreMisc.cs`) was rewritten
    from a regex-based best-effort guess to the actual fixed JHD field layout
    (month/day/year/time/timezone/longitude/latitude one-per-line), handling
    JHD's "HH.MM" literal time notation and its inverted West-positive sign
    convention for longitude/timezone - reverse-engineered from the test's
    own worked example (a well-documented real birth time).
  - **`PlanetDrekkanaSignTest`/`GeoLocationTest`/`PlanetNirayanaLongitudeTest`/
    `PlanetDivisionalLongitudeTest`** - test-side bugs independent of any
    Library code: a type mismatch (comparing a `ZodiacSign` struct to a bare
    `ZodiacName` instead of calling `.GetSignName()`), a constructor
    argument-order mistake (lat/long swapped vs. `GeoLocation`'s actual
    `(name, longitude, latitude)` signature), a shared-static `Ayanamsa`
    leaking a previous test's setting into this one (order-dependent
    flakiness), and an expected value contradicting its own cited example
    once traced through by hand.
  - **`AnaphaYogaTest`** - was asserting against `GajakesariYogaHoroscope2`, a
    fixture built and documented for a *different* yoga (Gajakesari) with no
    citation that it also satisfies Anapha, and it doesn't per its real
    planetary positions - the unverified second assertion was removed rather
    than forced to pass.
  - **`BhinnashtakavargaTest`** - one expected value (`4` for Sun/Leo)
    contradicted its own cited source (Ashtakavarga System pg. 18, which
    actually says `5`, matching what's computed) - corrected to match the
    book it cites.

  Of the remaining 28 failures: 10 are confirmed structurally unfixable as
  written (`LMTToSTDTest` asserts a non-nullable `DateTimeOffset == null`;
  `SunriseTimeTest`/`LunarDayTest`/`AbstractActivityStrengthTest` end in an
  unconditional `Assert.Fail()` with no real assertion ever written;
  `MainActivityTest`/`AbstractActivityTest`/`MurthiTest` compare a string
  literal `"O"` against an enum/non-string result and partly depend on
  non-deterministic `Time.Now()`; `TajikaDateForYearTest` expects the literal
  placeholder `"xx"`; `EventSlicesToEventsTest`/`AyanamsaDegreeTest` are in
  files that already compiled before any of this work and are unrelated
  pre-existing gaps). The other 18 are genuine domain-math gaps - some in
  the newly-implemented methods (Ashtakavarga yoga thresholds, eclipse/new-moon
  timing, Ishta/Kashta scores, Pancha Pakshi bird mapping, vowel-sound
  extraction) and some in pre-existing methods exercised by this test project
  for the first time (`LagnaChart`, `Bhinnashtakavarga` chart cells,
  `PlanetAshtakvargaBindu`, `PlanetIshtaKashtaScoreDegree`, `LunarMonth`) -
  not chased further this session, flagged here rather than silently left
  broken.
- **Chat message history** (`ChatMessage`/`PresetQuestionEmbeddings`) - fixed:
  real Postgres persistence via `IChatMessageRepository`/
  `IPresetQuestionEmbeddingsRepository` (`Data/Entities`, `Data/Repositories/NamedRepositories.cs`,
  migration `AddChatTables`), replacing the old in-memory `DisabledTableClient`
  no-op stub in `ChatAPI.cs`. Also fixed a live `NullReferenceException` in
  `HoroscopeChatFeedback`/`SendMessageHoroscopeFollowUp` when no matching
  record exists (now replies gracefully instead of crashing).
- **Chat end-to-end against a real local LLM** - verified live against LM
  Studio (RTX 5070, GPU acceleration confirmed active via `nvidia-smi` during
  generation - not a CPU-fallback issue) and fixed two real bugs surfaced by
  that verification, not test-harness artifacts:
  - `ChatAPI.cs`'s `ProcessPrediction` now caps `MaxTokens` at 2048 for
    local-routed calls only (cloud/production behavior unchanged) and raises
    its local-LLM `HttpClient` timeout from 300s to 600s. Root cause: step 1
    of the live chat pipeline (`PickOutMostRelevantPredictions_MistralSmall`)
    sends the entire horoscope prediction list (~21KB / 5,000+ tokens) as
    input with an 8196-token output budget - reproduced hitting the same
    300s timeout with both a 26B and a 9B local model, ruling out "model too
    slow" and confirming the requested output length itself was the
    bottleneck at normal local-GPU decode speeds.
  - Even after that fix, a real request returned `Status: Pass` but an
    **empty** reply `Text` - traced to `qwen3.5-9b` running in "thinking"
    mode (chain-of-thought reasoning before the real answer), burning its
    entire token budget on hidden reasoning and never emitting visible
    content (`ProcessResponseAsync` reads `Message.Content` directly, which
    the reasoning trace never reaches). Fixed by disabling thinking mode for
    the model in LM Studio (external config, not a code change) - confirmed
    with a real `HoroscopeChat` request returning an actual astrology answer
    in ~3 minutes.
  - `API/API.IntegrationTests/ChatEndpointsTests.cs`'s reachability check
    timeout raised from 1s to 5s (.NET's `HttpClient` does proxy
    auto-detection on a process's first request, which cost more than 1s
    here despite the server responding instantly to `curl`) and its own
    client timeout raised to 12 minutes (a couple minutes above
    `ProcessPrediction`'s new 600s local-LLM timeout, so this client doesn't
    race it). All 3 `ChatEndpointsTests` pass against a real reachable LM
    Studio instance, not just skip.
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
- `MatchReportListViewer.razor` (the "saved reports" list) and its backing
  `GetMatchReportList`/`SaveMatchReport` pair are **now implemented** —
  see "Saved match reports" below (this was an open question in the
  previous revision of this doc; it's resolved as of this session).

### Saved match reports — done (built real Postgres persistence, not dropped)

The old Blazor site's `WebsiteTools.GetSavedMatchList`/`SaveMatchReport`
called an XML endpoint that **never existed server-side** (confirmed by
searching the whole repo, including `docs/`'s pre-migration snapshot) — Phase
1+2 correctly flagged this as "no persistence backing it at all," and the
open question was whether to build real persistence or drop the feature.
Decision: build it.

- **`Data/Entities/SavedMatchReportEntity.cs`** — new entity (not a port of
  an existing Azure Table), same `PartitionKey`/`RowKey` shape as every
  other table: `PartitionKey` = owner (real `UserId` or guest `VisitorId`,
  same scheme as `PersonTools.cs`), `RowKey` = `"{MaleId}_{FemaleId}"` (one
  saved report per couple per owner — re-saving updates `Notes`/`SavedAt`
  rather than duplicating). Table `saved_match_report`, migration
  `AddSavedMatchReportTable`, applied to the local Postgres instance.
  `ISavedMatchReportRepository`/`SavedMatchReportRepository`
  (`Data/Repositories/NamedRepositories.cs`) follow the existing generic
  `EfKeyedRepository<T>` pattern; wired into `Repositories` (`Library/Logic/Repositories.cs`)
  and DI (`API/Program.cs`), identical to every other repository.
- **`API/FrontDesk/MatchAPI.cs`** — two new JSON endpoints:
  - `POST /api/SaveMatchReport` (JSON body `{OwnerId, MaleId, FemaleId, Notes}`)
    — upserts a `SavedMatchReportEntity`.
  - `GET /api/GetMatchReportList/OwnerId/{ownerId}` — reads all saved rows
    for that owner, re-computes each report live via the existing
    `MatchReportFactory` (same as `GetMatchReport`), grafting the saved
    `Id`/`Notes` onto the freshly-computed report rather than storing the
    whole report blob (keeps the score always current if the underlying
    birth data ever changes).
  - Both covered by `API.IntegrationTests/MatchEndpointsTests.cs`'s
    `SaveMatchReport_ThenGetMatchReportList_ReturnsSavedReportWithNotes`
    (save → list → assert Notes, then re-save with different Notes → assert
    upsert-not-duplicate). Full suite: 19 passed, 3 skipped (Chat, no
    LM Studio running), 0 failed.
- **`WebsiteNative`**: `src/lib/api/match.ts` gained `saveMatchReport`/
  `getMatchReportList`. New screen `src/app/Match/Saved.tsx` ports both
  `SavedReports.razor` and `MatchReportListViewer.razor` into one screen
  (couple name, heart-icon score summary, score %, notes, "View" button to
  `Match/Report`), reloading on focus (`useFocusEffect`) so a report saved
  from `Match/Report` shows up without a manual refresh. `Match/Report`
  gained a "Save Report" button (`saveMatchReport` + a success/error toast —
  see the toast library decision below) and `Match/Index` gained a "View
  Saved Matches" link. Confirmed via `expo export --platform web`: `/Match/Saved`
  and the updated `/Match` and `/Match/Report/[maleId]/[femaleId]` routes all
  bundle/static-render cleanly.

**Verification methodology used throughout** (same bar as Phase 1+2): `tsc
--noEmit` after every change, `npx expo export --platform web` to confirm
actual bundling/static-rendering (not just typechecking) of every new
route, `dotnet build`/`dotnet test` for every API-side change, all API
integration tests re-run full-suite before considering a slice done.

**Horoscope** (`Calculator/Horoscope.razor` → `src/app/Horoscope/index.tsx` +
`src/app/Horoscope/[personId].tsx`) — first calculator page ported (highest-traffic per the
porting-order decision above):
- New `src/lib/time.ts`: `timeToUrl(birthTime)` rebuilds `Time.cs`'s `ToUrl()` format
  (`/Location/{name}/Time/{HH:mm}/{dd}/{MM}/{yyyy}/{zzz}`) client-side from a person's
  already-parsed `BirthTime` JSON (`{StdTime, Location: {Name, Longitude, Latitude}}` — `Time.cs`'s
  `ToJson()`/`FromJson()` shape), instead of carrying a `Time` object across the network.
  `Person.birthTime` (`src/lib/api/person.ts`) is now typed as `BirthTimeJson` instead of `unknown`.
- New `src/lib/api/horoscope.ts` replaces vedastro.js's reflection-metadata-driven
  `GenerateAstroTable`/`GenerateAshtakvargaTable` (which built one fetch per table cell from
  `/ListCalls` metadata) with **typed, fixed-shape fetches** against the same underlying
  reflection-dispatched `Calculate/{name}` endpoints — confirmed by reading the actual
  `Library/Logic/Calculate/*.cs` method signatures rather than trusting the JS-side column
  metadata: `getHoroscopePredictions`, `getPlanetTable` (6 columns × 9 planets), `getHouseTable`
  (5 columns × 12 houses), `getSarvashtakavargaChart`/`getBhinnashtakavargaChart` (parses
  `Sarvashtakavarga.ToJson()`'s `{Total, Rows}` per-planet shape, and `Bhinnashtakavarga`'s plain
  `{ZodiacName: points}` map, into one common `AshtakvargaRow` shape), plus
  `getSkyChartImageUrl`/`getIndianChartImageUrl` (no JSON — server-rendered image URLs, per the
  `SkyChartViewer.razor`/`IndianChart.razor` `<img>`-only pattern already noted above).
- **Known gap found and left visible, not hidden**: the house table's "Aspects" column
  (`PlanetsAspectingHouse(HouseName, Time)`) has **no real implementation in `Library`** — only a
  documentation-only entry in `Library/Data/OpenAPIStaticTable.cs`, confirmed by grepping for an
  actual method definition and finding none. `callCalculate()` degrades any failing/missing
  endpoint to a `"—"` cell rather than crashing the whole table, so this reads as a normal empty
  cell today; flagged here the same way `LibraryTests`' pre-existing gaps are flagged, rather than
  silently worked around.
- New components: `SkyChartViewer.tsx`/`IndianChart.tsx` (straight `<Image>` ports, no interop, as
  predicted), `HoroscopeReferenceList.tsx` (prediction list — the original's planet/house/sign
  filter dropdowns were already a `"todo"` stub in the Blazor source, so the unfiltered list is a
  faithful port of what actually worked, not a regression), `PlanetDataTable.tsx`/
  `HouseDataTable.tsx`/`AshtakvargaTable.tsx` (horizontally-scrollable RN tables replacing
  vedastro.js's DOM table generation).
- Ayanamsa/chart-style selection is a small custom "chip" row (`ChipGroup` in
  `[personId].tsx`) rather than a native `<select>`/RN picker — no picker library was installed
  yet in `WebsiteNative`, and pulling one in for two narrow enum choices didn't seem worth it;
  revisit if more pages need a real dropdown.
- **Not ported this session (deferred, same bar as other Phase 3 gaps)**: `StrengthChart.razor`
  (hand-rolled `<canvas>` + JS interop bar charts, confirmed genuinely canvas-based, no RN
  equivalent chosen yet) and `PlanetChart.razor` (inline hand-built SVG bars from client-computed
  `Calculate.AllPlanetStrength`, labeled "Experimental" in the original UI — lower priority, and
  would need either an equivalent API endpoint or porting the strength calculation to the client).
  `AIPrediction.razor` (a second, more elaborate ranked-prediction view over the same
  `HoroscopePredictions` data `HoroscopeReferenceList` already shows) also not ported — judged
  redundant with `HoroscopeReferenceList` for a first pass, not a gap in coverage.
- Verified via `npx tsc --noEmit` (clean aside from two pre-existing, unrelated CSS-module type
  errors already present before this change) and `npx expo export --platform web` — `/Horoscope`
  and `/Horoscope/[personId]` both bundle/static-render cleanly alongside the previously-ported
  routes.

### Icon library and SweetAlert2/tippy.js replacement — decided and wired up

Both were open blocking decisions in the previous revision of this doc;
both are now resolved:

- **Icon library: `lucide-react-native`** (chosen over `@expo/vector-icons`
  for a closer visual match to the old Iconify set). Installed alongside
  its `react-native-svg` peer dependency. New `src/components/Icon.tsx` is
  a small **semantic registry**, not a 1:1 Iconify port — it maps a
  hand-picked set of names (`heart-broken`, `heart-flash`, `heart-half-full`,
  `cards-heart`, `heart-plus`, `plus`, `search`, `user`) onto Lucide
  components, covering what's actually used by ported components so far.
  A `heartIconFromIconify(iconifyName)` helper maps `MatchReport.Summary.HeartIcon`
  (an Iconify string like `"mdi:heart-plus"` from `Library/Data/MatchReport.cs`)
  onto the registry, falling back to a plain heart for anything unmapped.
  Wired into `InfoBox` (optional `icon` prop), `PersonSelector` (search
  icon, "Add New Person" plus icon), and the new `Match/Saved.tsx` screen
  (heart-icon score summary). The ~100 distinct Iconify names used across
  still-unported Blazor pages are **not** all mapped yet — extend the
  registry as each new page needs an icon it doesn't already have.
- **SweetAlert2/tippy.js replacement: `react-native-toast-notifications`
  + a small custom popover** (not `react-native-paper`, to avoid pulling in
  a whole design-system dependency for two narrow concerns). `<ToastProvider>`
  wraps the app root in `_layout.tsx`; `src/lib/toast.ts` wraps its global
  imperative `Toast.show(...)` ref behind `showSuccessToast`/`showErrorToast`,
  mirroring the shape of the old `_jsRuntime.ShowAlert(type, message)`
  helper. First real usage: the "Save Report" button on `Match/Report`.
  `src/components/Popover.tsx` is the tippy.js replacement — deliberately
  minimal (tap-to-open/tap-to-close centered modal, not a positioned hover
  tooltip, since RN has no hover concept) — built but not yet consumed by
  any ported page; `EventsChartViewer`'s heavier tippy usage (still
  unported) may need a fancier anchored variant when that page is tackled.
  The existing local `confirm()` helper (`src/lib/confirm.ts`, wraps RN
  `Alert`) is unchanged and still used for Match's same-person/
  reversed-gender confirms.

### Dead/unreachable/deferred code left alone during Phase 3 so far

- **AddPerson / Person.Editor flow**: not ported. `PersonSelector`'s
  "Add New Person" button shows an honest "coming soon" alert instead of
  navigating anywhere. This caps how useful Match/Horoscope/etc. actually
  are right now — a guest or user can only pick from example people until
  this exists.
- **`Match/Finder`, `Match/Profile`**: still not ported (`Match/Index`,
  `Match/Report`, and — as of this session — `Match/Saved` are). Links to
  `PageRoute.MatchFinder` from both the Home page and Match/Index exist but
  currently 404 in `WebsiteNative` — expected/incremental, same as any
  not-yet-ported route linked from an already-ported page.
- Old Blazor endpoints/components (`SignInGoogle`/`SignInFacebook` API
  routes, `SignInButton.razor` itself, `MatchReportListViewer.razor`, the
  XML `WebsiteTools.GetSavedMatchList`) are **not dead** — the Blazor site
  still runs and still uses them. Nothing on the API side was removed or
  changed in a way that could break it; only additive endpoints
  (`SignInFirebase`, `GetMatchReport`, `SaveMatchReport`,
  `GetMatchReportList`) were introduced.

### Open questions for the rest of Phase 3

- ~~Icon library choice~~ — **resolved**: `lucide-react-native` (see above).
- ~~SweetAlert2/tippy.js replacement libraries~~ — **resolved**:
  `react-native-toast-notifications` + a custom `Popover` component (see
  above).
- ~~Saved match reports~~ — **resolved**: real Postgres persistence was
  built (see "Saved match reports" above), not dropped.
- **Firebase service-account key** — still blocked, needs access this
  session didn't have to the Firebase console. `/api/SignInFirebase`
  degrades gracefully without one (console warning, not a crash — see
  `API/Program.cs`), but sign-in can't actually complete end-to-end until
  someone with console access:
  1. Opens the Firebase Console → the project's Project Settings → Service
     Accounts tab.
  2. Clicks "Generate new private key" (downloads a JSON file).
  3. Places that file at `API/firebase-service-account.json` (already
     gitignored, same treatment as `API/appsettings.Development.json`) —
     or anywhere else, and points `appsettings.Development.json`'s
     `FirebaseServiceAccountKeyPath` at it.
  No code change is needed once the key exists; `Program.cs` already reads
  it conditionally.
- **OAuth redirect URI registration** — also still blocked, needs access to
  the Google Cloud Console and Facebook Developer Console. The
  Google/Facebook app registrations reused from the Blazor site only allow
  `vedastro.org`'s origin today. Someone with console access needs to:
  1. Google Cloud Console → APIs & Services → Credentials → the existing
     OAuth 2.0 Client ID → add `WebsiteNative`'s dev origin
     (`http://localhost:8081` or whatever `expo start --web` prints) and
     its eventual production origin to "Authorized JavaScript origins"/
     "Authorized redirect URIs".
  2. Facebook Developer Console → the existing app → Facebook Login →
     Settings → add the same origins to "Valid OAuth Redirect URIs".
  This is independent of the Firebase key above — both need to be done
  before Google/Facebook sign-in completes end-to-end in `WebsiteNative`.

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
- **Phase 3 icon library**: `lucide-react-native` (over `@expo/vector-icons`),
  for closer visual parity with the old Iconify icon set.
- **Phase 3 SweetAlert2/tippy.js library**: `react-native-toast-notifications`
  for toasts (over `react-native-paper`, to avoid a whole design-system
  dependency for one narrow concern) + a small custom `Popover` component
  for tippy.js-style tooltips.
- **Saved match reports**: build real Postgres persistence
  (`SavedMatchReportEntity`, `/api/SaveMatchReport`/`/api/GetMatchReportList`)
  rather than dropping the feature — it's a genuinely new feature (the old
  endpoint never existed server-side), not a straight port.
