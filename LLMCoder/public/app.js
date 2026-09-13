// ---------- state ----------
let llmConfigs = [];
let selectedLlmConfig = null;
let conversationHistory = []; // [{id, role, content}]
let presets = [];
let abortController = null;
let thinkingTimerHandle = null;

// ---------- DOM refs ----------
const llmSelector = document.getElementById("llmSelector");
const temperatureInput = document.getElementById("temperature");
const topPInput = document.getElementById("topP");
const kbUsageLabel = document.getElementById("kbUsageLabel");
const tokenUsageBar = document.getElementById("tokenUsageBar");
const tokenUsageText = document.getElementById("tokenUsageText");

const snippetInjectCheckbox = document.getElementById("snippetInjectCheckbox");
const codeInjectPretext = document.getElementById("codeInjectPretext");
const largeCodeSnippet = document.getElementById("largeCodeSnippet");
const injectAssistantPretext = document.getElementById("injectAssistantPretext");
const snippetTokenCount = document.getElementById("snippetTokenCount");

const fileInjectCheckbox = document.getElementById("fileInjectCheckbox");
const presetSelect = document.getElementById("presetSelect");
const savePresetButton = document.getElementById("savePresetButton");
const addFileInjectButton = document.getElementById("addFileInjectButton");
const fileInjectList = document.getElementById("fileInjectList");

const pastUserPrompts = document.getElementById("pastUserPrompts");
const copyFinalPromptButton = document.getElementById("copyFinalPromptButton");
const resetChatHistoryButton = document.getElementById("resetChatHistoryButton");
const outputChatMessagePanel = document.getElementById("outputChatMessagePanel");

const userInput = document.getElementById("userInput");
const sendUserMsgButton = document.getElementById("sendUserMsgButton");
const clearUserMsgButton = document.getElementById("clearUserMsgButton");
const terminateOngoingLLMCallButton = document.getElementById("terminateOngoingLLMCallButton");

const llmThinkingRow = document.getElementById("llmThinkingRow");
const llmThinkingLabel = document.getElementById("llmThinkingLabel");
const llmThinkingTimerLabel = document.getElementById("llmThinkingTimerLabel");
const llmThinkingProgressBar = document.getElementById("llmThinkingProgressBar");

const openSettingsButton = document.getElementById("openSettingsButton");
const settingsDialog = document.getElementById("settingsDialog");
const settingsConfigList = document.getElementById("settingsConfigList");
const addSettingsConfigButton = document.getElementById("addSettingsConfigButton");
const saveSettingsButton = document.getElementById("saveSettingsButton");
const closeSettingsButton = document.getElementById("closeSettingsButton");

// ---------- KB/token math (ported from Form1.cs) ----------
function getBinarySizeOfTextInKB(text) {
  if (!text) return 0;
  return new TextEncoder().encode(text).length / 1024;
}

function convertKBToTokenCount(kb) {
  return Math.floor((kb * 1024) / 4);
}

function convertTokenCountToKB(tokenCount) {
  return (tokenCount * 4) / 1024;
}

function formatNumberWithKAbbreviation(n) {
  if (n < 1000) return String(n);
  const thousands = n / 1000;
  return n % 1000 !== 0 ? `${thousands.toFixed(1)}K` : `${thousands}K`;
}

function interpolateColor(start, end, ratio) {
  const r = Math.round(start[0] + ratio * (end[0] - start[0]));
  const g = Math.round(start[1] + ratio * (end[1] - start[1]));
  const b = Math.round(start[2] + ratio * (end[2] - start[2]));
  return `rgb(${r}, ${g}, ${b})`;
}

function colorForPercentage(percent) {
  const GREEN = [0, 128, 0];
  const YELLOW = [255, 255, 0];
  const RED = [255, 0, 0];
  if (percent < 50) return "green";
  if (percent < 75) return interpolateColor(GREEN, YELLOW, (percent - 50) / 25);
  if (percent < 100) return interpolateColor(YELLOW, RED, (percent - 75) / 25);
  return "red";
}

// ---------- request-stack assembly (ported from GenerateFinalChatMessageStack) ----------
function getCurrentCodeFiles() {
  return Array.from(fileInjectList.querySelectorAll(".file-inject-row")).map((row) => ({
    filePath: row.querySelector(".file-path").value,
    startLineNumber: parseInt(row.querySelector(".start-line").value, 10) || 1,
    endLineNumber: parseInt(row.querySelector(".end-line").value, 10) || 0,
    prePrompt: row.querySelector(".pre-prompt").value,
    extractedCode: row.querySelector(".code-textarea").value,
    postPrompt: row.querySelector(".post-prompt").value,
  }));
}

function generateFinalChatMessages() {
  const messages = [];

  if (snippetInjectCheckbox.checked) {
    messages.push({ role: "user", content: `${codeInjectPretext.value}\n\`\`\`\n${largeCodeSnippet.value}\n\`\`\`` });
    messages.push({ role: "assistant", content: injectAssistantPretext.value });
  }

  if (fileInjectCheckbox.checked) {
    for (const cf of getCurrentCodeFiles()) {
      messages.push({ role: "user", content: `${cf.prePrompt}\n\`\`\`\n${cf.extractedCode}\n\`\`\`` });
      messages.push({ role: "assistant", content: cf.postPrompt });
    }
  }

  for (const m of conversationHistory) {
    messages.push({ role: m.role, content: m.content });
  }

  return messages;
}

function generateFinalRequestBody() {
  return {
    llmConfigName: selectedLlmConfig ? selectedLlmConfig.name : "",
    messages: generateFinalChatMessages(),
    temperature: parseFloat(temperatureInput.value) || 0,
    topP: parseFloat(topPInput.value) || 0,
  };
}

// ---------- global usage meters ----------
function updateGlobalTokenStats() {
  const finalMessageText = JSON.stringify(generateFinalRequestBody());
  const totalKb = getBinarySizeOfTextInKB(finalMessageText);
  const totalTokens = convertKBToTokenCount(totalKb);

  if (!selectedLlmConfig) return;

  const maxTokens = selectedLlmConfig.maxContextWindowTokens;
  const maxKb = convertTokenCountToKB(maxTokens);
  kbUsageLabel.textContent = `${totalKb.toFixed(2)} / ${maxKb.toFixed(2)} KB`;

  const percent = Math.min((totalTokens / maxTokens) * 100, 100);
  tokenUsageBar.style.width = `${percent}%`;
  tokenUsageBar.style.background = colorForPercentage(percent);
  tokenUsageText.textContent = `${formatNumberWithKAbbreviation(totalTokens)} / ${formatNumberWithKAbbreviation(maxTokens)} Tokens`;
}

// ---------- LLM selector ----------
async function loadLlmConfigs() {
  const res = await fetch("/api/llm-configs");
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    alert(`Error loading LLM configs: ${body.error || res.statusText}`);
    return;
  }
  llmConfigs = await res.json();
  const previouslySelected = llmSelector.value;
  llmSelector.innerHTML = "";
  for (const c of llmConfigs) {
    const opt = document.createElement("option");
    opt.value = c.name;
    opt.textContent = c.model
      ? `${c.name} (${c.model})`
      : `${c.name} (${c.modelError || "no model detected"})`;
    llmSelector.appendChild(opt);
  }
  if (llmConfigs.length === 0) {
    llmSelector.innerHTML = "<option value=''>No LLM configured - open Settings</option>";
    return;
  }
  const stillExists = llmConfigs.some((c) => c.name === previouslySelected);
  const nameToSelect = stillExists ? previouslySelected : llmConfigs[0].name;
  llmSelector.value = nameToSelect;
  updateSelectedLlm(nameToSelect);
}

function updateSelectedLlm(name) {
  selectedLlmConfig = llmConfigs.find((c) => c.name.toLowerCase() === name.toLowerCase()) || null;
  updateGlobalTokenStats();
}

llmSelector.addEventListener("change", () => updateSelectedLlm(llmSelector.value));
temperatureInput.addEventListener("change", updateGlobalTokenStats);
topPInput.addEventListener("change", updateGlobalTokenStats);

// ---------- snippet inject ----------
function countTokensRough(text) {
  return (text.match(/[^\s]+/g) || []).length;
}

largeCodeSnippet.addEventListener("input", () => {
  snippetTokenCount.textContent = `Token Count : ${countTokensRough(largeCodeSnippet.value)}`;
  updateGlobalTokenStats();
});
snippetInjectCheckbox.addEventListener("change", updateGlobalTokenStats);

// ---------- file inject rows ----------
function createFileInjectRow(data = {}) {
  const row = document.createElement("div");
  row.className = "file-inject-row";
  row.innerHTML = `
    <div class="row-line1">
      <label>Path</label>
      <input type="text" class="file-path" value="${data.filePath ? escapeHtmlAttr(data.filePath) : ""}" />
      <button class="delete-btn danger">➖ Remove</button>
    </div>
    <div class="row-line1 line-range">
      <label>Start</label>
      <input type="number" class="start-line" value="${data.startLineNumber ?? 1}" />
      <label>End</label>
      <input type="number" class="end-line" value="${data.endLineNumber ?? 0}" />
      <button class="fetch-update-btn">⚡ Update</button>
      <span class="status-label"></span>
    </div>
    <div class="prompt-row">
      <label>Pre-prompt</label>
      <input type="text" class="pre-prompt" value="${data.prePrompt ? escapeHtmlAttr(data.prePrompt) : ""}" />
    </div>
    <textarea class="code-textarea" rows="2">${data.extractedCode ? escapeHtml(data.extractedCode) : ""}</textarea>
    <div class="prompt-row">
      <label>Post-prompt</label>
      <input type="text" class="post-prompt" value="${data.postPrompt ? escapeHtmlAttr(data.postPrompt) : ""}" />
      <button class="expand-btn">Expand</button>
    </div>
  `;

  const pathInput = row.querySelector(".file-path");
  const startInput = row.querySelector(".start-line");
  const endInput = row.querySelector(".end-line");
  const prePromptInput = row.querySelector(".pre-prompt");
  const codeTextarea = row.querySelector(".code-textarea");
  const postPromptInput = row.querySelector(".post-prompt");
  const statusLabel = row.querySelector(".status-label");
  const expandBtn = row.querySelector(".expand-btn");

  function updateRowStats() {
    const kb = getBinarySizeOfTextInKB(codeTextarea.value);
    const tokens = convertKBToTokenCount(kb);
    statusLabel.textContent = `Size : ${kb.toFixed(2)} KB ~ ${tokens} Tokens`;
    updateGlobalTokenStats();
  }

  async function fetchAndFillFromDisk() {
    const filePath = pathInput.value.trim();
    if (!filePath) return;
    try {
      const params = new URLSearchParams({
        path: filePath,
        start: startInput.value || "1",
        end: endInput.value || "0",
      });
      const res = await fetch(`/api/file?${params}`);
      const body = await res.json();
      if (!res.ok) {
        codeTextarea.value = "!! Error : File could not be read or does not exist. !!";
        updateRowStats();
        return;
      }
      if (endInput.value === "0" || endInput.value === "") {
        endInput.value = body.maxLines;
      }
      codeTextarea.value = body.extractedCode;
      prePromptInput.value = `Analyse and parse below ${body.languageName} code`;
      postPromptInput.value = "Ok, I've parsed the code";
      updateRowStats();
      showSavePresetButton();
    } catch {
      codeTextarea.value = "!! Error : File could not be read or does not exist. !!";
      updateRowStats();
    }
  }

  pathInput.addEventListener("change", fetchAndFillFromDisk);
  row.querySelector(".fetch-update-btn").addEventListener("click", fetchAndFillFromDisk);

  row.querySelector(".delete-btn").addEventListener("click", () => {
    row.remove();
    updateGlobalTokenStats();
    showSavePresetButton();
  });

  expandBtn.addEventListener("click", () => {
    const expanded = codeTextarea.classList.toggle("expanded");
    expandBtn.textContent = expanded ? "Collapse" : "Expand";
  });

  [startInput, endInput, prePromptInput, postPromptInput, codeTextarea].forEach((el) =>
    el.addEventListener("input", () => {
      updateRowStats();
      showSavePresetButton();
    })
  );

  updateRowStats();
  return row;
}

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str;
  return div.innerHTML;
}
function escapeHtmlAttr(str) {
  return escapeHtml(str).replace(/"/g, "&quot;");
}

function showSavePresetButton() {
  savePresetButton.classList.remove("hidden");
}

addFileInjectButton.addEventListener("click", () => {
  fileInjectList.appendChild(createFileInjectRow());
  showSavePresetButton();
});
fileInjectCheckbox.addEventListener("change", updateGlobalTokenStats);

// ---------- presets ----------
async function loadPresets() {
  presets = await (await fetch("/api/presets")).json();
  presetSelect.innerHTML = "";
  const defaultOpt = document.createElement("option");
  defaultOpt.textContent = "Select...";
  defaultOpt.value = "";
  presetSelect.appendChild(defaultOpt);
  for (const p of presets) {
    const opt = document.createElement("option");
    opt.value = p.name;
    opt.textContent = p.name;
    presetSelect.appendChild(opt);
  }
}

presetSelect.addEventListener("change", () => {
  if (!presetSelect.value) return;
  const preset = presets.find((p) => p.name === presetSelect.value);
  if (!preset) return;
  fileInjectList.innerHTML = "";
  for (const cf of preset.injectedFilesData || []) {
    fileInjectList.appendChild(createFileInjectRow(cf));
  }
  updateGlobalTokenStats();
});

savePresetButton.addEventListener("click", async () => {
  const currentFiles = getCurrentCodeFiles();

  if (!presetSelect.value) {
    const name = window.prompt("Enter preset name:");
    if (!name) return;
    presets.push({ name, injectedFilesData: currentFiles });
    await fetch("/api/presets", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(presets),
    });
    await loadPresets();
    presetSelect.value = name;
    alert("Preset saved successfully!");
  } else {
    const preset = presets.find((p) => p.name === presetSelect.value);
    preset.injectedFilesData = currentFiles;
    await fetch("/api/presets", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(presets),
    });
    alert("Preset updated successfully!");
  }
  savePresetButton.classList.add("hidden");
});

// ---------- chat history / logging ----------
async function logChatMessageToFile(message) {
  await fetch("/api/history", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      Timestamp: new Date().toISOString(),
      Id: message.id,
      Role: message.role,
      Message: message.content,
    }),
  });
}

async function loadPastPrompts() {
  const history = await (await fetch("/api/history")).json();
  const userMessages = history.filter((h) => h.Role === "user").map((h) => h.Message);
  const unique = [...new Set(userMessages)];
  pastUserPrompts.innerHTML = "";
  for (const msg of unique) {
    const opt = document.createElement("option");
    opt.value = msg;
    opt.textContent = msg.length > 60 ? msg.slice(0, 60) + "…" : msg;
    pastUserPrompts.appendChild(opt);
  }
}

pastUserPrompts.addEventListener("change", () => {
  if (pastUserPrompts.value) {
    userInput.value += pastUserPrompts.value;
  }
});

// ---------- message panel ----------
function addMessageToPanel(content, role) {
  if (!content) return;

  const id = crypto.randomUUID();
  const message = { id, role, content };
  conversationHistory.push(message);
  logChatMessageToFile(message);

  const el = document.createElement("div");
  el.className = `chat-message role-${role}`;
  el.dataset.id = id;
  el.innerHTML = `
    <textarea rows="1">${escapeHtml(content)}</textarea>
    <div class="message-buttons">
      <button class="delete-msg-btn danger">Delete</button>
      <button class="expand-msg-btn">Collapse</button>
    </div>
  `;

  const textarea = el.querySelector("textarea");
  textarea.addEventListener("input", () => {
    const found = conversationHistory.find((m) => m.id === id);
    if (found) {
      found.content = textarea.value;
      logChatMessageToFile(found);
    }
  });

  el.querySelector(".delete-msg-btn").addEventListener("click", () => {
    conversationHistory = conversationHistory.filter((m) => m.id !== id);
    el.remove();
  });

  el.querySelector(".expand-msg-btn").addEventListener("click", (e) => {
    const expanded = textarea.classList.toggle("expanded");
    e.target.textContent = expanded ? "Expand" : "Collapse";
  });

  outputChatMessagePanel.appendChild(el);
  outputChatMessagePanel.scrollTop = outputChatMessagePanel.scrollHeight;
  updateGlobalTokenStats();
}

resetChatHistoryButton.addEventListener("click", () => {
  conversationHistory = [];
  outputChatMessagePanel.innerHTML = "";
  updateGlobalTokenStats();
});

clearUserMsgButton.addEventListener("click", () => {
  userInput.value = "";
});

copyFinalPromptButton.addEventListener("click", async () => {
  const body = generateFinalRequestBody();
  await navigator.clipboard.writeText(JSON.stringify(body));
});

// ---------- sending / thinking indicator ----------
function startThinkingIndicator() {
  llmThinkingRow.classList.remove("hidden");
  terminateOngoingLLMCallButton.classList.remove("hidden");
  llmThinkingLabel.textContent = "LLM Thinking...";
  llmThinkingProgressBar.style.width = "25%";

  const startTime = Date.now();
  thinkingTimerHandle = setInterval(() => {
    const elapsed = ((Date.now() - startTime) / 1000).toFixed(2);
    llmThinkingTimerLabel.textContent = `${elapsed} s`;
  }, 200);
}

function stopThinkingIndicator(finalLabel) {
  clearInterval(thinkingTimerHandle);
  llmThinkingProgressBar.style.width = "100%";
  llmThinkingLabel.textContent = finalLabel;
  terminateOngoingLLMCallButton.classList.add("hidden");
  setTimeout(() => {
    llmThinkingProgressBar.style.width = "0%";
    llmThinkingRow.classList.add("hidden");
  }, 300);
}

async function sendUserMessage() {
  const text = userInput.value;
  if (!text.trim()) return;

  addMessageToPanel(text, "user");
  userInput.value = "";
  startThinkingIndicator();

  abortController = new AbortController();
  const body = generateFinalRequestBody();

  try {
    llmThinkingProgressBar.style.width = "50%";
    const res = await fetch("/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
      signal: abortController.signal,
    });
    llmThinkingProgressBar.style.width = "75%";
    const data = await res.json();

    if (!res.ok) {
      addMessageToPanel(data.error || "Error occurred.", "error");
    } else {
      addMessageToPanel(data.content, "assistant");
    }
    stopThinkingIndicator("✅ Done");
  } catch (err) {
    if (err.name === "AbortError") {
      addMessageToPanel("LLM call cancelled.", "error");
      stopThinkingIndicator("LLM Call Cancelled");
    } else {
      addMessageToPanel(`Error: ${err.message}`, "error");
      stopThinkingIndicator("Error");
    }
  }

  await loadPastPrompts();
}

sendUserMsgButton.addEventListener("click", sendUserMessage);
userInput.addEventListener("keydown", (e) => {
  if (e.key === "Enter" && (e.ctrlKey || e.metaKey)) {
    sendUserMessage();
  }
});

terminateOngoingLLMCallButton.addEventListener("click", () => {
  if (abortController) abortController.abort();
});

// ---------- settings (LLM endpoint configuration) ----------
const QUICK_ENDPOINTS = [
  { label: "LM Studio", baseUrl: "http://localhost:1234/v1", apiKey: "lm-studio" },
  { label: "Ollama", baseUrl: "http://localhost:11434/v1", apiKey: "ollama" },
  { label: "vLLM", baseUrl: "http://localhost:8000/v1", apiKey: "vllm" },
];

function createSettingsRow(config = {}) {
  const row = document.createElement("div");
  row.className = "settings-row";
  row.innerHTML = `
    <input type="text" class="s-name" placeholder="Name" value="${escapeHtmlAttr(config.Name || "")}" />
    <input type="text" class="s-baseurl" placeholder="Base URL, e.g. http://localhost:1234/v1" value="${escapeHtmlAttr(config.BaseUrl || "")}" />
    <input type="text" class="s-apikey" placeholder="API Key" value="${escapeHtmlAttr(config.ApiKey || "")}" />
    <input type="number" class="s-maxtokens" placeholder="Max Context Tokens" value="${config.MaxContextWindowTokens || 32000}" />
    <input type="text" class="s-model" placeholder="Model (blank = auto-detect via /models)" value="${escapeHtmlAttr(config.Model || "")}" />
    <div class="settings-quickfill">
      ${QUICK_ENDPOINTS.map((q, i) => `<button type="button" class="quickfill-btn" data-i="${i}">${q.label}</button>`).join("")}
    </div>
    <button type="button" class="remove-settings-row-btn danger">➖ Remove</button>
  `;

  row.querySelectorAll(".quickfill-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const q = QUICK_ENDPOINTS[parseInt(btn.dataset.i, 10)];
      row.querySelector(".s-baseurl").value = q.baseUrl;
      const nameInput = row.querySelector(".s-name");
      const apiKeyInput = row.querySelector(".s-apikey");
      if (!nameInput.value) nameInput.value = q.label;
      if (!apiKeyInput.value) apiKeyInput.value = q.apiKey;
    });
  });

  row.querySelector(".remove-settings-row-btn").addEventListener("click", () => row.remove());

  return row;
}

async function openSettings() {
  const configs = await (await fetch("/api/settings")).json();
  settingsConfigList.innerHTML = "";
  if (configs.length === 0) {
    settingsConfigList.appendChild(
      createSettingsRow({ Name: "LM Studio", BaseUrl: "http://localhost:1234/v1", ApiKey: "lm-studio", MaxContextWindowTokens: 32000 })
    );
  } else {
    for (const c of configs) settingsConfigList.appendChild(createSettingsRow(c));
  }
  settingsDialog.showModal();
}

openSettingsButton.addEventListener("click", openSettings);
closeSettingsButton.addEventListener("click", () => settingsDialog.close());
addSettingsConfigButton.addEventListener("click", () => settingsConfigList.appendChild(createSettingsRow()));

saveSettingsButton.addEventListener("click", async () => {
  const rows = Array.from(settingsConfigList.querySelectorAll(".settings-row"));
  const apiConfigs = rows
    .map((row) => ({
      Name: row.querySelector(".s-name").value.trim(),
      BaseUrl: row.querySelector(".s-baseurl").value.trim(),
      ApiKey: row.querySelector(".s-apikey").value.trim(),
      MaxContextWindowTokens: parseInt(row.querySelector(".s-maxtokens").value, 10) || 32000,
      Model: row.querySelector(".s-model").value.trim(),
    }))
    .filter((c) => c.Name && c.BaseUrl);

  await fetch("/api/settings", {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(apiConfigs),
  });

  settingsDialog.close();
  await loadLlmConfigs();
});

// ---------- init ----------
(async function init() {
  await loadLlmConfigs();
  await loadPresets();
  await loadPastPrompts();
  fileInjectList.appendChild(createFileInjectRow());

  if (llmConfigs.length === 0) {
    openSettings();
  }
})();
