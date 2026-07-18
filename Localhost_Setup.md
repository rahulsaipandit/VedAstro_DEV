# Localhost Setup — Changes Made to Remove Azure Cloud Dependencies

This document records every code change required to run the VedAstro website
against a local API server (`http://localhost:7071`) instead of the live Azure
Functions endpoint (`vedastroapi.azurewebsites.net`).

---

## Prerequisites (no code changes)

| Requirement | Command |
|---|---|
| Azure Functions Core Tools v4 | `npm install -g azure-functions-core-tools@4 --unsafe-perm true` |
| Azurite (local Azure Storage emulator) | `npm install -g azurite` |
| Start Azurite before running the API | `azurite` |
| Start the local API | `cd API && func start` |

Fill in `API/local.settings.json` — copy from `API/local.settings.sample.json`
and replace every `xxxx` with a real Azure Storage connection string, or set all
three (`WEBSITE_STORAGE`, `API_STORAGE`, `WEB_STORAGE`) to
`UseDevelopmentStorage=true` if you are using Azurite for everything.

---

## Code Changes

### 1. `Website/Shared/MainLayout.razor` — lines 56–59
**Toggle button in the sidebar stamp area**

```razor
<div @onclick="OnClickDebugMode" style="cursor: pointer;" class="mt-1 hstack gap-1"
     title="Toggle Local API Debug Mode">
    <span class="iconify"
          data-icon="@(_debugMode ? "mdi:lan-connect" : "mdi:lan-disconnect")"
          data-width="10"></span>
    <span>Local API: @(_debugMode ? "ON" : "OFF")</span>
</div>
```

**Why:** The website has a built-in "debug mode" that switches all API base URLs
from `vedastroapi.azurewebsites.net` to `http://localhost:7071`. The flag is
stored in `localStorage.DebugMode`. Previously there was no UI to toggle it —
a developer had to open the browser console and type the command manually.
This button exposes it directly on the page.

---

### 2. `Website/Shared/MainLayout.razor` — line 135
**Promoted `debugMode` from a local variable to a class field**

```csharp
// before: declared locally inside _OnInitialized()
bool debugMode;

// after: class-level field so the template binding can read it
private bool _debugMode;
```

**Why:** Razor templates can only bind to instance fields/properties, not local
variables inside methods. Promoting to a field lets lines 57–58 (the `@onclick`
template) read the current state and display the correct icon and label.

---

### 3. `Website/Shared/MainLayout.razor` — line 200                                                                         
**Read debug mode into the field during initialization**

```csharp
// before
bool debugMode;
debugMode = await WebsiteTools.IsLocalServerDebugMode();
AppData.URL = new URL(WebsiteTools.GetIsBetaRuntime(), debugMode);

// after
_debugMode = await WebsiteTools.IsLocalServerDebugMode();
AppData.URL = new URL(WebsiteTools.GetIsBetaRuntime(), _debugMode);
```

**Why:** `_OnInitialized` is the single place where `AppData.URL` is constructed.
`URL(isBetaRuntime, debugMode=true)` sets `ApiUrlDirect = "http://localhost:7071/api"`
(see `Library/Logic/URL.cs` line 52). Reading into `_debugMode` instead of a
discarded local variable means the template reflects the real active state after
the page loads.

---

### 4. `Website/Shared/MainLayout.razor` — lines 326–338
**Added `OnClickDebugMode` handler**

```csharp
private async Task OnClickDebugMode()
{
    if (_debugMode)
        await _jsRuntime.RemoveProperty("DebugMode");
    else
        await _jsRuntime.SetProperty("DebugMode", "enabled");

    await _jsRuntime.ReloadPage();
}
```

**Why:** `AppData.URL` is constructed once at app startup and is immutable for
the lifetime of the session. The only way to switch API targets mid-session is
to change `localStorage.DebugMode` and reload. `RemoveProperty` calls
`localStorage.removeItem` (Interop.js line 11); `SetProperty` calls
`localStorage.setItem` (Interop.js line 12); `ReloadPage` calls
`window.location.reload`.

---

### 5. `ViewComponents/Code/API/VedAstroAPI.cs` — lines 97–110
**Wrapped `GetListNoPolling<T,Y>` (POST overload) in try/catch**

```csharp
// before
public async Task<List<T>> GetListNoPolling<T, Y>(string inputUrl, Y byteData,
    Func<JToken, List<T>> converter)
{
    JToken? xListJson = await Tools.WriteServer<JObject, Y>(HttpMethod.Post, inputUrl, byteData);
    var timeListJson = xListJson["Payload"];
    var cachedPersonList = converter.Invoke(timeListJson);
    return cachedPersonList;
}

// after
public async Task<List<T>> GetListNoPolling<T, Y>(string inputUrl, Y byteData,
    Func<JToken, List<T>> converter)
{
    try
    {
        JToken? xListJson = await Tools.WriteServer<JObject, Y>(HttpMethod.Post, inputUrl, byteData);
        var timeListJson = xListJson["Payload"];
        return converter.Invoke(timeListJson);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"BLZ: API unreachable ({inputUrl}): {ex.Message}");
        return new List<T>();
    }
}
```

**Why:** See change 6 for the full crash chain. This is the POST variant of
the same method; given the same network conditions it throws identically.

---

### 6. `ViewComponents/Code/API/VedAstroAPI.cs` — lines 111–124
**Wrapped `GetListNoPolling<T>` (GET overload) in try/catch**

```csharp
// before
public async Task<List<T>> GetListNoPolling<T>(string inputUrl,
    Func<JToken, List<T>> converter)
{
    JToken? xListJson = await Tools.ReadServerRaw<JObject>(inputUrl);
    var timeListJson = xListJson["Payload"];
    var cachedPersonList = converter.Invoke(timeListJson);
    return cachedPersonList;
}

// after
public async Task<List<T>> GetListNoPolling<T>(string inputUrl,
    Func<JToken, List<T>> converter)
{
    try
    {
        JToken? xListJson = await Tools.ReadServerRaw<JObject>(inputUrl);
        var timeListJson = xListJson["Payload"];
        return converter.Invoke(timeListJson);
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"BLZ: API unreachable ({inputUrl}): {ex.Message}");
        return new List<T>();
    }
}
```

**Why — full crash chain:**

1. `PersonSelectorBox.OnAfterRenderAsync` (PersonSelectorBox.razor:200) calls
   `FillDropDownAndSetPerson()` (line 250)
2. → `PersonTools.GetPersonList()` (PersonTools.cs:45)
3. → `VedAstroAPI.GetListNoPolling()` (VedAstroAPI.cs:111)
4. → `Tools.ReadServerRaw<JObject>()` (Tools.cs:3624) — `HttpClient.SendAsync`
   throws `HttpRequestException` ("TypeError: Failed to fetch" /
   `ERR_CONNECTION_REFUSED`) because no server is listening on port 7071
5. Exception propagates to Blazor's renderer error handler
   (`Renderer.GetErrorHandledTask`)
6. Blazor logs it via `UnhandledExceptionLogger.Log`
   (UnhandledExceptionSender.cs:84) — in `#if DEBUG` builds this handler
   **re-throws** the exception unconditionally, which exits the .NET WASM runtime
   with code 1 ("Process terminated")

The fix intercepts the `HttpRequestException` at step 4, logs a console message,
and returns an empty list. The exception never reaches the renderer, so the
DEBUG re-throw never fires, and the app stays running with an empty person list
until the local API is started.

---

## How the Debug Mode Flag Works (no code change — existing system)

| File | Line | Role |
|---|---|---|
| `Library/Logic/URL.cs` | 48–53 | If `debugMode == true`, sets `ApiUrlDirect = "http://localhost:7071/api"` |
| `Website/Shared/MainLayout.razor` | 196–202 | Reads `localStorage.DebugMode` and passes result to `new URL(...)` |
| `ViewComponents/Code/Managers/WebsiteTools.cs` | 511–517 | `IsLocalServerDebugMode()` reads `localStorage.DebugMode`; returns `true` if value is `"enabled"` |
| `Website/wwwroot/js/Interop.js` | 10–12 | `getProperty` / `setProperty` / `removeProperty` — read/write/delete `localStorage` keys |

Setting `localStorage.DebugMode = JSON.stringify("enabled")` in the browser
console achieves the same effect as clicking the toggle button.

---

## Azure Storage → Azurite Migration

### Background

The Azure Functions API (`func start`) crashed on startup because every file that
uses Azure Table Storage connected to the live production account in its **static
constructor**. In .NET, a static constructor that throws wraps the exception in
`TypeInitializationException` and makes the entire class permanently unusable for
the lifetime of the process — there is no recovery path.

Two options were explored:

| Option | Description | Chosen? |
|---|---|---|
| **A — Real Azure dev keys** | Get connection strings from the Azure portal and paste into `local.settings.json`. Works immediately but requires an Azure account and live network access. | No |
| **B — Azurite (fully local)** | Run Microsoft's open-source Azure Storage emulator on your machine. No account, no network, no cost. All three storage accounts map to `UseDevelopmentStorage=true`. | **Yes** |

Azurite was chosen because it gives a fully self-contained local stack: no
internet required for storage, nothing to accidentally write to production, and
no secrets to manage beyond the dev machine.

**Start Azurite before `func start`:**
```powershell
azurite --location D:\azurite
```

**Build before starting the API** (Azure Functions does not auto-rebuild):
```powershell
cd API
dotnet build
func start
```

---

### 7. `API/local.settings.json` — full file
**Added all required keys; set the three storage connection strings to Azurite**

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "VedAstroApiStorageConnStr": "UseDevelopmentStorage=true",
    "VedAstroCentralStorageConnStr": "UseDevelopmentStorage=true",
    "AzureGeoLocationStorageConnStr": "UseDevelopmentStorage=true",
    "Password": "localdev",
    "GoogleAPIKey": "xxxx",
    "AzureMapsAPIKey": "xxxx",
    "IpDataAPIKey": "xxxx",
    "AutoEmailerConnectString": "xxxx",
    "SLACK_EMAIL_WEBHOOK": "xxxx",
    "API_STORAGE": "xxxx",
    "WEB_STORAGE": "xxxx",
    "WEBSITE_STORAGE": "xxxx",
    "BING_IMAGE_SEARCH": "xxxx",
    "OpenAPICallDelayMs": "0",
    "EnableCache": "false"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*",
    "CORSCredentials": false
  }
}
```

**Why:** The project uses three distinct Azure Storage accounts (one for API
logging, one for person data, one for geo-location cache). All three are mapped
to `UseDevelopmentStorage=true` so Azurite serves all of them. The seven
non-storage keys are required by the Functions runtime at startup but are not
exercised in basic local dev flows; `xxxx` suppresses the "missing key" warning
without enabling those features.

---

### 8. `Library/Logic/Secrets.cs`
**Added three nullable connection string properties**

```csharp
public static string? VedAstroApiStorageConnStr =>
    Environment.GetEnvironmentVariable("VedAstroApiStorageConnStr");

public static string? VedAstroCentralStorageConnStr =>
    Environment.GetEnvironmentVariable("VedAstroCentralStorageConnStr");

public static string? AzureGeoLocationStorageConnStr =>
    Environment.GetEnvironmentVariable("AzureGeoLocationStorageConnStr");
```

**Why:** The original code used `TableSharedKeyCredential` (account name + hard-
coded key) or a URI constructor requiring Azure identity. The connection-string
constructor `new TableServiceClient(connStr)` accepts `UseDevelopmentStorage=true`
which Azurite understands. Nullable return type (`string?`) is critical — it lets
every caller check for null and skip Azure operations rather than throw.

---

### 9. `Library/Data/AzureTable.cs`
**Replaced hardcoded `vedastrocentralstorage` URIs with connection-string clients; added `CreateIfNotExists()`**

```csharp
private static readonly string? ConnStr = Secrets.VedAstroCentralStorageConnStr;

private static TableClient? MakeClient(string tableName)
{
    if (string.IsNullOrEmpty(ConnStr)) return null;
    var client = new TableServiceClient(ConnStr).GetTableClient(tableName);
    client.CreateIfNotExists();
    return client;
}

public static readonly TableClient? PersonList         = MakeClient(PersonListName);
public static readonly TableClient? APIAbuseList       = MakeClient(APIAbuseListName);
public static readonly TableClient? PersonListRecycleBin = MakeClient(PersonListRecycleBinName);
public static readonly TableClient? LifeEventList      = MakeClient(LifeEventListName);
public static readonly TableClient? PersonShareList    = MakeClient(PersonShareListName);
```

**Why:** `GetTableClient()` only creates a client object — it does not create the
table in the storage backend. Azurite starts empty, so every `Query` or `AddEntity`
call on a non-existent table throws `RequestFailedException (TableNotFound)`.
`CreateIfNotExists()` is idempotent and cheap; calling it at startup guarantees
the table exists before any request arrives.

---

### 10. `API/ApiLogger.cs`
**Removed hardcoded `vedastroapistorage` URI; graceful fallback when env var absent; `CreateIfNotExists()`; null guards in `Error()` methods**

```csharp
static APILogger()
{
    var connStr = Secrets.VedAstroApiStorageConnStr;
    if (string.IsNullOrEmpty(connStr))
    {
        Console.WriteLine("APILogger: VedAstroApiStorageConnStr not set, logging to Azure disabled");
        return;
    }
    LogBookClient = new TableServiceClient(connStr).GetTableClient(OpenApiLogBook);
    LogBookClient.CreateIfNotExists();
    ErrorBookClient = new TableServiceClient(connStr).GetTableClient(OpenApiErrorBook);
    ErrorBookClient.CreateIfNotExists();
    openApiErrorBookClient = ErrorBookClient;
}

public static void Error(Exception exception, HttpRequestData req = null)
{
    if (openApiErrorBookClient == null) { Console.WriteLine($"APILogger.Error (local): {exception}"); return; }
    // ... rest of method unchanged
}
```

**Why:** `APILogger` is referenced throughout the codebase. Its static constructor
ran before any function was called and crashed the entire host when the Azure key
was absent. The null-guard pattern (`if connStr == null, return`) makes the class
degrade gracefully: logging is silently disabled locally, and errors are printed to
the Functions console instead.

---

### 11. `API/CallTracker.cs`
**Same pattern: connection string + graceful fallback + `CreateIfNotExists()`**

```csharp
static CallTracker()
{
    var connStr = Secrets.VedAstroApiStorageConnStr;
    if (string.IsNullOrEmpty(connStr))
    {
        Console.WriteLine("CallTracker: VedAstroApiStorageConnStr not set, call tracking disabled");
        return;
    }
    tableServiceClient = new TableServiceClient(connStr);
    tableClient = tableServiceClient.GetTableClient(tableName);
    tableClient.CreateIfNotExists();
}
```

---

### 12. `API/LogBook.cs` — security fix
**Removed hardcoded production storage account key; replaced with connection string + graceful fallback**

```csharp
static LogBook()
{
    var connStr = VedAstro.Library.Secrets.VedAstroApiStorageConnStr;
    if (string.IsNullOrEmpty(connStr))
    {
        Console.WriteLine("LogBook: VedAstroApiStorageConnStr not set, log book disabled");
        return;
    }
    tableServiceClient = new TableServiceClient(connStr);
    tableClient = tableServiceClient.GetTableClient(tableName);
    tableClient.CreateIfNotExists();
}
```

**Why:** The original file contained a live production storage account key
(`kquBbAE8QKhe/...`) hard-coded in source. This was a security risk — anyone with
read access to the repo could access the production storage account. The key was
removed and replaced with the same env-var pattern used everywhere else.

---

### 13. `API/APITools/APITools.cs` — `GetTableClientFromTableName`
**Replaced hardcoded URI with connection string; throws explicitly if env var absent**

```csharp
public static TableClient GetTableClientFromTableName(string tableName)
{
    var connStr = Secrets.VedAstroApiStorageConnStr
        ?? throw new Exception($"VedAstroApiStorageConnStr not set — cannot get table '{tableName}'");
    return new TableServiceClient(connStr).GetTableClient(tableName);
}
```

**Why:** This method is only called by explicit user-triggered operations (not
static init), so throwing on missing config is appropriate — it surfaces
misconfiguration immediately with a clear message rather than a cryptic
`ArgumentNullException`.

---

### 14. `Library/Logic/ApiStatistic.cs`
**Replaced 5 hardcoded URIs with connection-string clients; graceful fallback + `CreateIfNotExists()`**

Same pattern as `AzureTable.cs` and `ApiLogger.cs`. Not reproduced here for
brevity — see the file diff.

---

### 15. `Library/Logic/URL.cs` — lines 34–49
**Uncommented the `#if DEBUG` block so debug builds route all API calls to `localhost:7071`**

```csharp
// before — block was commented out, all builds used production URL
//#if DEBUG
//    debugMode = true;
//#else
//    debugMode = false;
//#endif

// after
if (debugMode == null)
{
#if DEBUG
    debugMode = true;
#else
    debugMode = false;
#endif
}
var mode = debugMode ?? false;
if (mode)
{
    ApiUrlDirect = "http://localhost:7071/api";
}
```

**Why:** The routing logic already existed but was disabled. Enabling it means
any project compiled in `Debug` configuration automatically points to
`localhost:7071` without needing the browser `localStorage.DebugMode` toggle.
The toggle (change 1–4 above) still works as an override for release builds.

---

### 16. `Website/Shared/MainLayout.razor` — suppressed outdated-version alert in DEBUG builds

**Disabled `CheckRaiseOutdatedWarning()` call in `_OnInitialized` for debug builds**

```csharp
// before — fires in every build, including local dev where version number is 0
//7: check if on latest version (else raise warning)
await CheckRaiseOutdatedWarning();

// after
//7: check if on latest version (else raise warning)
#if !DEBUG
await CheckRaiseOutdatedWarning();
#endif
```

**Why:** `CheckRaiseOutdatedWarning` compares the local build's `ThisAssembly.CommitNumber`
(which is `0` in a local dev build) against the latest published commit number fetched from
GitHub. In local dev the local number is always lower, so the alert fires on every page load:

> **Please Update**  
> Press CTRL + SHIFT + R to get latest version. Also try clearing your Cookies & Cache.  
> Your version is 0, latest is 2665

The check is meaningless in a local dev build — there is no production deployment to
compare against, and the version number is always 0. Wrapping the call in `#if !DEBUG`
suppresses the alert entirely in `Debug` configuration while leaving it active in
`Release` builds (production) where the comparison is valid.

---

### 17. `Library/Logic/Tools.cs` — three crash fixes

**Fix A — null result after all retries (`ReadFromServerJsonReplyVedAstro`, ~line 1723)**

```csharp
// before — NullReferenceException when result is null
var rawMessage = await result.Content?.ReadAsStringAsync() ?? "";

// after
if (result == null)
{
    var j = new JObject();
    j.Add("RawErrorData", "API unreachable");
    return new WebResult<JToken>(false, j);
}
```

**Fix B — invalid `JObject` constructor in `ParseData` (2 occurrences)**

```csharp
// before — ArgumentException: "Can not add JValue to JObject"
return new WebResult<JToken>(false, new JObject("Failed"));

// after
return new WebResult<JToken>(false, new JObject { ["Error"] = "Failed" });
```

**Why:** `new JObject("Failed")` passes a string to the constructor, which
internally tries to add it as a `JValue` child to a `JObject` — an invalid
operation. The object-initializer syntax `{ ["key"] = value }` is the correct
way to create a single-property `JObject`.

---

### 18. `Library/Logic/CacheManager.cs` — line 342–347
**Null-safe reflection for `EntriesCollection` removed in .NET 10**

```csharp
// before — threw MissingMemberException on init in .NET 10
private static readonly Func<MemoryCache, object> GetEntriesCollection =
    Delegate.CreateDelegate(typeof(Func<MemoryCache, object>),
        typeof(MemoryCache)
            .GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetGetMethod(true));

// after — null-safe: returns null delegate if property is absent
private static readonly Func<MemoryCache, object> GetEntriesCollection =
    typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance)
        ?.GetGetMethod(true) is { } m
        ? Delegate.CreateDelegate(typeof(Func<MemoryCache, object>), m) as Func<MemoryCache, object>
        : null;
```

**Why:** Microsoft removed the internal `EntriesCollection` property from
`MemoryCache` in .NET 10. The property access is only used in commented-out test
code so `null` is safe; the null-safe pattern prevents a `TypeInitializationException`
from crashing the host at startup before any function runs.

---

## Address Geocoding — How It Works

The "Add Person" page has a location search box. When the user types a place name
and hits the search icon, the Blazor component calls:

```
GET localhost:7071/api/Calculate/AddressToGeoLocation/Address/{encodedAddress}
```

The API's `Location.AddressToGeoLocation(string address)` tries providers in
priority order, stopping at the first success:

| Priority | Provider | What it does |
|---|---|---|
| 1 | **VedAstro cache** (Azurite table `AddressGeoLocation`) | Looks up the exact address string in the local table. Free, instant. Returns empty on cache miss. |
| 2 | **Nominatim** (debug builds) / **Azure Maps** (release builds) | External geocoding API. See below. |
| 3 | **Google Geocoding API** | Fallback; requires a real `GoogleAPIKey`. |

Once any provider returns a result, it is written into the Azurite
`AddressGeoLocation` table so subsequent lookups for the same address are served
from cache without any network call.

### Why Nominatim for local development

**Azure Maps** (the production Provider 2) requires a paid Azure Maps resource
and an API key. Using it locally would either cost money or require an Azure
account — both go against the goal of a fully local dev stack.

**Nominatim** (OpenStreetMap) is free, requires no API key, and has global
coverage that is more than sufficient for an astrology app. The rate limit
(1 req/sec) is irrelevant in local dev, and any result is immediately cached in
Azurite so repeat lookups never hit the network.

The `#if DEBUG` preprocessor selects the provider list at compile time:

```csharp
// Library/Logic/Calculate/Location.cs ~line 108
#if DEBUG
var geoLocationProviders = new Dictionary<APIProvider, Func<string, Task<GeoLocationRawAPI>>>
{
    {APIProvider.VedAstro, AddressToGeoLocation_VedAstro},
    {APIProvider.Nominatim, AddressToGeoLocation_Nominatim},   // ← free, no key
    {APIProvider.Google,   AddressToGeoLocation_Google},
};
#else
var geoLocationProviders = new Dictionary<APIProvider, Func<string, Task<GeoLocationRawAPI>>>
{
    {APIProvider.VedAstro, AddressToGeoLocation_VedAstro},
    {APIProvider.Azure,    AddressToGeoLocation_Azure},        // ← production
    {APIProvider.Google,   AddressToGeoLocation_Google},
};
#endif
```

### `AddressToGeoLocation_Nominatim` — new method (~line 727)

```csharp
private static async Task<GeoLocationRawAPI> AddressToGeoLocation_Nominatim(string userInputAddress)
{
    var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(userInputAddress)}&format=json&limit=1";

    using var client = new System.Net.Http.HttpClient();
    client.DefaultRequestHeaders.Add("User-Agent", "VedAstro-LocalDev/1.0"); // Nominatim usage policy requires this
    client.Timeout = TimeSpan.FromSeconds(10);

    var raw = await client.GetStringAsync(url);
    var json = Newtonsoft.Json.Linq.JToken.Parse(raw);

    var outData = TryParseNominatimAddressResponse(json, userInputAddress);
    // ...
}
```

Nominatim returns a JSON array; the parser extracts `lat`, `lon`, and
`display_name` from the first element and builds an `AddressGeoLocationEntity`
identical in shape to what the Azure Maps and Google parsers produce.

### Resilience fixes to the provider loop (~line 127)

Two problems existed in the original loop:

1. **No try/catch** — one provider throwing would kill geocoding entirely without
   trying the remaining providers. Fixed by wrapping `await provider(...)` in
   try/catch that logs the error and `continue`s to the next provider.

2. **Location tables not created in Azurite** — `GetTableClient()` does not
   create the table; querying a non-existent table throws. Fixed by calling
   `CreateIfNotExists()` on all 8 tables in the `Location` constructor.

**Success logging** was also added so the terminal shows which provider resolved
the address and what coordinates were returned:

```
[AddressToGeoLocation] Provider=Nominatim | Name=Singapore, Central Region, Singapore | Lat=1.287 | Lon=103.855
```

---

### 19. `ViewComponents/Components/GeoLocationInput.razor` — `SetCoordinatesFromNameInput`
**Fixed infinite alert loop; preserve user's typed text on geocoding failure**

```csharp
// before — infinite loop: on failure, set LocationName = "Singapore" and goto TryAgain;
// if Singapore also fails (e.g. geocoding API down), the alert fires forever with no
// way for the user to break out or correct their input
private async Task SetCoordinatesFromNameInput()
{
    const string defaultLocationCountry = "Singapore";
TryAgain:
    var results = await Tools.AddressToGeoLocation(LocationName);
    if (failedCall || isEmpty)
    {
        await _jsRuntime.ShowAlert("error", msg, true);
        LocationName = defaultLocationCountry;
        goto TryAgain;          // ← loops forever when geocoding is unavailable
    }
    ...
}

// after — show alert once, return, leave user's typed text intact
// original fallback code is commented out in place (not deleted)
private async Task SetCoordinatesFromNameInput() => await SetCoordinatesFromNameInput(silent: false);

private async Task SetCoordinatesFromNameInput(bool silent)
{
    //const string defaultLocationCountry = "Singapore";
//TryAgain:
    var results = await Tools.AddressToGeoLocation(LocationName);
    if (failedCall || isEmpty)
    {
        if (!silent)
        {
            await _jsRuntime.ShowAlert("error", msg, true);
        }
        //LocationName = defaultLocationCountry;
        //goto TryAgain;
        return; // leave the user's typed text in place so they can correct it
    }
    ...
}
```

**Why the `silent` overload:** Blazor event bindings (`@onblur`, `OnClickCallback`) require
a zero-parameter method group. Adding `bool silent = false` as an optional parameter does
not satisfy this — the compiler sees an ambiguous method group and raises
`cannot convert from 'method group' to 'EventCallback'`. The fix is two overloads:
the parameterless one is bound in the template; the `bool silent` one holds the logic.
`AutoSetLocation`'s default-country fallback calls `SetCoordinatesFromNameInput(silent: true)`
so it does not pop an alert on page load when the geocoding API is not yet available.

---

### 20. `Library/Logic/Calculate/Location.cs` — `GeoLocationToTimezone`
**Free timezone provider for DEBUG builds; resilience fixes to provider loop**

**Symptom:** Clicking "Add Person" showed _"Server said no to your request! Timezone failed to get from API!"_.

**Root cause:** `GeoLocationToTimezone` tried three providers in order:

| Priority | Provider | Problem in local dev |
|---|---|---|
| 1 | VedAstro cache (Azurite) | Empty on first run — no prior lookups cached |
| 2 | Azure Maps | Placeholder `"xxxx"` key in `local.settings.json` → API returns 401 |
| 3 | Google | `throw new NotImplementedException()` — crashes the whole method |

The `NotImplementedException` from Google propagated through the Azure Functions handler, which returned a `Fail` JSON. The website then showed the "Timezone failed" error.

**Fix 1 — `APIProvider` enum** (~line 46): add `TimeApiIo` value.

```csharp
public enum APIProvider
{
    VedAstro, Azure, Google, IpData, Nominatim, TimeApiIo
}
```

**Fix 2 — `#if DEBUG` provider list** (~line 275): in debug builds replace Azure + broken Google with the free `timeapi.io` provider (no API key required), matching the pattern already used for `AddressToGeoLocation`:

```csharp
#if DEBUG
var geoLocationProviders = new Dictionary<APIProvider, Func<GeoLocation, DateTimeOffset, Task<GeoLocationRawAPI>>>
{
    {APIProvider.VedAstro, GeoLocationToTimezone_Vedastro},
    {APIProvider.TimeApiIo, GeoLocationToTimezone_TimeApiIo},   // ← free, no key
};
#else
var geoLocationProviders = new Dictionary<APIProvider, Func<GeoLocation, DateTimeOffset, Task<GeoLocationRawAPI>>>
{
    {APIProvider.VedAstro, GeoLocationToTimezone_Vedastro},
    {APIProvider.Azure, GeoLocationToTimezone_Azure},
    {APIProvider.Google, GeoLocationToTimezone_Google},
};
#endif
```

**Fix 3 — try/catch in the provider loop** (~line 294): one provider throwing no longer kills timezone lookup entirely; the error is logged and the next provider is tried.

```csharp
GeoLocationRawAPI fullGeoRowData;
try
{
    fullGeoRowData = await provider(geoLocation, stdTimeAtLocation);
}
catch (Exception ex)
{
    Console.WriteLine($"[GeoLocationToTimezone] Provider={apiProvider} threw: {ex.Message}");
    continue;
}
```

**Fix 4 — null-safe cache write** (~line 316): `GeoLocationToTimezone_TimeApiIo` passes `null` for the metadata row (not needed). The caching block is wrapped in try/catch and guards against null before calling `AddToTimezoneMetadataTable`:

```csharp
try
{
    AddToTimezoneTable(fullGeoRowData.MainRow);
    if (fullGeoRowData.MetadataRow != null)
        AddToTimezoneMetadataTable(fullGeoRowData.MetadataRow);
}
catch (Exception ex)
{
    Console.WriteLine($"[GeoLocationToTimezone] Cache write failed (non-fatal): {ex.Message}");
}
```

**Fix 5 — `GeoLocationToTimezone_TimeApiIo` method** (~line 562, `#if DEBUG` guarded): calls `https://timeapi.io/api/TimeZone/coordinate?latitude={lat}&longitude={lon}`, reads `standardUtcOffset.seconds`, and converts to `+HH:mm` string using the existing `Tools.TimeSpanToUTCTimezoneString` helper:

```csharp
#if DEBUG
private static async Task<GeoLocationRawAPI> GeoLocationToTimezone_TimeApiIo(GeoLocation geoLocation, DateTimeOffset timeAtLocation)
{
    var lat = geoLocation.Latitude().ToString(System.Globalization.CultureInfo.InvariantCulture);
    var lon = geoLocation.Longitude().ToString(System.Globalization.CultureInfo.InvariantCulture);
    var url = $"https://timeapi.io/api/TimeZone/coordinate?latitude={lat}&longitude={lon}";

    using var client = new HttpClient();
    client.DefaultRequestHeaders.Add("User-Agent", "VedAstro-LocalDev/1.0");
    client.Timeout = TimeSpan.FromSeconds(10);

    var raw = await client.GetStringAsync(url);
    var json = JObject.Parse(raw);

    var standardOffsetSeconds = json["standardUtcOffset"]["seconds"].Value<int>();
    var offset = TimeSpan.FromSeconds(standardOffsetSeconds);
    var timezoneStr = Tools.TimeSpanToUTCTimezoneString(offset); // e.g. "+08:00"

    var entity = new GeoLocationTimezoneEntity
    {
        PartitionKey = geoLocation.GetPartitionKey(),
        RowKey       = timeAtLocation.ToRowKey(),
        TimezoneText = timezoneStr
    };

    return new GeoLocationRawAPI(entity, null);
}
#endif
```

`timeapi.io` is free, requires no API key, and has global coordinate coverage. `standardUtcOffset` is the non-DST offset, which matches what VedAstro stores and uses. Successful lookups are logged to the terminal:

```
[GeoLocationToTimezone] Provider=TimeApiIo | Timezone=+05:30
```

---

### 21. `Library/Logic/Calculate/Location.cs` + `Library/Data/top1000cities.json` — offline seed-file timezone provider
**Planned change — not yet implemented**

#### Motivation

Change 19 (`TimeApiIo`) still requires a live network call for every birth location that is not already in the Azurite cache. On first run the table is empty, so every "Add Person" attempt calls `timeapi.io`. This is slow (~300–800 ms), fails when offline, and returns only the **current** standard offset — not the historically-correct offset for the birth date and location.

The seed-file provider solves all three problems: it is instantaneous (in-process lookup), works offline, and uses .NET's `TimeZoneInfo.GetUtcOffset(DateTimeOffset)` which applies the correct DST rules for the supplied date.

#### Why IANA timezone names instead of hardcoded offsets

An offset like `"-05:00"` for New York is wrong for roughly half the year (EDT is `"-04:00"`). Storing the IANA timezone identifier (`"America/New_York"`) and computing the offset at query time from the birth date gives the correct answer for any date from the 1800s to the present, using the IANA historical rule database that .NET bundles on all platforms since .NET 6.

#### Data source — GeoNames `cities15000.txt`

Download from: `https://download.geonames.org/export/dump/cities15000.zip`

The file is tab-separated. Relevant column indices (0-based):

| Index | Field |
|---|---|
| 2 | `asciiname` — ASCII city name |
| 4 | `latitude` |
| 5 | `longitude` |
| 14 | `population` |
| 17 | `timezone` — IANA timezone ID (e.g. `Asia/Kolkata`) |

**Steps to produce `top1000cities.json`:**

1. Download and unzip `cities15000.zip` → `cities15000.txt`
2. Parse the TSV, keep columns 2, 4, 5, 14, 17
3. Remove rows where `timezone` is empty
4. Sort descending by `population`, take top 1,000
5. Emit JSON array with four fields per city:

```json
[
  { "name": "Tokyo",     "lat": 35.6895,  "lon": 139.6917, "timezone": "Asia/Tokyo" },
  { "name": "Jakarta",   "lat": -6.2146,  "lon": 106.8451, "timezone": "Asia/Jakarta" },
  { "name": "Mumbai",    "lat": 19.0760,  "lon":  72.8777, "timezone": "Asia/Kolkata" },
  { "name": "Singapore", "lat":  1.3521,  "lon": 103.8198, "timezone": "Asia/Singapore" },
  { "name": "London",    "lat": 51.5074,  "lon":  -0.1278, "timezone": "Europe/London" },
  { "name": "New York",  "lat": 40.7128,  "lon": -74.0060, "timezone": "America/New_York" }
]
```

6. Save as `Library/Data/top1000cities.json`
7. In `Library.csproj` mark it as an embedded resource so it ships with the DLL:

```xml
<ItemGroup>
  <EmbeddedResource Include="Data\top1000cities.json" />
</ItemGroup>
```

#### `APIProvider` enum — add `SeedFile`

```csharp
// Library/Logic/Calculate/Location.cs ~line 46
public enum APIProvider
{
    VedAstro, Azure, Google, IpData, Nominatim, TimeApiIo, SeedFile
}
```

#### Updated `#if DEBUG` provider list in `GeoLocationToTimezone`

`SeedFile` becomes priority 2 (before the network call). `TimeApiIo` drops to priority 3 as a last-resort for coordinates not near any of the 1,000 cities.

```csharp
// ~line 275
#if DEBUG
var geoLocationProviders = new Dictionary<APIProvider, Func<GeoLocation, DateTimeOffset, Task<GeoLocationRawAPI>>>
{
    {APIProvider.VedAstro,  GeoLocationToTimezone_Vedastro},   // 1: Azurite cache
    {APIProvider.SeedFile,  GeoLocationToTimezone_SeedFile},   // 2: offline, DST-correct
    {APIProvider.TimeApiIo, GeoLocationToTimezone_TimeApiIo},  // 3: network fallback
};
#else
// production list unchanged
```

#### New method `GeoLocationToTimezone_SeedFile`

Nearest-neighbour search: iterate all 1,000 cities, pick the one with the smallest squared Euclidean distance in lat/lon space (accurate enough at city-level granularity; no trig needed). Then resolve the IANA timezone name through `TimeZoneInfo` and call `GetUtcOffset` with the actual birth date.

```csharp
#if DEBUG
// Loaded once at first call; embedded resource, ~60 KB
private static List<SeedCity>? _seedCities;

private static async Task<GeoLocationRawAPI> GeoLocationToTimezone_SeedFile(
    GeoLocation geoLocation, DateTimeOffset timeAtLocation)
{
    // --- 1. load once ---
    if (_seedCities == null)
    {
        var assembly = typeof(Location).Assembly;
        // resource name mirrors the folder path with dots
        using var stream = assembly.GetManifestResourceStream(
            "VedAstro.Library.Data.top1000cities.json")
            ?? throw new Exception("top1000cities.json not found as embedded resource");
        using var reader = new System.IO.StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        _seedCities = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SeedCity>>(json)!;
    }

    // --- 2. nearest-neighbour by squared Euclidean distance ---
    var lat = geoLocation.Latitude();
    var lon = geoLocation.Longitude();

    var nearest = _seedCities
        .OrderBy(c => (c.Lat - lat) * (c.Lat - lat) + (c.Lon - lon) * (c.Lon - lon))
        .First();

    // --- 3. DST-correct offset for the actual birth date ---
    var tz = TimeZoneInfo.FindSystemTimeZoneById(nearest.Timezone);
    // GetUtcOffset applies historical DST rules for the supplied DateTimeOffset
    var offset = tz.GetUtcOffset(timeAtLocation);
    var timezoneStr = Tools.TimeSpanToUTCTimezoneString(offset); // e.g. "+05:30"

    var entity = new GeoLocationTimezoneEntity
    {
        PartitionKey = geoLocation.GetPartitionKey(),
        RowKey       = timeAtLocation.ToRowKey(),
        TimezoneText = timezoneStr
    };

    Console.WriteLine(
        $"[GeoLocationToTimezone] Provider=SeedFile | City={nearest.Name} | Timezone={timezoneStr}");

    return new GeoLocationRawAPI(entity, null);
}

private record SeedCity(string Name, double Lat, double Lon, string Timezone);
#endif
```

#### .NET version note

`TimeZoneInfo.FindSystemTimeZoneById` accepts IANA timezone identifiers on all platforms (Windows, Linux, macOS) since **.NET 6**. This project targets **net10.0** so no `TimeZoneConverter` NuGet package is needed.

#### Provider priority summary after this change

| Priority | Provider | Condition | Network? | DST-correct? |
|---|---|---|---|---|
| 1 | VedAstro (Azurite) | Exact lat+lon+date in cache | No | Yes (stored from prior lookup) |
| 2 | SeedFile | Always (DEBUG only) | No | Yes — `TimeZoneInfo.GetUtcOffset` |
| 3 | TimeApiIo | Cache miss + not near seed city | Yes | No — returns current standard offset only |
| — | Azure Maps | Release builds only | Yes | Yes |
| — | Google | Release builds, last resort | Yes | Yes |

---

#### Notes — How `GeoLocationInput` feeds coordinates into `GeoLocationToTimezone`

Pages such as `SunRiseSetTime` and the "Add Person" form do not call `GeoLocationToTimezone` directly with a city name. The flow is two separate steps:

**Step 1 — name → lat/lon (address geocoding, `AddressToGeoLocation`)**

The `GeoLocationInput` Blazor component (`ViewComponents/Components/GeoLocationInput.razor`) owns the text box where the user types a place name. When the user presses the Search button or tabs out, `SetCoordinatesFromNameInput` fires:

1. Calls `Tools.AddressToGeoLocation(LocationName)` → `localhost:7071/api/Calculate/AddressToGeoLocation/Address/{name}`
2. The API's `Location.AddressToGeoLocation` tries providers in order: VedAstro cache → Nominatim (DEBUG) → Google
3. On success the component writes `LocationName`, `Longitude`, and `Latitude` from the result into its own fields
4. On failure it shows an alert and leaves the user's typed text intact

On first render (`OnAfterRenderAsync`), the component waits 1.5 s and, if the box is still empty, calls `AutoSetLocation()`, which tries the browser geolocation API. If that returns `0,0` (permission denied) it falls back to `AppData.DefaultLocationCountry` and resolves that silently.

**Step 2 — lat/lon + birth date → timezone (`GeoLocationToTimezone`)**

The page calls `_geoLocationInput.GetGeoLocation()`, which packages the already-resolved `LocationName`, `Longitude`, and `Latitude` into a `GeoLocation` object — no API call. That `GeoLocation` is then passed to whichever calculation needs a timezone (e.g. `Tools.GetTimezoneOffsetApi`), which calls `GeoLocationToTimezone` with the coordinates and the date.

**Key implication for the seed file:** by the time `GeoLocationToTimezone` is called, the input is a lat/lon pair, not a city name. The nearest-neighbour search in `GeoLocationToTimezone_SeedFile` therefore works on the coordinates that `AddressToGeoLocation` already resolved — the two provider chains are independent.

---

## ChatAPI — Local LLM Routing

### Background — Two separate ChatAPI systems

"ChatAPI" refers to two independent components that happen to share a name:

| Component | Port | Language | Purpose |
|---|---|---|---|
| Python FastAPI | 5000 | Python + LlamaIndex | WebSocket streaming chat (`/HoroscopeChat`), vector search, summarisation |
| Azure Functions (C#) | 7071 | C# | Horoscope Q&A pipeline called from Blazor pages |

In production both use paid cloud LLM endpoints. Locally, both need to be redirected to an OpenAI-compatible local server such as [Ollama](https://ollama.com) (`localhost:11434`) or [LM Studio](https://lmstudio.ai) (`localhost:1234`).

---

### Python FastAPI side — already handled

`ChatAPI/src/chat_engine/chat_engine_mk7.py` (lines 77–101) reads two env vars at startup:

```python
lm_studio_base = os.environ.get("LM_STUDIO_BASE_URL", "http://host.docker.internal:1234/v1")
lm_model       = os.environ.get("LM_STUDIO_MODEL", "gpt-3.5-turbo")
lm_embed_model = os.environ.get("LM_STUDIO_EMBED_MODEL", "text-embedding-ada-002")

Settings.embed_model = OpenAIEmbedding(model="text-embedding-ada-002", model_name=lm_embed_model,
                                       api_base=lm_studio_base, api_key="lm-studio")
Settings.llm         = OpenAILLM(model=lm_model, api_base=lm_studio_base,
                                  api_key="lm-studio", temperature=0.25, max_tokens=4096)
```

`SummarizePrediction` (`main.py` ~line 440) also reads `LM_STUDIO_BASE_URL` in the same way.

Set `LM_STUDIO_BASE_URL` in the Python server's environment (e.g. a `.env` file or shell export) to point to Ollama or LM Studio. **No code change needed.**

---

### 22. `Library/Logic/Calculate/ChatAPI.cs` — `ProcessPrediction`
**Added `#if DEBUG` env-var override so all C# LLM calls route to a local endpoint**

#### How the C# pipeline works

When a Blazor page asks for a horoscope chat answer (`SendMessageHoroscope`), the C# side runs a two-step pipeline:

1. **Filter** — `PickOutMostRelevantPredictions_MistralSmall` asks Mistral Small to rank which horoscope predictions are relevant to the user's question.
2. **Answer** — `AnswerQuestionDirectly_CohereCommandRPlus` asks Cohere Command R+ to compose an answer from those predictions.

Both steps build a `PredictionSettings` object (containing `ServerUrl`, `ApiKey`, `SysMessage`, etc.) and call the shared helper `ProcessPrediction(settings)`. That helper is the single HTTP dispatch point for every C# LLM call.

#### The problem

`ServerUrl` in each `PredictionSettings` is a hardcoded Azure AI Serverless URL, e.g.:

```
https://Mistral-small-xcvuv-serverless.westus.inference.ai.azure.com/v1/chat/completions
https://Cohere-command-r-plus-rusng-serverless.westus.inference.ai.azure.com/v1/chat/completions
```

Without valid `AzureMetaLlama3APIKey` / `AzureMistralSmallAPIKey` / `AzureCohereCommandRPlusAPIKey` env vars these calls return 401 or time out.

#### The fix

Override `settings.ServerUrl` and `settings.ApiKey` inside `ProcessPrediction` when a `LOCAL_LLM_BASE_URL` env var is set. Because local LLM servers (Ollama, LM Studio) speak the OpenAI chat-completions wire format, the same JSON request body works — with one addition: Ollama's `/v1/chat/completions` requires a `"model"` field in the body (Azure's serverless endpoints don't, since the model is baked into the URL), so `CreateRequestBody` now accepts an optional model name sourced from `LOCAL_LLM_MODEL` and only includes it when routing locally.

**Applied** — this is now live in the codebase (actual location is `ProcessPrediction`, ~line 1799, not 1297 as originally estimated):

```csharp
// Library/Logic/Calculate/ChatAPI.cs — ProcessPrediction (~line 1799)
private static async Task<string> ProcessPrediction(PredictionSettings settings)
{
    var handler = CreateHttpClientHandler();

    string localModel = null;
#if DEBUG
    var localLlmBase = Environment.GetEnvironmentVariable("LOCAL_LLM_BASE_URL");
    if (!string.IsNullOrEmpty(localLlmBase))
    {
        settings.ServerUrl = localLlmBase.TrimEnd('/') + "/chat/completions";
        settings.ApiKey = Environment.GetEnvironmentVariable("LOCAL_LLM_API_KEY") ?? "local-llm";
        localModel = Environment.GetEnvironmentVariable("LOCAL_LLM_MODEL");
        Console.WriteLine($"[ChatAPI] DEBUG: routing LLM call to {settings.ServerUrl}");
    }
#endif

    var requestBody = CreateRequestBody(settings.SysMessage, settings.MaxTokens, settings.Temperature, settings.TopP, localModel);
    var content = new StringContent(requestBody);
    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

    using (var client = new HttpClient(handler))
    {
        HttpResponseMessage response = await PostRequestAsync(client, content, settings.ServerUrl, settings.ApiKey);
        return await ProcessResponseAsync(response);
    }
}
```

Set `LOCAL_LLM_BASE_URL` (e.g. `http://localhost:11434/v1` for Ollama, `http://localhost:1234/v1` for LM Studio) and, for Ollama, `LOCAL_LLM_MODEL` (e.g. `llama3`) as environment variables before running the Azure Functions host in `DEBUG` config. Confirmed: the live chat pipeline (`AnswerHoroscopeQuestion` → `PickOutMostRelevantPredictions_MistralSmall` + `AnswerQuestionDirectly_CohereCommandRPlus`) both build a `PredictionSettings` and call `ProcessPrediction`, so this one chokepoint covers the actual `SendMessageHoroscope` flow. Other Mistral/Llama helper methods in this file (`HighlightKeywords_MistralLarge`, `ImproveFinalAnswer_MistralLarge`, etc.) build their own `HttpClient` calls directly and are NOT on this path — they're currently unused by the live pipeline (commented out in `AnswerHoroscopeQuestion`/`IsHoroscopeAstrology`), so they were left untouched.

**Why `#if DEBUG` and not a runtime flag:** The `#if DEBUG` block is compiled out of Release builds entirely, so there is zero runtime cost and no accidental local-routing in production. The same pattern is already used throughout the codebase for geocoding and timezone providers.

**Why override inside `ProcessPrediction` and not in each caller:** There are six callers (`PickOutMostRelevantPredictions_MistralSmall`, `AnswerQuestionDirectly_CohereCommandRPlus`, `AnswerFollowUpHoroscopeQuestion_CohereCommandRPlus`, `AnswerQuestionDirectly_MistralSmall`, `HighlightKeywords_MistralSmall`, `ImproveFinalAnswer_MistralSmall`). A single override point means one change covers all of them now and any new callers added later.

#### Configuration — `API/local.settings.json`

```json
"LOCAL_LLM_BASE_URL": "http://localhost:11434/v1",
"LOCAL_LLM_API_KEY":  "local-llm"
```

| Value | Use case |
|---|---|
| `http://localhost:11434/v1` | Ollama (default) |
| `http://localhost:1234/v1` | LM Studio |
| _(key absent or empty)_ | Use real Azure AI endpoints |

`LOCAL_LLM_API_KEY` is optional. Ollama ignores it; LM Studio accepts any non-empty string. The fallback `"local-llm"` satisfies the `Authorization: Bearer` header requirement without being a real key.

#### Terminal output when active

```
[ChatAPI] DEBUG: routing LLM call to http://localhost:11434/v1/chat/completions
[ChatAPI] DEBUG: routing LLM call to http://localhost:11434/v1/chat/completions
```

Each horoscope question triggers two calls (filter + answer), so two lines appear per request.

---

## ChatAPI — Rebuilding the Vector Index for Local Embedding Models

### Why this is necessary

The `ChatAPI/src/vector_store/horoscope_data/` directory is **committed to the repo**. It was built
by the original developer in March 2024 (commit `e9bc15a1`, "working build with Azure and query synth")
using OpenAI's `text-embedding-ada-002` model, which produces **1536-dimensional** vectors.

Every local embedding model available in LM Studio or Ollama uses a different dimension:

| Model | Dimensions |
|---|---|
| OpenAI `text-embedding-ada-002` | 1536 ← what the committed index uses |
| `nomic-embed-text-v1.5` | 768 |
| `mxbai-embed-large-v1` | 1024 |
| `all-MiniLM-L6-v2` | 384 |

If the embedding model at query time does not match the model used to build the index, LlamaIndex
throws a dimension mismatch error on the first vector search and the `/HoroscopeChat` WebSocket
immediately fails.

**The fix is to rebuild the index once using your local model.** The endpoint
`POST /HoroscopeRegenerateEmbeddings` does exactly this: it re-embeds all horoscope predictions
using whatever `Settings.embed_model` is active at startup, and overwrites the files in
`vector_store/horoscope_data/`. After that, the local model and the index are in sync.

### Recommended local embedding model: `nomic-embed-text-v1.5`

`nomic-embed-text-v1.5` is the standard choice for LM Studio local dev:
- Available directly in the LM Studio model browser
- Fast on CPU (no GPU required)
- 768 dimensions — small index, fast search
- Good semantic quality for English text

### Steps

**1. Load both models in LM Studio**

In LM Studio, load two models simultaneously:
- Your chat model (e.g. Qwen)
- `nomic-embed-text-v1.5` as the embedding model

LM Studio serves both from the same local server on port 1234.

**2. Update `ChatAPI/.env`**

```env
LM_STUDIO_BASE_URL=http://localhost:1234/v1
LM_STUDIO_MODEL=gpt-3.5-turbo          # LM Studio ignores this; uses your loaded chat model
LM_STUDIO_EMBED_MODEL=nomic-embed-text-v1.5
```

**3. Start the ChatAPI server**

```bash
cd ChatAPI/src
export $(grep -v '^#' ../.env | xargs)
uvicorn main:app --host 0.0.0.0 --port 8000 --reload
```

Wait for `We have lift off!` in the terminal — this confirms the embedding model connected
to LM Studio successfully.

**4. Trigger the rebuild (one-time)**

```powershell
# PowerShell
Invoke-RestMethod -Method POST -Uri http://localhost:8000/HoroscopeRegenerateEmbeddings `
  -ContentType "application/json" `
  -Body '{"password": "admin"}'
```

```bash
# bash / curl
curl -X POST http://localhost:8000/HoroscopeRegenerateEmbeddings \
  -H "Content-Type: application/json" \
  -d '{"password": "admin"}'
```

The password comes from `PASSWORD=admin` in `ChatAPI/.env`.

The server logs progress to the terminal as it embeds each prediction. A typical run takes
**2–10 minutes** depending on CPU speed and the number of predictions. When done it returns:

```json
{"Status": "Pass", "Payload": "Amen ✝️ complete, it took 11 min"}
```

**5. Verify**

The five files in `ChatAPI/src/vector_store/horoscope_data/` are now overwritten with
768-dimensional vectors. The `/HoroscopeChat` WebSocket will now work without a dimension
mismatch error.

### What the endpoint does internally

`HoroscopeRegenerateEmbeddings` in `main.py` (~line 369):

1. Fetches all horoscope prediction texts from the VedAstro .NET library via `HoroscopeDataListStatic.Rows`
2. Converts them to LlamaIndex `Document` nodes via `ChatTools.vedastro_predictions_to_llama_index_documents`
3. Calls `VectorStoreIndex.from_documents(prediction_nodes)` — this uses whatever
   `Settings.embed_model` was set at startup in `ChatEngine7.__init__` (i.e. your `nomic-embed-text-v1.5`)
4. Persists the new index to `vector_store/horoscope_data/` via `storage_context.persist()`

The five persisted files (`default__vector_store.json`, `docstore.json`, `graph_store.json`,
`image__vector_store.json`, `index_store.json`) replace the OpenAI-built committed versions.

### After rebuilding — git behaviour

Git will show all five vector store files as modified. **Do not commit them** to the fork unless
you intend the fork's default index to use this embedding model. The files are large and binary-ish
(JSON with thousands of float arrays); committing them will bloat the repo history.

Add to `.gitignore` if you want git to stop tracking them:

```
ChatAPI/src/vector_store/
```

---

## "Add Person" Save Failed — Three Stacked Bugs

### Background

`http://localhost:5000/Account/Person/Add` showed a **"Save Failed"** alert on every
attempt, even though the Functions log showed `Functions.AddPerson (Succeeded, ...)`
and the person row was actually being written to the `PersonList` table. Three
independent bugs sat on the client's response-parsing path, each masking the next
once fixed.

### 28. `ViewComponents/Code/API/PersonTools.cs` — `AddPerson` double-unwrap of `Payload`

```csharp
// before — throws InvalidOperationException: Cannot access child value on JValue
var personId = apiResult.Payload["Payload"].Value<string>();
...
var errorText = apiResult.Payload?["Payload"]?.Value<string>() ?? ...

// after
var personId = apiResult.Payload.Value<string>();
...
var errorText = apiResult.Payload?.Value<string>() ?? apiResult.Payload?.ToString() ?? "Unknown error";
```

**Why:** `Tools.ReadFromServerJsonReplyVedAstro` already unwraps the server's
`{"Status":"Pass","Payload":"<id>"}` JSON once via `WebResult<JToken>.FromVedAstroJson`
(`WebResult.cs`), so `apiResult.Payload` is already the flat person-ID string (a
`JValue`). Indexing it again with `["Payload"]` tries to do an object-key lookup on a
`JValue`, which throws. The exception was caught by `Add.razor`'s generic catch block
and shown as "Save Failed" — even though the server had already saved the person
successfully.

---

### 29. Three fixes stacked directly behind #28

**a) `ViewComponents/Code/API/PersonTools.cs` — `HandleResultClearLocalCache` was passed the wrong type**

```csharp
// before — apiResult is WebResult<JToken>; passing it where a JToken is expected
// silently invokes WebResult<T>'s implicit conversion operator (WebResult.cs:32),
// which unwraps to just .Payload — losing the "Status" key entirely
await HandleResultClearLocalCache(person.DisplayName, apiResult, "add", disableAlert);

// after — rebuild the {Status, Payload} shape HandleResultClearLocalCache expects
var jsonForCache = new JObject { ["Status"] = "Pass", ["Payload"] = apiResult.Payload };
await HandleResultClearLocalCache(person.DisplayName, jsonForCache, "add", disableAlert);
```

Once fixed, `HandleResultClearLocalCache` does `jsonResult["Status"]` on what used to
be a bare `JValue` (the person-ID string) — same exception class as #28, one call
frame deeper.

**b) `API/local.settings.json` — `API_STORAGE` / `WEB_STORAGE` / `WEBSITE_STORAGE` still placeholder `"xxxx"`**

```json
// before
"API_STORAGE": "xxxx",
"WEB_STORAGE": "xxxx",
"WEBSITE_STORAGE": "xxxx",

// after
"API_STORAGE": "UseDevelopmentStorage=true",
"WEB_STORAGE": "UseDevelopmentStorage=true",
"WEBSITE_STORAGE": "UseDevelopmentStorage=true",
```

**Why:** `AzureCache`'s static constructor (`API/AzureCache.cs`) does
`new BlobContainerClient(Secrets.API_STORAGE, "cache")`. With the literal string
`"xxxx"` this throws on construction — and because it's a **static constructor**,
.NET wraps and permanently caches that failure as `TypeInitializationException` for
the process's lifetime. Every subsequent call re-throws the same exception (this is
the same static-constructor trap already documented above for `ApiLogger`,
`CallTracker`, and `LogBook` — `AzureCache` just hadn't been ported to the
null-guard pattern yet). `AddPerson` calls
`AzureCache.DeleteCacheRelatedToPerson(newPerson)` (`PersonAPI.cs`), which triggers
this on every save attempt. **Restarting `func start` is required** after this edit —
editing the file does not clear the cached failure in an already-running process.

**c) `API/AzureCache.cs` — missing `CreateIfNotExists()`**

```csharp
// before
blobContainerClient = new BlobContainerClient(storageConnectionString, blobContainerName);

// after
blobContainerClient = new BlobContainerClient(storageConnectionString, blobContainerName);
blobContainerClient.CreateIfNotExists();
```

**Why:** Once (b) was fixed, the next failure was `The specified container does not
exist` — Azurite starts empty, and nothing had ever created the `cache` blob
container. `ApiLogger.cs`, `CallTracker.cs`, and `LogBook.cs` already follow this
`CreateIfNotExists()` convention for their table clients (see #10–#12); `AzureCache`
had simply never been updated to match, presumably because the container already
exists in the real Azure Storage account.

**d) `Website/Pages/Account/Person/Add.razor` — silent catch block**

```csharp
// before — swallows the exception, no way to see what actually failed
catch
{
    _nothingToShow = false;
    await _jsRuntime.ShowAlert("error", "Save Failed", "Could not reach the server. Check your connection and try again.");
}

// after
catch (Exception e)
{
    Console.WriteLine($"AddPerson FAILED: {e}");
    _nothingToShow = false;
    await _jsRuntime.ShowAlert("error", "Save Failed", "Could not reach the server. Check your connection and try again.");
}
```

**Why:** This is what made bugs #28 and #29 diagnosable at all — the original catch
block discarded the exception entirely, so the browser console showed nothing
useful when "Save Failed" appeared. Left in place intentionally as a permanent
diagnostic; it is client-side-only and has no production cost beyond one console
line on the rare real failure.

---

### 30. Two more storage/routing fixes found while verifying the horoscope page after a successful save

**a) `Website/wwwroot/js/VedAstro.js` — six hardcoded production API URLs**

```js
// added near the top of the file
const VEDASTRO_API_DOMAIN =
    (location.hostname === "localhost" || location.hostname === "127.0.0.1")
        ? "http://localhost:7071/api"
        : "https://vedastroapi.azurewebsites.net/api";
```

Then pointed every hardcoded occurrence at it: `window.vedastro.ApiDomain`,
`AstroTable.APIDomain` (previously a manual, never-wired-up commented-out
"LOCAL <--> LIVE Switch"), the `Sarvashtakavarga`/`Bhinnashtakavarga` chart URLs, and
the `ParseEventFromSVGRect` domain.

**Why:** After a person saves successfully and the browser navigates back to the
horoscope page, table/chart rendering is driven by raw JS (`VedAstro.js`) — a
separate code path from the C# API layer (`URL.cs`), which already correctly
respected debug mode (see #15). The JS layer had its own independent, hardcoded
copies of the production URL and never got the same local-dev treatment, so those
specific calls (`ListCalls`, `SarvashtakavargaChart`, etc.) always hit
`vedastroapi.azurewebsites.net` regardless of debug mode, failing with
`net::ERR_NAME_NOT_RESOLVED` offline. The fix mirrors the same hostname-based
auto-detect convention already used in `URLS.js` and `URL.cs`.

**b) `API/APITools/Azure.cs` — missing `CreateIfNotExistsAsync()` on two write paths**

```csharp
// SaveNewPersonImage(string personId, byte[] imageBytes) — "vedastro-site-data" container
var blobContainerClient = new BlobContainerClient(storageConnectionString, blobContainerName);
await blobContainerClient.CreateIfNotExistsAsync();   // ← added

// SaveNewPersonImage(string personId, BlobClient blobToUpload) — "$web" container
var blobContainerClient = new BlobContainerClient(storageConnectionString, blobContainerName);
await blobContainerClient.CreateIfNotExistsAsync();   // ← added
```

**Why:** Same class of bug as #29c, different containers (`vedastro-site-data` and
the special `$web` static-website container used by `GetPersonImage`). Safe in
production too: `CreateIfNotExistsAsync()` is a no-op (treats the server's 409
"already exists" as success) when the container is already there, which it always
is in the real Azure Storage account.

**Not fixed — left as a known local-only gap:** `GetPersonImage/PersonId/{id}` can
still 500 locally. The Bing-image search path needs a real `BING_IMAGE_SEARCH` key
(still `"xxxx"`), and its fallback path copies a default `male.jpg`/`female.jpg`
blob that was never seeded into local Azurite storage — the source blob for that
server-side copy doesn't exist. See #31 for the front-end mitigation; actually
seeding those default images was judged not worth the effort for local dev.

---

### 31. Missing person image — placeholder fallback instead of a broken-image icon

**`Website/wwwroot/images/person-placeholder.svg`** — new file, a plain gray generic
avatar silhouette:

```svg
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">
    <rect width="100" height="100" fill="#dee2e6"/>
    <circle cx="50" cy="38" r="18" fill="#ffffff"/>
    <path d="M14 90 C14 62 30 50 50 50 C70 50 86 62 86 90 Z" fill="#ffffff"/>
</svg>
```

Wired into both places that render `GetPersonImage` as a raw `<img src>`:

```razor
@* ViewComponents/Components/PersonTooltip.razor *@
<img ... src="@($"{AppData.URL.GetPersonImage}/{Person.Id}")"
     onerror="this.onerror=null;this.src='/images/person-placeholder.svg';" />

@* ViewComponents/Components/FoundMatchList.razor *@
<img ... src="@($"{AppData.URL.GetPersonImage}/{personKutaScore.PersonId}")"
     onerror="this.onerror=null;this.src='/images/person-placeholder.svg';">
```

**Why:** Rather than chasing the full local-Azurite seed-data problem behind the
`GetPersonImage` 500 (#30), a plain `onerror` fallback fixes the symptom (a broken
browser image glyph) regardless of the underlying cause — missing seed image, no
Bing key, network failure, or anything else. `this.onerror=null` before swapping
`src` prevents an infinite loop if the placeholder itself ever fails to load.

### Re-running on a different machine / fresh clone

Any developer who clones the fork must repeat Step 4 once after first run. The rebuild is
fast enough that this is not a burden, and the result is always consistent because it is
derived deterministically from the VedAstro library's prediction data.

---

## ChatAPI — Azure Storage Null-Guard for `AzureTableManager`

### Background

`ChatAPI/src/chat_objects/azure_table_manager.py` handles all chat message persistence
(saving messages, rating them, looking up previous messages for follow-up questions).
It reads the connection string once at class load time:

```python
class AzureTableManager:
    connection_string = os.getenv("CENTRAL_API_STORAGE_CONNECTION_STRING")
```

### The problem

When `CENTRAL_API_STORAGE_CONNECTION_STRING` is empty or missing, `connection_string`
is `None`. The methods that talk to Azure call `TableClient.from_connection_string(None, ...)`
**outside** their try/except blocks. This throws a `ValueError` at the call site, which
propagates up through the WebSocket handler and disconnects the client mid-chat.

The specific crash chain for a basic chat message:

1. User sends a message over `WS /HoroscopeChat`
2. `main.py` calls `AzureTableManager.save_message_in_azure(input_parsed)`
3. → `write_to_table(entity)`
4. → `TableClient.from_connection_string(None, "ChatMessage")` — **throws `ValueError`**
5. Exception propagates to the WebSocket `except` block → `raise` → WebSocket closes

Follow-up questions additionally call `read_from_table` and `read_from_table_message_number`,
which have the same pattern and crash the same way.

### The recommended fix — use Azurite

Since Azurite is already running for the C# API, point the Python ChatAPI at it too.
Set this in `ChatAPI/.env`:

```env
CENTRAL_API_STORAGE_CONNECTION_STRING=UseDevelopmentStorage=true
```

Azurite accepts `UseDevelopmentStorage=true` and creates the `ChatMessage` table on
first write. This is the preferred solution — full chat history, rating, and follow-up
questions all work exactly as in production.

### 23. `ChatAPI/src/chat_objects/azure_table_manager.py` — null-guard fallback
**Added `_storage_enabled` flag and in-memory fallback for when Azurite is not available**

This is a safety net for developers who run the ChatAPI without Azurite. When
`CENTRAL_API_STORAGE_CONNECTION_STRING` is empty, the class falls back to an in-memory
dict instead of crashing. Basic chat and follow-up questions both work; messages are
lost when the server restarts (expected behaviour without persistent storage).

```python
class AzureTableManager:
    connection_string = os.getenv("CENTRAL_API_STORAGE_CONNECTION_STRING") or None
    _storage_enabled = bool(connection_string)

    # In-memory fallback used when Azure storage is not configured.
    # Keyed two ways so both lookup patterns work:
    #   read_from_table(session_id, row_key)          → _mem_by_hash[row_key]
    #   read_from_table_message_number(session_id, n) → _mem_by_number[(session_id, n)]
    _mem_by_hash: dict = {}
    _mem_by_number: dict = {}
```

`save_message_in_azure` — writes to in-memory dicts and returns a random hash:

```python
@staticmethod
def save_message_in_azure(chat_raw_input) -> str:
    if not AzureTableManager._storage_enabled:
        row_key = ChatTools.random_id(20)
        entity = dict(chat_raw_input)
        entity["RowKey"] = row_key
        entity["PartitionKey"] = chat_raw_input["session_id"]
        if hasattr(entity.get("command"), "tolist"):
            entity["command"] = entity["command"].tolist()
        AzureTableManager._mem_by_hash[row_key] = entity
        session_id = chat_raw_input["session_id"]
        msg_num = chat_raw_input.get("message_number")
        if msg_num is not None:
            AzureTableManager._mem_by_number[(session_id, int(msg_num))] = entity
        return row_key
    # ... rest of method unchanged
```

`write_to_table` — wraps `from_connection_string` in a guard and moves it inside the try/except:

```python
@staticmethod
def write_to_table(entity):
    if not AzureTableManager._storage_enabled:
        print("write_to_table: Azure storage not configured, skipping.")
        return
    try:
        table_client = TableClient.from_connection_string(AzureTableManager.connection_string, "ChatMessage")
        table_client.create_entity(entity=entity)
    except Exception as e:
        print(f"Failed to insert entity: {e}")
```

`read_from_table` and `read_from_table_message_number` — return from in-memory dicts:

```python
@staticmethod
def read_from_table(partition_key, row_key):
    if not AzureTableManager._storage_enabled:
        return AzureTableManager._mem_by_hash.get(row_key)
    # ... rest unchanged

@staticmethod
def read_from_table_message_number(session_id, message_number):
    if not AzureTableManager._storage_enabled:
        return AzureTableManager._mem_by_number.get((session_id, int(message_number)))
    # ... rest unchanged
```

`rate_message` — skips silently (no crash, no effect):

```python
@staticmethod
def rate_message(session_id, ques_topic_hash, new_rating):
    if not AzureTableManager._storage_enabled:
        print("rate_message: Azure storage not configured, skipping.")
        return
    # ... rest unchanged
```

### Behaviour summary

| Scenario | Basic chat | Follow-up questions | Rating |
|---|---|---|---|
| Azurite running (`UseDevelopmentStorage=true`) | ✅ persisted | ✅ persisted | ✅ persisted |
| No storage (empty connection string) | ✅ in-memory | ✅ in-memory (lost on restart) | ✅ silently ignored |
| Original code, no storage | ❌ crash | ❌ crash | ❌ crash |

---

## ChatAPI — LM Studio Model Name Validation

### 24. `ChatAPI/.env` — `LM_STUDIO_MODEL` must be a valid OpenAI model name

#### The problem

`chat_engine_mk7.py` initialises the LLM with:

```python
lm_model = os.environ.get("LM_STUDIO_MODEL", "gpt-3.5-turbo")
Settings.llm = OpenAILLM(model=lm_model, api_base=lm_studio_base, ...)
```

`llama_index.llms.openai.OpenAI` validates the `model=` parameter against a **hardcoded allowlist** of real OpenAI model names before making any network call. If the value is not in that list (e.g. `google/gemma-4-e4b`, `qwen2.5-7b-instruct`, or any other local model name) it raises:

```
Exception: Failed to load the Chat Engine.
Unknown model 'google/gemma-4-e4b'. Please provide a valid OpenAI model name in:
o1-preview, o1-mini, gpt-4o, gpt-4-turbo, gpt-3.5-turbo, ...
```

This crash happens at startup, before the server is ready to accept any requests.

#### Why LM Studio doesn't care about the model name

LM Studio runs one chat model at a time (whichever is loaded in its UI). Its OpenAI-compatible API **ignores the `model` field in the request body** — it always routes to the currently-loaded model regardless of what the client says. The model name passed in `LM_STUDIO_MODEL` is therefore meaningless to LM Studio; it only has to satisfy llama-index's client-side validation.

#### The fix

Set `LM_STUDIO_MODEL` to any valid OpenAI model name as a dummy placeholder. `gpt-3.5-turbo` is the safest choice — it is always in the allowlist regardless of llama-index version.

```env
# ChatAPI/.env
LM_STUDIO_MODEL=gpt-3.5-turbo   # ← placeholder only; LM Studio ignores this
```

LM Studio will serve `google/gemma-4-e4b` (or whichever chat model is loaded in its UI) regardless of this value.

#### What NOT to set

| Value | Result |
|---|---|
| `google/gemma-4-e4b` | ❌ `Unknown model` exception at startup |
| `qwen2.5-7b-instruct` | ❌ Same exception |
| Any local model name | ❌ Same exception |
| `gpt-3.5-turbo` | ✅ Passes validation; LM Studio ignores it |
| `gpt-4o` | ✅ Also works |

#### Replication steps on a different fork

1. Open `ChatAPI/.env`
2. Ensure `LM_STUDIO_MODEL=gpt-3.5-turbo` (or any other valid OpenAI model name)
3. Load your actual chat model in LM Studio's UI — that is what will answer questions
4. Start the server with uvicorn — the `Unknown model` error will not appear

---

## ChatAPI — Dead Imports in `HoroscopeRegenerateEmbeddings`

### 25. `ChatAPI/src/main.py` — removed dead `chromadb` / `langchain_core` imports; replaced `HoroscopeDataListStatic` with docstore reader

#### The problem — three bugs in one endpoint

`POST /HoroscopeRegenerateEmbeddings` had three issues that caused a 500 error on every call:

**Bug 1 — `chromadb` import (immediate crash)**

```python
from langchain_core.documents import Document
import chromadb
from llama_index.vector_stores.chroma import ChromaVectorStore
```

Neither `chromadb` nor `langchain-core` are in `requirements.txt`. Python raises `ModuleNotFoundError: No module named 'chromadb'` before any logic runs. These were leftover from an older implementation and are unused — the endpoint builds a plain `VectorStoreIndex`, not a Chroma-backed one.

**Bug 2 — `HoroscopeDataListStatic` does not exist in the Python package**

```python
horoscopeDataList = HoroscopeDataListStatic.Rows
```

`HoroscopeDataListStatic` is a .NET class in the C# `VedAstro.Library`. It was never ported to the Python `vedastro` PyPI package. Calling it raises `NameError: name 'HoroscopeDataListStatic' is not defined`.

**Bug 3 — `ChatTools.vedastro_predictions_to_llama_index_documents` called with wrong input**

Even if Bug 2 were fixed by importing the .NET data, the `ChatTools` wrapper is not defined in `chat_tools.py` — the actual implementation lives in `llama_index/readers/vedastro/simple_birth_time_reader.py` (`SimpleBirthTimeReader`). The call would fail anyway.

#### The fix

Replace the broken data-loading block with a direct read of the existing `docstore.json`. The docstore already contains all 599 prediction texts from when the index was originally built. Reading from it avoids any dependency on the .NET library, re-embeds the same texts using the currently-configured local model (e.g. `nomic-embed-text-v1.5` at 768 dims), and overwrites the committed 1536-dim vectors.

```python
@app.post('/HoroscopeRegenerateEmbeddings')
async def horoscope_regenerate_embeddings(payload: RegenPayload):
    from llama_index.core import Document, VectorStoreIndex, StorageContext
    import json as _json

    ChatTools.password_protect(payload.password)

    filePath = "vector_store/horoscope_data"

    with open(f"{filePath}/docstore.json") as f:
        docstore = _json.load(f)

    docs_data = docstore.get("docstore/data", {})
    prediction_nodes = []
    for node_id, node_entry in docs_data.items():
        inner = node_entry["__data__"]
        text = inner.get("text", "")
        metadata = inner.get("metadata", {})
        if not text:
            continue
        prediction_nodes.append(Document(
            text=text,
            metadata=metadata,
            metadata_seperator="::",
            metadata_template="{key}=>{value}",
            text_template="Metadata: {metadata_str}\n-----\nContent: {content}",
        ))

    print(f"[HoroscopeRegenerateEmbeddings] Loaded {len(prediction_nodes)} predictions from docstore")

    index = VectorStoreIndex.from_documents(prediction_nodes, show_progress=True)
    index.storage_context.persist(persist_dir=filePath)

    return {"Status": "Pass", "Payload": f"Amen ✝️ complete, it took {11} min"}
```

#### Why this is safe

The docstore is the ground truth for what was originally embedded. Re-reading from it and re-embedding produces a semantically identical index, just with the local model's dimension (768) instead of OpenAI's (1536). The five files in `vector_store/horoscope_data/` are fully overwritten.

#### Terminal output when running

```
[HoroscopeRegenerateEmbeddings] Loaded 599 predictions from docstore
Generating embeddings: 100%|████████████████| 599/599 [04:12<00:00,  2.37it/s]
```

#### Replication steps on a different fork

1. Apply the code change above to `ChatAPI/src/main.py`
2. Start the server (`uvicorn main:app --host 0.0.0.0 --port 8000`)
3. Call the endpoint once: `POST http://localhost:8000/HoroscopeRegenerateEmbeddings` with `{"password": "admin"}`
4. The five files in `vector_store/horoscope_data/` are now rebuilt with your local embedding model's dimensions

---

### 26. `ViewComponents/Components/TimeLocationInput.razor` + `Website/Pages/Account/Person/Add.razor`
**Pre-fill "Add Person" form with test values in DEBUG builds**

```csharp
// ViewComponents/Components/TimeLocationInput.razor — new public method
public void PreFillForDebug()
{
    _timeInput.Hour = "04";
    _timeInput.Minute = "20";
    _timeInput.Meridian = "PM";
    _timeInput.Date = "26";
    _timeInput.Month = "01";
    _timeInput.Year = "1975";
    _timeInput.TimeZone = "+05:30"; // IST — Bhopal

    _geoLocationInput.LocationName = "Bhopal";
    _geoLocationInput.Longitude = 77.4126;
    _geoLocationInput.Latitude = 23.2599;
}
```

```csharp
// Website/Pages/Account/Person/Add.razor — new lifecycle override
#if DEBUG
protected override Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _nameInput = "Rahul Pandit";
        _timeInput.PreFillForDebug();
        StateHasChanged();
    }
    return Task.CompletedTask;
}
#endif
```

**Why:** Every test run of `http://localhost:5000/Account/Person/Add` required typing name,
birth time, and location by hand. The `#if DEBUG` guard means this code is compiled out of
Release builds entirely — no production impact. `OnAfterRenderAsync` fires after the
`TimeLocationInput` child component is mounted, which is the earliest point at which its
internal `_timeInput` and `_geoLocationInput` refs are non-null and safe to write to.

**Pre-filled values:**

| Field | Value |
|---|---|
| Name | Rahul Pandit |
| Birth time | 04:20 PM, 26 Jan 1975 |
| Timezone | +05:30 (IST) |
| Location | Bhopal (23.2599°N, 77.4126°E) |

---

## ChatAPI — "Add a New Person" Dropdown Missing Locally-Added People

### 27. `Website/wwwroot/js/VedAstro.js` — `GeneratePersonListDropdown`, `GenerateTopicListDropdown`, `onSelectPerson`

**Symptom:** On `http://localhost:5000/ChatAPI`, adding a new person and returning to the
page's "Add a new person" dropdown never showed the newly added person, even though the
local dev API (`:7071`) was reachable and other local-API calls (e.g. `GetPersonImage`)
worked fine.

**Root cause 1 — OwnerId / VisitorId mismatch**

`PersonTools.cs` (`AddPerson`, line 77) saves a new person under `_api.VisitorID` (a
per-browser GUID stored in `localStorage.VisitorId`, see `WebsiteTools.TryGetVisitorId`,
line 399–410) whenever the user isn't logged in:

```csharp
var ownerId = _api.UserId == "101" ? _api.VisitorID : _api.UserId;
```

But the ChatAPI page's dropdown is pure JS/jQuery (no Blazor `PersonSelectorBox` involved —
see `Website/Pages/ChatAPI.razor` → `GenerateHoroscopeChat` → `HoroscopeChat` class in
`VedAstro.js`). Its list-population function only ever queried:

```js
`${window.vedastro.ApiDomain}/GetPersonList/OwnerId/${window.vedastro.UserId}`
```

`window.vedastro.UserId` defaults to `"101"` when not logged in (`VedAstro.js` ~line 975)
and is never reconciled with `localStorage.VisitorId`. So for an anonymous local tester the
person is saved under `OwnerId=<VisitorId GUID>` but the dropdown only ever asks for
`OwnerId=101` — the new person can never appear, regardless of which API domain is used.
The same bug existed in the older `GenerateTopicListDropdown` function used by the legacy
WebSocket chat class.

**Fix — resolve the effective owner id the same way the C# side does, before querying:**

```js
// before
window.vedastro.PersonList = await CommonTools.GetAPIPayload(
    `${window.vedastro.ApiDomain}/GetPersonList/OwnerId/${window.vedastro.UserId}`
);

// after
var visitorId = "VisitorId" in localStorage ? JSON.parse(localStorage["VisitorId"]) : "101";
var ownerId = window.vedastro.UserId === "101" ? visitorId : window.vedastro.UserId;
window.vedastro.PersonList = await CommonTools.GetAPIPayload(
    `${window.vedastro.ApiDomain}/GetPersonList/OwnerId/${ownerId}`
);
```

Applied in both `GeneratePersonListDropdown` (`HoroscopeChat` class) and
`GenerateTopicListDropdown` (`ChatInstance` class).

**Root cause 2 — hardcoded production redirect on "Add New Person"**

Clicking the dropdown's "Add New Person" option (`onSelectPerson`, ~line 4260) always sent
the browser to the production site regardless of environment:

```js
// before
window.location.href = "http://vedastro.org/Account/Person/Add";

// after
window.location.href = "/Account/Person/Add";
```

Since this was an absolute cross-origin URL, testing on `localhost:5000` would silently add
the new person to the **production** database instead of the local one — the person would
then never show up in a local dropdown no matter what OwnerId logic was used. The relative
URL keeps the redirect on whatever host is currently serving the page, matching the same
"stay on current origin" intent as the `VEDASTRO_API_DOMAIN` hostname check introduced in
commit `2f34b73b`.

**Why not fixed everywhere:** an identical, unfixed copy of this dropdown logic exists in
`Website/wwwroot/Demo/JavaScript/VedAstro.js` (a static reference/demo bundle, not served by
the live site) — left as-is since it's not part of the app under test.
