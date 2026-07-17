# VedAstro Local Development Setup

## Quick Run Commands
# Terminal 1: Azurite (storage emulator)
npx azurite

# Terminal 2: API (Azure Functions)
cd API
dotnet build
func start --verbose

# Terminal 3: ChatAPI (Docker; build first if needed: docker build -t chat-api .)
cd ChatAPI
docker run -p 8000:8000 -p 80:80 --env-file .env -d chat-api

# Terminal 4: Website
cd Website
dotnet run

## Quick Start Options

### Option 1: Docker (Recommended — mirrors production)
**Prerequisites:** Docker Desktop installed and running.

#### Step 1 — Create your .env file
Copy `ChatAPI/.env-EDIT-ME` to `ChatAPI/.env` and fill in your keys:

```powershell
Copy-Item ChatAPI\.env-EDIT-ME ChatAPI\.env
```

# Then edit ChatAPI\.env and set ANYSCALE_API_KEY and PASSWORD

#### Step 2 — Build the Docker image (from the ChatAPI/ folder)
```bash
cd ChatAPI
docker build -t chat-api .
```

#### Step 3 — Run the container
```bash
docker run -p 8000:8000 -p 80:80 --env-file .env -d chat-api
```

The API will be available at 
http://localhost:7071/api/version
http://localhost:5000/

**http://localhost:8000** and the file browser at **http://localhost** (default password: `admin`).

---
### Option 2: Run Directly with Python (faster for dev)

This uses the VS Code launch config already defined in `ChatAPI/.vscode/launch.json`.

#### Step 1 — Create .env (same as above)

#### Step 2 — Install dependencies (requires .NET 7.0 runtime for the vedastro lib)

```bash
cd ChatAPI
pip install -r requirements.txt
```

#### Step 3 — Load env vars and start uvicorn

From the `ChatAPI\` directory:

```powershell
Get-Content .env | ForEach-Object { if ($_ -match "^(\w+)=(.+)$") { [System.Environment]::SetEnvironmentVariable($Matches[1], $Matches[2]) } }
cd src
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

Or simply use VS Code's debugger — open the `ChatAPI/` folder in VS Code and run the **"FastAPI"** launch config (F5). It will auto-install requirements and start with `--reload`.

---

## Key Notes
| Thing             | Detail                                                                                |
|-------------------|---------------------------------------------------------------------------------------|
| API port          | 8000                                                                                  |
| Required env var  | `ANYSCALE_API_KEY` — the app crashes at startup if missing                            |
| vedastro lib      | Needs .NET 7 runtime on host (it's a Python wrapper around .NET lib)                  |
| Website → API URL | Check if the website's config points to `localhost:8000` for local dev                |
| .env location     | The `.env` file **must** be inside `ChatAPI/` (same folder as the Dockerfile)|
---

## Step-by-Step: Docker Desktop on Windows

### Step 1 — Install Docker Desktop
1. Download from: https://www.docker.com/products/docker-desktop/
2. Run the installer (it will enable WSL 2 or Hyper-Ver automatically on Windows 11)
3. After install, restart your machine if prompted
4. Launch Docker Desktop and wait for it to say **"Engine running"** in the bottom-left
> **Note:** Docker Desktop requires WSL 2 on Windows 11. The installer handles this, but if it prompts you to update your WSL kernel, follow the link it gives you.

### Step 2 — Create your .env file
Once Docker is running, continue with the Docker option above.

---

## Using LM Studio for Local LLM (Chat Feature)

The chat engine has been migrated from Azure OpenAI to **LM Studio** for local development (no cloud API keys required for the main chat flow).

### LM Studio Checklist (do this before running the container)
- Open **LM Studio** → go to the **"Local Server"** tab (left sidebar)
- Load a chat model — any instruction-tuned model works (Mistral 7B, LLaMA 3, etc.)
- Load an embedding model — search for `nomic-embed-text` in the model browser and load it alongside the chat model
- Start the server — click **"Start Server"** → it should say `Running on http://localhost:1234`
- In LM Studio settings, enable **"Allow connections from local network"** (needed for Docker to reach it via `host.docker.internal`)

### Why ANYSCALE_API_KEY is still needed (but can be fake)
- `main.py` checks that `ANYSCALE_API_KEY` exists at startup (harmless guard).
- The **real** Anyscale key is only used by the `/SummarizePrediction` admin endpoint.
- The main **`/HoroscopeChat` WebSocket** now routes through the LM Studio engine.

For local testing set:

```env
ANYSCALE_API_KEY=local-dev
```

You can later point the summarize endpoint at LM Studio too if needed:

```python
# From:
client = openai.OpenAI(base_url="https://api.endpoints.anyscale.com/v1", api_key=...)

# To:
client = openai.OpenAI(base_url=os.environ.get("LM_STUDIO_BASE_URL", "http://host.docker.internal:1234/v1"), api_key="lm-studio")
```
---

## Architecture: The Three Services

```
┌─────────────────┐     REST/XML      ┌──────────────────────────┐
│                 │ ─────────────────► │  API (Azure Functions)   │
│  Website        │                    │  .NET  · port 7071       │
│  Blazor WASM    │                    │  local.settings.json     │
│  .NET · any port│                    └──────────────────────────┘
│                 │     WebSocket      ┌──────────────────────────┐
│                 │ ─────────────────► │  ChatAPI (Docker)        │
│                 │                    │  Python FastAPI · :8000  │
└─────────────────┘                    └──────────────────────────┘
```

### What you actually need to run
| Service | Run locally?   | Why                                                                               |
|---------|----------------|-----------------------------------------------------------------------------------|
| Website | ✅ Yes          | It's what you're building                                                         |
| ChatAPI | ✅ Yes (Docker) | Already in progress                                                               |
| API     | ⚠️ Optional     | When running on localhost, the website points to beta.api.vedastro.org by default |

### How to start everything
1. **ChatAPI (Docker)** — already building in previous steps

   ```bash
   cd ChatAPI
   docker run -p 8000:8000 -p 80:80 --env-file .env -d chat-api
   ```

2. **Website**

   ```bash
   cd Website
   dotnet run
   ```
   Then open the URL it prints (e.g. `https://localhost:5001`).

3. **Switch Chat to local server (in the browser)**
   The Chat page has a **"Use Local Server"** toggle in its settings. When enabled it switches the WebSocket from the live Azure server to:
   ```
   ws://127.0.0.1:8000/HoroscopeChat
   ```
   That hits your local Docker container.

### If you also want a local API (optional)

Only needed if you're editing the .NET API code. You'd need:

- Fill in `API/local.settings.json` with Azure Storage keys (or use `UseDevelopmentStorage=true`)
- Run it: `cd API && func start` (requires Azure Functions Core Tools)
- In the website, go to your profile → **Enable Debug** → this tells the website to use `localhost:7071` instead of the cloud

> You can develop and test most of the API without any cloud keys at all. The only hard requirement for the Functions runtime is `AzureWebJobsStorage=UseDevelopmentStorage=true` (uses the local Azurite emulator).

---

## Common Issues & Fixes

### CORS error for ConsoleGreeting.txt / static files
```
Access to XMLHttpRequest at 'https://vedastro.org/data/ConsoleGreeting.txt' ... blocked by CORS policy
```
**Root cause:** Hardcoded production domain in `Tools.js` (and sometimes `URLS.js`).
**Fix:** Serve the file locally using a relative URL from the website's own static files (`wwwroot`).

### Llama-Index Package Conflicts
Old monolithic `llama_index` package + new modular `llama-index-*` packages caused pip to backtrack for 37+ minutes.

**Fixed by:**
- Removed `llama_index` monolith
- Pinned all `llama-index-*` packages to a coherent generation (aligned on 0.11.x)
- Upgraded pip in the Dockerfile for faster modern resolver
- Deleted stale vendored `llama_index/` subdirectories that were shadowing pip packages

### Model Name Validation Errors with LM Studio

`OpenAIEmbedding` / LLM classes validate against strict OpenAI enum.

**Solution:** Use valid OpenAI names in config (LM Studio ignores the name and uses whatever model you have loaded locally):

- Chat model: `gpt-3.5-turbo`
- Embedding model: `text-embedding-ada-002`

---

## Background: LLM Provider History

| Endpoint             | Original Provider        | Current (Local Dev)  |
|----------------------|--------------------------|----------------------|
| /HoroscopeChat       | Azure OpenAI → LM Studio | LM Studio            |
| /SummarizePrediction | Anyscale (Mistral 7B)    | LM Studio (optional) |

The two endpoints were written at different times. The summarize endpoint is admin-only (requires `PASSWORD`).

---

## Verification Commands

```powershell
# Check Docker
docker --version
docker info --format "{{.ServerVersion}}"

# Check Azurite (for local API)
azurite --version
npx azurite --version
```

---
**End of Setup Guide**
// Import the functions you need from the SDKs you need
import { initializeApp } from "firebase/app";
// TODO: Add SDKs for Firebase products that you want to use
// https://firebase.google.com/docs/web/setup#available-libraries

// Your web app's Firebase configuration


// Initialize Firebase
const app = initializeApp(firebaseConfig);

==========================================
## Appendix
To find and checkout a tag named stable (or similar), run these git commands:

1. List all tags:
git tag

2. If there's a stable tag, checkout it:
git checkout stable

3. If the tag name is different (e.g., v1.2.0), list with dates to find the right one:
git log --tags --simplify-by-decoration --pretty="format:%d %h %ai" | head -20

4. Or list tags sorted by date:
git tag --sort=-creatordate | head -20

5. Checkout a specific tag:
git checkout tags/stable          # if tag is named "stable"
git checkout tags/v1.2.0          # if tag is versioned
Note: Checking out a tag puts you in "detached HEAD" state. If you want to work on it, create a branch from it:

git checkout -b my-stable-branch tags/stable