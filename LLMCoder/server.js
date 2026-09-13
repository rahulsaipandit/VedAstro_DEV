const express = require("express");
const fs = require("fs");
const path = require("path");

const app = express();
app.use(express.json({ limit: "50mb" }));
app.use(express.static(path.join(__dirname, "public")));

const CONFIG_PATH = path.join(__dirname, "config.json");
const PRESETS_PATH = path.join(__dirname, "presets.json");
const HISTORY_PATH = path.join(__dirname, "chat-history.json");

const EXTENSION_TO_LANGUAGE = {
  ".js": "JavaScript",
  ".cs": "C#",
  ".html": "HTML",
  ".py": "Python",
  ".java": "Java",
  ".cpp": "C++",
  ".c": "C",
  ".php": "PHP",
  ".rb": "Ruby",
  ".swift": "Swift",
  ".go": "Go",
  ".ts": "TypeScript",
  ".vb": "Visual Basic",
  ".sql": "SQL",
};

function readJsonFile(filePath, fallback) {
  try {
    if (!fs.existsSync(filePath)) return fallback;
    return JSON.parse(fs.readFileSync(filePath, "utf8"));
  } catch {
    return fallback;
  }
}

function writeJsonFile(filePath, data) {
  fs.writeFileSync(filePath, JSON.stringify(data, null, 2));
}

// ApiConfigs are keyed by a BaseUrl (e.g. "http://localhost:1234/v1") rather than a full
// chat-completions URL, so the same shape works unmodified against LM Studio, Ollama, and vLLM -
// all three serve an OpenAI-compatible /chat/completions and /models under that base.
function loadApiConfigs() {
  const configFile = readJsonFile(CONFIG_PATH, { ApiConfigs: [] });
  return Array.isArray(configFile.ApiConfigs) ? configFile.ApiConfigs : [];
}

function baseUrlOf(config) {
  return String(config.BaseUrl || "").replace(/\/+$/, "");
}

// If the config pins a Model, use it as-is. Otherwise ask the server what's loaded via the
// standard OpenAI-compatible GET /models endpoint (LM Studio, Ollama, and vLLM all support this)
// and use the first model it reports.
async function resolveModelName(config) {
  if (config.Model && config.Model.trim()) return config.Model.trim();

  const url = `${baseUrlOf(config)}/models`;
  const res = await fetch(url, {
    headers: config.ApiKey ? { Authorization: `Bearer ${config.ApiKey}` } : {},
  });
  if (!res.ok) {
    throw new Error(`Could not list models from ${url} (${res.status})`);
  }
  const body = await res.json();
  const modelId = body?.data?.[0]?.id;
  if (!modelId) {
    throw new Error(`No models reported by ${url} - is a model loaded?`);
  }
  return modelId;
}

// GET /api/llm-configs - list available LLMs without exposing baseUrl/apiKey to the browser
app.get("/api/llm-configs", async (req, res) => {
  const configs = loadApiConfigs();
  const results = await Promise.all(
    configs.map(async (c) => {
      let model = "";
      let modelError = "";
      try {
        model = await resolveModelName(c);
      } catch (err) {
        modelError = err.message;
      }
      return {
        name: c.Name,
        maxContextWindowTokens: c.MaxContextWindowTokens || 32000,
        model,
        modelError,
      };
    })
  );
  res.json(results);
});

// GET /api/settings - full ApiConfigs list (including baseUrl/apiKey) for the Settings editor.
// Local single-user tool bound to localhost, so this is fine to expose unredacted.
app.get("/api/settings", (req, res) => {
  res.json(loadApiConfigs());
});

// PUT /api/settings - overwrite the full ApiConfigs list
app.put("/api/settings", (req, res) => {
  writeJsonFile(CONFIG_PATH, { ApiConfigs: req.body || [] });
  res.json({ ok: true });
});

// POST /api/chat - proxy a chat completion call to the selected LLM's endpoint
app.post("/api/chat", async (req, res) => {
  const { llmConfigName, messages, temperature, topP } = req.body || {};

  const configs = loadApiConfigs();
  const config = configs.find(
    (c) => c.Name.toLowerCase() === String(llmConfigName || "").toLowerCase()
  );
  if (!config) {
    return res.status(400).json({ error: `LLM config '${llmConfigName}' not found.` });
  }

  let model;
  try {
    model = await resolveModelName(config);
  } catch (err) {
    return res
      .status(502)
      .json({ error: `Could not determine model for '${config.Name}': ${err.message}` });
  }

  const requestBody = {
    messages,
    temperature: typeof temperature === "number" ? temperature : 0,
    top_p: typeof topP === "number" ? topP : 0,
    model,
  };

  const controller = new AbortController();
  req.on("close", () => controller.abort());

  try {
    const upstreamResponse = await fetch(`${baseUrlOf(config)}/chat/completions`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${config.ApiKey}`,
        "api-key": config.ApiKey,
      },
      body: JSON.stringify(requestBody),
      signal: controller.signal,
    });

    const rawText = await upstreamResponse.text();

    if (!upstreamResponse.ok) {
      return res
        .status(502)
        .json({ error: `Error: ${upstreamResponse.status} - ${rawText}` });
    }

    const parsed = JSON.parse(rawText);
    const content = parsed?.choices?.[0]?.message?.content ?? "No response!!";
    res.json({ content });
  } catch (err) {
    if (err.name === "AbortError") {
      return; // client disconnected/cancelled, nothing to send back
    }
    res.status(500).json({ error: err.message });
  }
});

// GET /api/file - extract a line range from a file on disk
app.get("/api/file", (req, res) => {
  const filePath = req.query.path;
  const start = parseInt(req.query.start, 10) || 1;
  let end = parseInt(req.query.end, 10) || 0;

  if (!filePath) {
    return res.status(400).json({ error: "path is required" });
  }

  let lines;
  try {
    lines = fs.readFileSync(filePath, "utf8").split(/\r\n|\r|\n/);
  } catch (err) {
    return res.status(404).json({ error: `File could not be read or does not exist: ${filePath}` });
  }

  const maxLines = lines.length;
  if (end === 0) end = maxLines;

  if (start < 1 || end < 1 || start > end) {
    return res.status(400).json({ error: "Invalid line range" });
  }

  const extractedCode = lines.slice(start - 1, end).join("\n");
  const languageName = EXTENSION_TO_LANGUAGE[path.extname(filePath).toLowerCase()] || "";

  res.json({ extractedCode, maxLines, languageName });
});

// GET /api/presets - load saved file-inject presets
app.get("/api/presets", (req, res) => {
  res.json(readJsonFile(PRESETS_PATH, []));
});

// PUT /api/presets - overwrite the full preset list
app.put("/api/presets", (req, res) => {
  writeJsonFile(PRESETS_PATH, req.body || []);
  res.json({ ok: true });
});

// GET /api/history - full chat log (used to populate past-prompts)
app.get("/api/history", (req, res) => {
  res.json(readJsonFile(HISTORY_PATH, []));
});

// POST /api/history - append one log entry
app.post("/api/history", (req, res) => {
  const history = readJsonFile(HISTORY_PATH, []);
  history.push(req.body);
  writeJsonFile(HISTORY_PATH, history);
  res.json({ ok: true });
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, "localhost", () => {
  console.log(`LLMCoder running at http://localhost:${PORT}`);
});
