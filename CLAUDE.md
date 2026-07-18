# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Running the website locally

Three services, each in its own terminal, no Docker required:

```bash
# 1. Azurite (local Azure Storage emulator — required by the API)
npx azurite

# 2. API (Azure Functions, .NET) — includes chat logic (Library/Logic/Calculate/ChatAPI.cs)
cd API
dotnet build
func start --verbose
# Listens on http://localhost:7071

# 3. Website (Blazor)
cd Website
dotnet run
# Listens on http://localhost:5000 (prints the exact URL on startup)
```

`API/local.settings.json` already has `UseDevelopmentStorage=true` wired up for
Azurite, so no cloud storage keys are needed for local dev.

By default the website talks to the deployed API (`vedastroapi.azurewebsites.net`),
not your local one. To point it at `localhost:7071`: open the site, use the
sidebar "Local API" toggle (sets `localStorage.DebugMode`), then reload. See
`Localhost_Setup.md` for the full list of code changes behind this toggle.

**Note:** `Setup.md`'s ChatAPI section (a separate Python/Docker service on
port 8000, LM Studio, etc.) describes an older architecture and is stale —
chat is now handled inside the API project above, not a standalone service.
The Python ChatAPI only exists as a historical reference under `docs/` (see below).

## docs/ directory

`docs/` is a read-only snapshot kept for comparison only — never edit files
under it.
