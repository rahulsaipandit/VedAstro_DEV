# LLMCoder

## Quick Note: running the site

```sh
cd LLMCoder
npm install
npm start
```

Open `http://localhost:3000` (override the port with `PORT=xxxx npm start`). On first run, with
no `config.json` yet, the app opens the ⚙ Settings panel automatically — pick the "LM Studio"
quick-fill button (or "Ollama"/"vLLM") to fill in that server's base URL, then Save. See
`LLMCoder/README.md` for the full setup/config details.

**LLMCoder** is a small local web app (`LLMCoder/`, Node/Express backend + plain HTML/CSS/vanilla
JS frontend) — a manual "prompt engineering" tool for chatting with LLMs while feeding them
curated code context. It's a developer utility, separate from the main API/Website/WebsiteNative
products.

It was originally a Windows-only WinForms (.NET) desktop app; it was converted to a local web app
so it runs on any OS with Node installed and opens in any browser (no build step for the
frontend).

## Key pieces

- **`LLMCoder/server.js`** — Express backend. Serves the static frontend under `public/` and
  exposes:
  - `GET /api/llm-configs` — the list of configured LLMs (name/max-context/model only; endpoint
    and API key stay server-side).
  - `POST /api/chat` — proxies a chat-completion request to the selected LLM's endpoint using its
    stored API key, and returns the reply text.
  - `GET /api/file` — extracts a given line range out of a file on disk (for file-context
    injection), also returning the file's total line count and a guessed language name from its
    extension.
  - `GET`/`PUT /api/presets` — load/save named sets of injected files (`presets.json`).
  - `GET`/`POST /api/history` — read/append the chat log (`chat-history.json`), used to power the
    "past prompts" dropdown.
- **`LLMCoder/public/`** — the whole frontend (`index.html`, `style.css`, `app.js`). Lets you:
  - Pick an LLM from a dropdown, set temperature/top-p.
  - Inject one or more source files (or line ranges of them) as prior "user/assistant" turns,
    each with configurable pre/post prompt text, so the LLM has code context before your real
    question.
  - Save/load these file-injection sets as named **presets**.
  - Track token/KB usage against the selected model's context window with a live meter.
  - Chat with the LLM in a scrollable message panel; each message is editable, deletable,
    expandable; history is logged to `chat-history.json` and past prompts resurface in a dropdown
    for reuse.
  - Cancel an in-flight LLM call.
- **`LLMCoder/config.json`** (gitignored, copy from `config.example.json`) — the local list of
  LLM endpoints/API keys the server reads.
- **`.github/workflows/UpdateLLMCodes.yml`** — a `workflow_dispatch` CI job that mirrors the
  `LLMCoder/` folder into a separate public repo, `VedAstro/LLMCodes`, wiping and re-pushing it
  each run. Unaffected by the web-app rewrite — it still just copies the whole folder
  (gitignored files like `config.json`/`node_modules/` are excluded from the checkout the same
  way `secrets.json` was before).

In short: it's an internal tool the VedAstro team built to have long, context-rich conversations
with LLMs about specific slices of their codebase (e.g. for planning refactors or generating
code), predating/alongside the newer in-app `ChatAPI`/`HoroscopeChat` feature described in
CLAUDE.md.
