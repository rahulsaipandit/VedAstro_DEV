# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Running the website locally

**As of the Postgres migration (branch `feature/postgres-migration`), the backend runs on
ASP.NET Core + Postgres + local disk instead of Azure Functions + Azure Table/Blob Storage.**
Azurite is no longer needed.

Two services, each in its own terminal (Postgres is a one-time setup, not a per-session step):

```bash
# 0. One-time: a local Postgres instance (native service or `docker run postgres` both count as
#    "self-hosted on local computer" - use whichever is already on your machine). Then, once
#    per schema change:
cd Data
dotnet ef database update --project VedAstro.Data.csproj --startup-project VedAstro.Data.csproj
# (installs the schema described by Data/Migrations/*.cs into the `vedastro` database -
#  connection string comes from API/appsettings.Development.json's ConnectionStrings:Postgres)

# 1. API (ASP.NET Core, minimal API) — includes chat logic (Library/Logic/Calculate/ChatAPI.cs)
cd API
dotnet run
# Listens on http://localhost:7071 (Kestrel)

# 2a. Website (Blazor) - the older web frontend, still deployed
cd Website
dotnet run
# Listens on http://localhost:5000 (prints the exact URL on startup)

# 2b. WebsiteNative (Expo/React Native) - the current frontend; Website_Mobile is stale/
#     superseded and WebsiteNative is where new frontend work (incl. the Horoscope page,
#     SkyChart/IndianChart viewers) actually happens. Runs on web, iOS, and Android from one
#     codebase. Read WebsiteNative/AGENTS.md before writing any code here - Expo has changed
#     versions since most training data, so its own instructions there take priority.
cd WebsiteNative
npm install   # first time only
npm run web   # or `npm run ios` / `npm run android`; plain `npm start` prints a QR code + menu
```

`API/appsettings.Development.json` holds the local-dev secrets/connection string (this replaces
Azure Functions' `API/local.settings.json`, which no longer exists). Chart images are cached to
local disk under `API/ChartCache/` (configurable via `ChartCacheDirectory`) instead of an Azure
Blob container.

By default both frontends talk to the deployed API (`vedastroapi.azurewebsites.net`), not your
local one. To point either at `localhost:7071`:
- **Website** (Blazor): open the site, use the sidebar "Local API" toggle (sets
  `localStorage.DebugMode`), then reload. See `Localhost_Setup.md` for the full list of code
  changes behind this toggle.
- **WebsiteNative**: same `debugMode` concept, persisted via `useAppStore` (see
  `WebsiteNative/src/store/useAppStore.ts` and `WebsiteNative/src/constants/urls.ts`) instead of
  a raw `localStorage` key - toggle it from the app's settings/debug screen.

**Note:** `Setup.md`'s ChatAPI section (a separate Python/Docker service on
port 8000, LM Studio, etc.) describes an older architecture and is stale —
chat is now handled inside the API project above, not a standalone service.
The Python ChatAPI only exists as a historical reference under `docs/` (see below).

## Running tests

All backend functionality is verifiable via automated tests over real HTTP/DB calls —
no browser required. **Docker must be running** (tests use Testcontainers to spin up a
disposable Postgres instance per run; nothing needs to be installed or started manually).

```bash
# Repository/data-layer unit tests (against a real, ephemeral Testcontainers Postgres)
dotnet test Data/VedAstro.Data.Tests/VedAstro.Data.Tests.csproj

# Full HTTP-level integration tests (spins up the real ASP.NET Core app in-process via
# WebApplicationFactory<Program>, hits every endpoint over real HTTP)
dotnet test API/API.IntegrationTests/API.IntegrationTests.csproj
```

Both suites are also registered in `VedAstro.sln`, so `dotnet test VedAstro.sln` runs them
together (along with any other test projects in the solution).

**Chat API tests**: the `ChatEndpointsTests` class exercises `Calculate.HoroscopeChat` /
`HoroscopeFollowUpChat` / `HoroscopeChatFeedback` (reached through `OpenAPI.cs`'s
`Calculate/{calculatorName}/...` reflection dispatcher) against a real LLM. These use
`[SkippableFact]` — they skip cleanly (not fail) if no OpenAI-compatible server is
reachable. To actually exercise them, run **LM Studio** (or any OpenAI-compatible server)
locally with a model loaded, and set before running tests:

```bash
export LOCAL_LLM_BASE_URL=http://localhost:1234/v1   # LM Studio's default
export LOCAL_LLM_API_KEY=local-llm                   # any placeholder value works
export LOCAL_LLM_MODEL=<the model name loaded in LM Studio>
```

Without these set (or with no server reachable at that URL), the Chat tests skip and the
rest of the suite still passes.

## docs/ directory

If there is a specific request to update documentation, you can edit files in `docs/` folder. If there is no explicit request, treat it as a read-only folder.
