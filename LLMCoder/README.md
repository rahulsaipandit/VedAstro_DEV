# LLMCoder

A small local web app for chatting with LLMs while injecting curated source-file context into
the conversation — a developer utility, not a shipped product. Originally a Windows-only WinForms
app; now a Node/Express backend + plain HTML/CSS/vanilla JS frontend, so it runs anywhere Node
runs and opens in any browser.

## Features

- Chat with any OpenAI-compatible LLM server — defaults to **LM Studio**
  (`http://localhost:1234/v1`), switchable in-app to **Ollama** or **vLLM** (or any other
  OpenAI-compatible base URL) via the ⚙ Settings panel, no file editing required.
- Auto-detects which model is loaded via the server's `GET {baseUrl}/models` endpoint - no need
  to type a model name unless you want to pin one.
- Inject one or more source files (or line ranges of them) as prior context, each with its own
  pre/post prompt text.
- Save/load sets of injected files as named **presets**.
- Live KB/token usage meter against the selected model's context window.
- Editable, deletable, collapsible chat messages; history logged to `chat-history.json`; past
  prompts resurface in a dropdown for reuse.
- Cancel an in-flight LLM call.

## Setup

```sh
cd LLMCoder
npm install
npm start
```

Open `http://localhost:3000` (override the port with `PORT=xxxx npm start`). On first run, with
no `config.json` yet, the app opens ⚙ Settings automatically so you can add your first LLM - pick
the "LM Studio" quick-fill button (or "Ollama"/"vLLM") to fill in its base URL, then Save.

`config.json` is gitignored — it's your local list of LLM configs (`ApiConfigs`, matching
`config.example.json`'s shape: `Name`, `BaseUrl`, `ApiKey`, `MaxContextWindowTokens`, optional
`Model`). `BaseUrl` is the server's OpenAI-compatible base (e.g. `http://localhost:1234/v1` for
LM Studio, `http://localhost:11434/v1` for Ollama, `http://localhost:8000/v1` for a default vLLM
`--served-model-name` setup) - no `/chat/completions` suffix, the server appends that itself. The
server keeps `BaseUrl`/`ApiKey` on the backend; the browser only ever sees an LLM's
`name`/`maxContextWindowTokens`/detected `model`.

`presets.json` and `chat-history.json` are plain local JSON files the server reads/writes as you
use the app.

## Architecture

- `server.js` — Express app. Serves `public/` and exposes:
  - `GET /api/llm-configs` — configs for the dropdown (name/model/max-context only).
  - `GET`/`PUT /api/settings` — full config CRUD for the Settings panel (includes `BaseUrl`/`ApiKey`).
  - `POST /api/chat` — resolves the model (pinned `Model`, or auto-detected via `GET
    {baseUrl}/models`) and proxies the chat-completion call to the selected config's `BaseUrl`.
  - `GET /api/file` — extracts a line range from a file on disk.
  - `GET`/`PUT /api/presets`, `GET`/`POST /api/history`.
- `public/` — the whole frontend: `index.html` + `style.css` + `app.js`, no build step.
