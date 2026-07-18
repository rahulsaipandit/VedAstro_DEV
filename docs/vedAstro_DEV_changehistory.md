# VedAstro_DEV Change History

## 2026-07-18 (continued) — Restored the entire "Horoscope Predictions" feature (443 calculators recovered from disconnected git history)

### Context

While chasing the AM/PM time-input report, the user's browser console showed the Horoscope
page failing with:
```
System.TypeInitializationException: The type initializer for 'VedAstro.Library.HoroscopeDataListStatic' threw an exception.
 ---> System.Exception: Calculator method not found! : SunAshtakavargaYoga2
```
This is the same "permanent static-constructor crash" pattern as the `AzureTable`/`AzureCache`/
`CacheManager` bugs earlier in this session, except the underlying cause here was much larger:
not a missing null-check, but a genuinely missing implementation.

### Investigation

`HoroscopeDataListStatic.cs` is a 490-entry static table (one per Vedic astrology "Yoga",
e.g. `GajakesariYoga`, `SunAshtakavargaYoga2`). Each entry's constructor calls
`EventManager.GetHoroscopeCalculatorMethod(HoroscopeName.X)`, which reflects over a class
called `CalculateHoroscope` looking for a method tagged `[HoroscopeCalculator(HoroscopeName.X)]`.
Because this happens inside a `static List<HoroscopeData> Rows = new(){ ... }` field
initializer, the *first* lookup failure poisons the whole class for the rest of the process.

Checked `Library/Logic/Calculate/CalculateHoroscope.cs`: it had exactly **2 methods**
(`MarsVenusIn7th`, `MercuryOrJupiterIn7th` — unrelated match-making helpers used by
`MatchReportFactory.cs`), and **zero** `[HoroscopeCalculator]`-tagged methods anywhere in the
codebase. All 490 lookups were doomed to fail.

`git show 71a6054d` (the prior session's own commit, "Get VEDASTRO_DEV build to compile
locally") explained why: it documented reconstructing ~120 missing `Calculate.*` methods from
scratch after discovering the *original* implementation was never committed to `master`'s
reachable history, and explicitly said `CalculateHoroscope.cs` was written from scratch to
cover only `MatchReportFactory.cs`'s 2 calls — the ~490-entry Horoscope-Yoga engine was out of
scope for that work and was never addressed.

### The recovery

The user asked "is it not there in docs/?" This turned out to be the right question:

1. `docs/Library/Logic/Calculate/Horoscope.cs` (2441 lines) contains a full `CalculateHoroscope`
   class with **443 real `[HoroscopeCalculator]`-tagged methods** — genuine Vedic astrology
   logic (Ashtakavarga bindus, house-lord placements, classical Yoga combinations), built on
   top of `Calculate.*` primitives that already exist in the current codebase.
2. Traced its provenance with `git log --all -S "public class CalculateHoroscope"`: found it in
   commit `9c7a4b1a` ("Gonzo bUIDLABLE", 2024-06-06) — a commit that **still exists in the local
   git object database** (`git show` can read it) but is **not an ancestor of current HEAD**
   (`git merge-base --is-ancestor` confirms) - i.e. a disconnected/orphaned branch tip, not
   reachable via normal `git log`. `diff` against the `docs/` copy came back byte-for-byte
   identical (0 lines), confirming `docs/` is a checkout of this exact dangling commit, and
   validating every other `docs/`-vs-current comparison made earlier in this session.

### Fix 1 — Restored `Library/Logic/Calculate/CalculateHoroscope.cs` wholesale

Replaced the 2-method stub with the full 2441-line/443-method version from
`git show 9c7a4b1a:Library/Logic/Calculate/Horoscope.cs`. Verified it doesn't drop the 2
existing methods `MatchReportFactory.cs` depends on - the restored file already contains both,
tagged with `[HoroscopeCalculator]` too.

Tried building immediately to see what, if anything, had drifted since 2024-06-06:

**Enum gap (3 errors)** - the restored file references 3 legacy names
(`HoroscopeName.MoonAshtakavargaYoga`, `MoonAshtakavargaYoga2`, `MoonAshtakavargaYoga3`) that
don't exist in the current `HoroscopeName` enum, which has since been split into
`MoonAshtakavargaYoga1A`/`1B`/`2B` etc. Fixed by **adding** the 3 legacy names as new enum
members in `Library/Data/Enum/HoroscopeName.cs` (additive only - didn't rename or remove
anything the current `HoroscopeDataListStatic.cs` relies on).

**Missing helper methods (197 errors, only 5 distinct methods)** - `Calculate` was missing
`HousePlanetOccupies`, `IsHouseLordInHouse`, `IsAnyPlanetInHouse`, `IsHouseSignName`,
`IsPlanetMalefic`. All 5 existed verbatim in the same historical `Calculate.cs`
(`docs/Library/Logic/Calculate/Calculate.cs`) and depend only on primitives that already exist
in the current, previously-reconstructed `Core.cs`/`CoreSigns.cs`/`CoreRelationships.cs` - with
exactly one rename to account for (`AllHouseMiddleLongitudes` → `AllHouseLongitudes`, confirmed
by finding the identical cusp-calculation logic already present under the new name in
`Core.cs:3548`). Ported all 5 verbatim (one name substitution) into
`Library/Logic/Calculate/CoreRelationships.cs`.

After these two small additions: **0 compile errors** for the full 443-method file.

### Fix 2 — `Calculate.HoroscopePredictions` entry point was also never restored

Runtime testing (`GET /api/Calculate/HoroscopePredictions/...`) still failed with
`"Calculator method not found!"` - a *different* error, from `Tools.MethodNameToMethodInfo`,
because the generic API dispatcher couldn't find a method literally named `HoroscopePredictions`
on the `Calculate` class at all (unrelated to the 443 calculators above - this is the single
entry-point method that iterates `HoroscopeDataListStatic.Rows`). It was a thin wrapper over
`Tools.GetHoroscopePrediction`, which *does* already exist in current `Tools.cs`. Restored the
missing wrapper (plus its sibling `HoroscopePredictionNames`, used by the AI chat feature) into
`CoreRelationships.cs`, verbatim from the historical `Calculate.cs`.

### Fix 3 — Graceful degradation for the part that's genuinely still missing

Comparing the 490 entries `HoroscopeDataListStatic.cs` currently expects against the 443 restored
methods left a **51-entry gap**, all in one family: the numbered "Ashtakavarga Yoga" predictions
(`SunAshtakavargaYoga2..11`, `MoonAshtakavargaYoga1A/1B/2B/4/6..10`, `MarsAshtakavargaYoga2..25`,
`MercuryAshtakavargaYoga3..12B` - per the file's own comment, "yoga's from BV Raman's
Ashtakavarga System Book"). Checked: **zero** of these 51 have an implementation anywhere,
including in the recovered historical branch - this specific family appears to have never been
written in this codebase's entire git history (reachable or not). Writing 51 classical
astrology rules from scratch is real domain-authoring work, out of scope for this session.

Rather than leave the whole feature permanently crashable by this known-incomplete 51-entry
family, made the lookup degrade gracefully instead of poisoning the entire static class:

`Library/Logic/EventManager.cs`, `GetHoroscopeCalculatorMethod`:
```csharp
// before
throw new Exception($"Calculator method not found! : {inputEventName.ToString()}");

// after
Console.WriteLine($"WARN: Horoscope calculator method not found, skipping : {inputEventName}");
return null;
```

`Library/Data/HoroscopeData.cs`, `IsEventOccuring` (the call site that invokes the delegate):
```csharp
public bool IsEventOccuring(Time time)
{
    //no calculator implementation exists for this entry - skip it
    if (this.HoroscopeCalculator == null) { return false; }
    ...
```

Net effect: the 439 (443 minus the 4 that turned out to be Match-report-only/legacy-named,
not part of the 490-entry table) implemented predictions now compute and return correctly; the
51 Ashtakavarga-family entries are silently skipped (never appear in results) instead of
crashing the entire feature for every user for the rest of the process's lifetime.

### Verification

```
GET /api/Calculate/HoroscopePredictions/Location/Singapore/Time/16:20/26/01/1975/+05:30
→ {"Status":"Pass","Payload":[
    {"FormattedName":"Anapha Yoga", "Description":"...", "RelatedBody":{...}},
    {"FormattedName":"Dhurdhura Yoga", ...},
    {"FormattedName":"Rajalakshana Yoga", ...},
    {"FormattedName":"House 1 Lord In House 1 Fortified", ...},
    ... dozens more real, computed predictions ...
  ]}
```
Real Yoga/house-lord predictions, computed from actual planetary positions for the given birth
data - not a stub, not an empty list. Both API and Website rebuilt (0 errors) and restarted.

### Caveat

This is a **recovered, not audited** implementation. The prior session's commit message was
explicit that its own ~120 reconstructed `Calculate.*` methods carry real but varying
confidence (solid for Julian dates/divisional charts/aspects; flagged as best-effort for
Shadbala, Upagrahas, Pancha Pakshi, D30, .jhd import). The 443 `CalculateHoroscope` methods
restored here come from a real historical commit rather than being freshly reconstructed, which
is a stronger provenance signal - but I have not personally verified the astrological
correctness of any individual Yoga rule against source texts. Treat prediction *content* as
unverified even though the *code path* is now confirmed working end-to-end.

---

## Pending Fixes

Known gaps identified during local-dev debugging that were **not** fixed — left open
pending a decision on scope/approach. Recorded here so they aren't rediscovered from
scratch next time.

### 51 "Ashtakavarga Yoga" horoscope predictions have no implementation anywhere

**Where:** `Library/Data/HoroscopeDataListStatic.cs` (490-entry table), calculators would live
in `Library/Logic/Calculate/CalculateHoroscope.cs`.

**What's missing:** the numbered Ashtakavarga Yoga family - `SunAshtakavargaYoga2..11`,
`MoonAshtakavargaYoga1A/1B/2B/4/6..10`, `MarsAshtakavargaYoga2..25`,
`MercuryAshtakavargaYoga3..12B` (51 entries total). Per the file's own comment, these come
from "B.V. Raman's Ashtakavarga System Book". Checked both the current codebase and a
recovered pre-existing historical branch (see the 2026-07-18 Horoscope-restoration entry
above) - neither has an implementation. As far as this investigation could determine, these
51 rules have never been implemented in this codebase's history at all.

**Current behavior (safe, not a crash):** `EventManager.GetHoroscopeCalculatorMethod` and
`HoroscopeData.IsEventOccuring` were made null-safe as part of the Horoscope restoration above,
so these 51 entries are silently skipped - they simply never appear in Horoscope Predictions
results, rather than crashing the feature.

**To actually fix:** write real Vedic-astrology logic for 51 classical Yoga rules against a
source text (the book referenced in-code) - genuine domain-authoring work, not a bug fix.
Not started; no decision made yet on whether/when to pursue this.

### AM/PM birth-time input may drop the PM designator (root cause not yet confirmed)

**Where:** `ViewComponents/Components/TimeInputSimple.razor`, `GetFullTimeString()`.

**Original symptom:** a person added with birth time entered as "4:20 PM" persisted to Azurite
as `"04:20"` (read back as AM). A temporary diagnostic (`Console.WriteLine` printing the raw
`hour`/`minute`/`meridian` values read from the DOM, right before the `DateTime.Parse` call)
was added and is **still present in the code** - not yet removed.

**Status: inconclusive, likely not reproducible as originally described.** On a later retry in
the same session, the user re-added a person and the API log showed the *correct* time
(`Time/16:20/...`) went through - but the diagnostic's console output line was not present in
what was pasted back, so it's unclear whether the underlying bug actually got fixed, or whether
the user simply interacted with the calendar-picker widget correctly this time (the earlier
attempt may have been a one-off UI interaction slip rather than a code bug). Confirmed via
`git diff`-equivalent comparison that both `TimeInputSimple.razor` and the JS calendar interop
(`Interop.js`'s `LoadCalendar`/`changeTime`) are byte-for-byte identical to the recovered
historical snapshot - so if this is a real bug, it's a long-standing latent one, not a
regression, and not something documented anywhere previously.

**Next step:** remove the temporary diagnostic line once confirmed not needed, or use it to
capture one more repro if the AM/PM issue resurfaces.

### Address geocoding returns empty for any uncached address in local dev

**Where:** `Library/Logic/Calculate/LocationManager.cs`, `AddressToGeoLocation(string)`
(~line 164) and its provider chain.

**Symptom:** calling `GET /api/Calculate/AddressToGeoLocation/Address/{name}` locally for
any address not already sitting in the Azurite `AddressGeoLocation` table returns
`{"Status":"Pass","Payload":{}}` — not an error, just nothing. Confirmed live via
`curl http://localhost:7071/api/Calculate/AddressToGeoLocation/Address/Singapore` during
this session.

**Why it happens — `Localhost_Setup.md`'s "Address Geocoding — How It Works" section does
not match the current code:**
- The doc describes `Location.cs` with an `#if DEBUG` provider list that swaps in a free
  Nominatim (OpenStreetMap) provider for local dev. That file and that provider **do not
  exist** — it's been renamed/rewritten as `LocationManager.cs`, and its
  `AddressToGeoLocation` provider list (`VedAstro` → `Azure` → `Google` → `LocalFile`) has no
  Nominatim option in either Debug or Release.
- **Azure Maps** and **Google** providers both fail silently (caught internally, return
  empty) because `API/local.settings.json` only has placeholder `"xxxx"` keys for
  `AzureMapsAPIKey` / `GoogleAPIKey` — expected for local dev, not a bug.
- **`LocalFile`** (`AddressToGeoLocation_LocalFile`, `LocationManager.cs:1715`) is a fully
  implemented offline CSV-lookup fallback, but it reads from `AddressGeoLocation.csv` in the
  process's working directory — **that file does not exist anywhere in the repo**. The code
  path is real; the data it depends on was never created/checked in. So it always falls
  through to "no match" too.

**Net effect:** not a crash, not a loop (the `GeoLocationInput.razor` fix from the main
change list above already stops this from looping) — just a single "location not found"
alert for any address not already cached, with manual lat/long entry as the only local
workaround.

**Options discussed with the user (2026-07-17), decision: leave as-is for now**
1. Wire up a free Nominatim provider in `LocationManager.cs`'s Debug path — matches the
   original doc's intent, works for any address, needs network access.
2. Create/seed `AddressGeoLocation.csv` (e.g. a top-1000-cities list) so the existing
   `LocalFile` provider actually resolves common places fully offline — no network needed,
   limited to whatever's in the file.
3. Both — Nominatim primary, CSV as an offline fallback.
4. **Chosen: do nothing yet.** User wanted to understand the gap first; revisit later if it
   becomes a blocker.

### Add-Person time input may be dropping AM/PM (under investigation)

**Where:** `ViewComponents/Components/TimeInputSimple.razor`, `GetFullTimeString()`
(~line 188).

**Symptom:** user entered birth time as **4:20 PM** on the Add Person page; the record that
actually persisted to Azurite shows `"04:20 26/01/1975 +05:30"` — i.e. read back as 4:20 AM,
PM designator lost.

**Suspected cause (not yet confirmed):** `GetFullTimeString()` reads `hour`/`minute`/`meridian`
text back out of the DOM (`#HourInput{id}`, `#MinuteInput{id}`, `#MeridianInput{id}`, written
by the `vanilla-calendar` JS widget's `changeTime` callback in `Interop.js:419-423`) and calls
`DateTime.Parse($"{hour}:{minute} {meridian}")`. If the widget's `changeTime` callback isn't
firing reliably for the meridian span specifically (vs. hour/minute, which clearly did update),
the `MeridianInput` span would retain its original server-rendered default
(`DateTime.Today.ToString("tt")`) regardless of what the user actually picked.

**Diagnostic added (temporary, not a fix):** a `Console.WriteLine` was added right before the
`DateTime.Parse` call in `GetFullTimeString()`, printing the raw `hour`/`minute`/`meridian`
strings as read from the DOM. Since this app is Blazor **WebAssembly** (confirmed from browser
console stack traces - `blazor.webassembly.js`), this prints straight to the browser console.
**Next step:** user to re-add a person with a PM time and paste the
`TEMP DEBUG raw time input: hour=... minute=... meridian=...` line so the actual faulty value
can be identified before writing a real fix. This diagnostic line must be removed once the
underlying bug is found and fixed.

---

## 2026-07-18 — Fixed real "Add Person" / "Invalid or Outdated Call" root cause: wrong client-side URL construction

### Context

After the 2026-07-17 fixes (below) made the local API itself functional, the user could still
not add a person — every attempt returned production's/local's generic
`"Invalid or Outdated Call, please rebuild API URL at vedastro.org/APIBuilder.html"`. This
turned out to be a **real bug unrelated to local dev or Azurite** - it would affect production
too, since it's client-side URL construction shared by both.

### Investigation

Added temporary `Console.WriteLine($"...URL: {incomingRequest.Url}")` logging to the API's
`Calculate` function and the `zCatch404` catch-all (`API/FrontDesk/OpenAPI.cs`,
`API/FrontDesk/GeneralAPI.cs`; both reverted after diagnosis). This revealed the browser was
calling URLs like:
```
http://localhost:7071/api/GetPersonList/OwnerId/.../VisitorId/...
http://localhost:7071/api/AddPerson/OwnerId/.../Name/Rahul Pandit/Gender/Male/Location/Singapore/Time/16:20/26/01/1975
```
— i.e. **missing the `/Calculate/` path segment entirely**. The API only exposes a small set of
dedicated Azure Functions (`Home`, `FindMatch`, `SignInGoogle`, `SignInFacebook`,
`RegisterSubscription`, etc.) plus one generic dispatcher,
`Calculate/{calculatorName}/{*fullParamString}`, which uses reflection
(`Tools.MethodNameToMethodInfo`) to find and invoke a matching method on `VedAstro.Library.Calculate`
or `API.PersonAPI`. `GetPersonList`, `AddPerson`, `DeletePerson`, `UpdatePerson`, `GetPerson`,
and `HoroscopePredictions` are all only reachable through that dispatcher — there is no
dedicated Function for any of them. Any URL missing `/Calculate/` for these hits the
catch-all 404 every time, regardless of local vs. production.

### Fix 1 — `Library/Logic/URL.cs`: added missing `/Calculate/` prefix

```csharp
// before
public string GetPersonList => $"{ApiUrlDirect}/GetPersonList";
public string AddPerson => $"{ApiUrlDirect}/AddPerson";
public string DeletePerson => $"{ApiUrlDirect}/DeletePerson";
public string UpdatePerson => $"{ApiUrlDirect}/UpdatePerson";
public string GetPerson => $"{ApiUrlDirect}/GetPerson";
public string HoroscopePredictions => $"{ApiUrlDirect}/HoroscopePredictions";

// after
public string GetPersonList => $"{ApiUrlDirect}/Calculate/GetPersonList";
public string AddPerson => $"{ApiUrlDirect}/Calculate/AddPerson";
public string DeletePerson => $"{ApiUrlDirect}/Calculate/DeletePerson";
public string UpdatePerson => $"{ApiUrlDirect}/Calculate/UpdatePerson";
public string GetPerson => $"{ApiUrlDirect}/Calculate/GetPerson";
public string HoroscopePredictions => $"{ApiUrlDirect}/Calculate/HoroscopePredictions";
```

Left `UpsertLifeEvent`, `GetNewPersonId`, and `GetPersonImage` untouched — their backing methods
were not verified to exist in `PersonAPI`/`Calculate` (`GetPersonImage`'s implementation is in
fact commented out in `PersonAPI.cs`), so prefixing them would just move the failure elsewhere
without fixing anything; not investigated further this session.

**Important divergence from the recovered `docs/Library/Logic/URL.cs` reference:** that
reference snapshot does **not** have `/Calculate/` on any of these properties either - meaning
the "correct"/originally-intended architecture was for each of these to get its own dedicated
Azure Function (the same way `FindMatch` and `SignInGoogle` already do), not to be routed
through the generic dispatcher. Those dedicated Functions were apparently never built in the
current `API` project. Prefixing the client URLs with `/Calculate/` is a pragmatic fix that
works with the API as it actually exists today, not a restoration of the original design.
Building proper dedicated Functions for these would be the more "correct" long-term fix but is
out of scope here.

### Fix 2 — `ViewComponents/Code/API/PersonTools.cs`: fixed `AddPerson()`'s parameter order/name

Compulsory parameters in the generic dispatcher are parsed **positionally by type**, not by
matching key names (`API/FrontDesk/OpenAPI.cs`, `ParseUrlParameterByType`) - so the URL segment
order must exactly match `PersonAPI.AddPerson(string ownerId, Time birthTime, string personName, Gender gender, ...)`.

```csharp
// before - wrong order (Name/Gender before Location/Time), wrong key ("Name" vs "PersonName"),
// and missing the timezone offset segment entirely
var url = $"{_api.URL.AddPerson}/OwnerId/{ownerId}/Name/{person.Name}" +
          $"/Gender/{person.Gender}" +
          $"/Location/{Tools.RemoveWhiteSpace(person.GetBirthLocation().Name())}" +
          $"/Time/{person.BirthHourMinute}/{person.BirthDateMonthYear}";

// after - matches AddPerson's compulsory param order: ownerId, birthTime, personName, gender
var url = $"{_api.URL.AddPerson}/OwnerId/{ownerId}" +
          $"/Location/{Tools.RemoveWhiteSpace(person.GetBirthLocation().Name())}" +
          $"/Time/{person.BirthHourMinute}/{person.BirthDateMonthYear}/{person.BirthTimeZoneString}" +
          $"/PersonName/{person.Name}" +
          $"/Gender/{person.Gender}";
```

**Verified end-to-end** by calling the exact URL shape the fixed client now produces directly
against the running local API:
```
GET /api/Calculate/AddPerson/OwnerId/.../Location/Singapore/Time/16:20/26/01/1975/+05:30/PersonName/Rahul%20Pandit/Gender/Male
  → {"Status":"Pass","Payload":"RahulPandit1975"}
GET /api/Calculate/GetPersonList/OwnerId/.../VisitorId/...
  → {"Status":"Pass","Payload":[{ ...real record... }]}
```
Then confirmed a second time by querying the Azurite `PersonList` table directly (a throwaway
console app referencing `Azure.Data.Tables`, connecting with `UseDevelopmentStorage=true`,
bypassing the API entirely) after the user re-added the person through the actual browser UI -
the real record was present with the user's actual visitor-ID partition key.

**Note on process:** an earlier report that "tests passed" for `AddPerson` was based on a
hand-built curl URL using the *correct* parameter order - which validated the API, not the
actually-broken client code path. The bug in `PersonTools.cs` was only found afterward by
reading the API's request log for the literal URL the browser sent. Lesson: verifying a fix
means driving it through the real client code path, not a hand-built equivalent.

### Fix 3 — `Library/Logic/Tools.cs`: two long-standing crash bugs in server-reply parsing, now actually reachable because Fix 1/2 let real requests get further

Both `ReadFromServerJsonReply` and `ReadFromServerJsonReplyVedAstro` had two bugs matching
`Localhost_Setup.md`'s already-documented (but never-applied) change #17:

**Bug A - null `result` after all retries fail** (`RequestServer`'s doc comment literally says
*"if all tries fail will return null"*, and it does): `result.Content?.ReadAsStringAsync()`
null-conditions `.Content`, but not `result` itself - if `result` is null, this throws
`NullReferenceException` before ever reaching the caller's try/catch. Fixed by adding an
explicit null-check immediately after `RequestServer` returns, in both methods:
```csharp
var result = await RequestServer(apiUrl, 3);
if (result == null)
{
    var noResponseJson = new JObject { ["RawErrorData"] = "API unreachable" };
    return new WebResult<JToken>(false, noResponseJson);
}
```

**Bug B - invalid `JObject` constructor** (2 occurrences, lines ~1737 and ~1819):
```csharp
// before - throws ArgumentException: "Can not add JValue to JObject"
return new WebResult<JToken>(false, new JObject("Failed"));

// after
return new WebResult<JToken>(false, new JObject { ["Error"] = "Failed" });
```
`new JObject("Failed")` tries to add a bare string as a child token of a `JObject`, which
Newtonsoft.Json rejects. This was live-observed in the user's browser console: a `Fail` response
from the API (the "Invalid or Outdated Call" message, itself now fixed by Fix 1/2 above) was
being parsed by `ReadFromServerJsonReplyVedAstro`, and *that parsing itself* crashed with
`System.ArgumentException: Can not add Newtonsoft.Json.Linq.JValue to Newtonsoft.Json.Linq.JObject`
right after the original API error - a crash-on-error-handling bug masking the real error.

---

## 2026-07-17 — Local dev environment: fix Horoscope page infinite loop + missing local-API controls

### Context

Local dev stack was set up per `Localhost_Setup.md` (Azurite via Docker, `func start` for
the API on `localhost:7071`, `dotnet run` for the Website on `localhost:5000`). Two problems
surfaced during manual testing:

1. The sidebar had no way to switch the website from calling production
   (`api.vedastro.org`) to the local API — `Localhost_Setup.md` described a toggle button
   that did not actually exist in the working tree.
2. After enabling local-API mode, adding a Person and opening the Horoscope page sent the
   UI into an infinite loading/alert loop.

`Localhost_Setup.md` documents 22 changes that were supposedly already made to support local
dev. Cross-checking against the actual code (and an older reference snapshot recovered under
`docs/`) showed several of those changes were never actually applied, and the real root cause
of the Horoscope loop wasn't in the document at all — it was a separate static-constructor
crash in `CacheManager` that poisoned every calculator call for the life of the API process.

---

### Changes made

#### 1. `Website/Shared/MainLayout.razor` — added the missing "Local API" toggle

- Added `private bool _debugMode;` field (was previously a throwaway local `bool debugMode`
  inside `_OnInitialized`, never exposed to the UI).
- Added a click-to-toggle element in the sidebar "deployment stamp" area (next to the
  existing recycle-icon Easter egg), showing "Local API: ON/OFF" with a `mdi:lan-connect` /
  `mdi:lan-disconnect` icon.
- Added `OnClickDebugMode()` handler: flips `localStorage.DebugMode` between `"enabled"` /
  `"disabled"` via the existing `_jsRuntime.SetProperty` interop helper, then reloads the page.
- `_OnInitialized` now reads into `_debugMode` (was reading into the discarded local
  variable), so `AppData.URL = new URL(..., _debugMode)` and the template binding both see
  the real state.

**Why:** `URL.cs` picks the API base address (`ApiLocalDebug` = `http://localhost:7071/api`
vs. production) purely from the `debugMode` bool passed into its constructor. Without a UI
control, that bool could only be set by manually running
`localStorage.setItem("DebugMode","enabled")` in the browser console — every page reload
silently fell back to hitting production, which was the reason the first Person-add attempt
produced a production-side "Invalid or Outdated Call" error instead of ever touching the
local API (confirmed by an empty `api_run.log`; the local API had received zero requests).

Reference used: `docs/Website/Shared/MainLayout.razor` (older snapshot) already had this
exact code — it was recovered from there and matched line-for-line to what the file was
missing.

#### 2. `Website/Shared/MainLayout.razor` — suppressed outdated-version alert in Debug builds

Wrapped the `CheckRaiseOutdatedWarning()` call in `_OnInitialized` with `#if !DEBUG`.

**Why:** the check compares `ThisAssembly.CommitNumber` (always `0` in a local dev build)
against the latest published commit number on GitHub, so it fired the "Please Update, your
version is 0" popup on every single page load locally. Meaningless outside of a real
deployment; guarding it with `#if !DEBUG` leaves it active in Release/production builds.

#### 3. `ViewComponents/Components/GeoLocationInput.razor` — fixed infinite alert loop (root cause of the reported Horoscope loop's symptom)

`SetCoordinatesFromNameInput()` used a `goto TryAgain` loop: on a failed/empty geocode
result it showed an error alert, reset `LocationName` to `"Singapore"`, and jumped back to
retry. If geocoding failed for `"Singapore"` too (e.g. local geocoding provider not
resolving), it looped forever, alerting repeatedly with no way for the user to break out or
correct their input.

Fix (split into a silent/non-silent overload, matching `docs/ViewComponents/Components/GeoLocationInput.razor`):
- `SetCoordinatesFromNameInput()` → thin wrapper calling `SetCoordinatesFromNameInput(silent: false)`
  (kept parameterless because Blazor's `@onblur` / `OnClickCallback` bindings require a
  zero-arg method group).
- `SetCoordinatesFromNameInput(bool silent)` → on failure, only shows the alert if
  `!silent`, and simply `return`s (leaving the user's typed text in place) instead of
  looping.
- `AutoSetLocation()` (the on-page-load fallback path) now calls
  `SetCoordinatesFromNameInput(silent: true)` so a failed auto-lookup doesn't pop an alert
  on every page load.

**Why:** exactly the failure mode reported — geocoding is less reliable in local dev (no
Azure Maps key configured), so this path was hit far more often than in production, turning
a single failed lookup into an unbreakable alert loop.

#### 4. `ViewComponents/Code/API/VedAstroAPI.cs` — wrapped `GetListNoPolling` in try/catch

Both the `GetListNoPolling<T, Y>` (POST) and `GetListNoPolling<T>` (GET) overloads now
catch `HttpRequestException`, log to console, and return an empty list instead of letting
the exception propagate.

**Why:** these are called from `PersonSelectorBox` (via `PersonTools.GetPersonList`) on
every page load to populate the person dropdown. An unhandled `HttpRequestException` here
(e.g. API temporarily unreachable) tears down the Blazor Server SignalR circuit, which the
client auto-retries — visible to the user as the page "going in a loop" / reconnecting
spinner. Matches `docs/ViewComponents/Code/API/VedAstroAPI.cs` exactly.

#### 5. `Library/Data/AzureTable.cs` — fixed a permanent static-constructor crash blocking all Person CRUD

Old code:
```csharp
public static readonly string AccountName = Secrets.Get("CentralStorageAccountName");
private static readonly string StorageAccountKey = Secrets.Get("CentralStorageKey");
private static readonly TableSharedKeyCredential Credentials = new(...);
private static readonly Uri ServiceUri = new($"https://{AccountName}.table.core.windows.net");
private static readonly TableServiceClient TableServiceClient = new(ServiceUri, Credentials);
public static readonly TableClient PersonList = TableServiceClient.GetTableClient("PersonList");
// ...and 8 more TableClient fields built the same way
```

`Secrets.Get(key)` does a reflection lookup for a **private static field** literally named
`key` on the `Secrets` class. No such field (`CentralStorageAccountName` /
`CentralStorageKey`) exists anywhere in the codebase — the only place they were ever defined
was the fully commented-out `Library/Secrets-HideMe-sample.cs`. Every access to
`AzureTable.PersonList` (or any other table) threw `Secrets.Get`'s exception, which — because
these are `static readonly` field initializers — got wrapped in a `TypeInitializationException`
and **permanently** disabled the whole `AzureTable` class for the life of the process (.NET
caches static-constructor failure; it does not retry).

Confirmed live via `func_start` logs while testing `AddPerson`:
```
The key --> 'CentralStorageAccountName' is missing sweetheart! ...
The type initializer for 'VedAstro.Library.AzureTable' threw an exception.
```

**Fix:** switched to the connection-string + null-safe pattern already used elsewhere in the
partially-migrated codebase (`SecretsEnv.cs` already exposes
`Secrets.VedAstroCentralStorageConnStr` as a nullable env-var-backed property, reading
`VedAstroCentralStorageConnStr` from `API/local.settings.json`, already set to
`UseDevelopmentStorage=true` for Azurite):

```csharp
private static readonly string? ConnStr = Secrets.VedAstroCentralStorageConnStr;
private static TableClient? MakeClient(string tableName)
{
    if (string.IsNullOrEmpty(ConnStr)) return null;
    var client = new TableServiceClient(ConnStr).GetTableClient(tableName);
    client.CreateIfNotExists();
    return client;
}
public static readonly TableClient? PersonList = MakeClient("PersonList");
// ...all other table clients converted the same way, all 9 preserved
```

**Note on divergence from `Localhost_Setup.md` change #9 / `docs/Library/Data/AzureTable.cs`:**
the reference version only has 5 table clients (`PersonList`, `APIAbuseList`,
`PersonListRecycleBin`, `LifeEventList`, `PersonShareList`). The current file has grown to 9
(also `SubscriberCallRecords`, `AnonymousIpCallRecords`, `UserDataList`, `OpenAPIErrorBook`,
`CallTracker`, `WebsiteErrorLog`, `WebsiteDebugLog`, `CallInfoStatistic`) — the doc's fix
could not be copy-pasted; the connection-string/`MakeClient` pattern was reapplied to the
current, larger field set instead.

#### 6. `Library/Logic/CacheManager.cs` — fixed the actual root cause of "every calculator call fails"

Old code:
```csharp
private static readonly Func<MemoryCache, object> GetEntriesCollection = Delegate.CreateDelegate(
    typeof(Func<MemoryCache, object>),
    typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(true),
    throwOnBindFailure: true) as Func<MemoryCache, object>;
```

The referenced `Microsoft.Extensions.Caching.Memory` package version no longer exposes an
internal `EntriesCollection` property on `MemoryCache`, so `GetProperty(...)` returns `null`,
and calling `.GetGetMethod(true)` on that `null` throws `NullReferenceException` — inside a
`static readonly` field initializer, which again becomes a permanent
`TypeInitializationException` for `CacheManager`.

This is the one that actually explains the Horoscope-page symptom: `CacheManager.GetCache`
is called from `Time.FromUrl` (`Library/Data/Time.cs:546`), which is on the hot path for
**every single calculator invocation** through the `Calculate/{calculatorName}/...` route —
so once poisoned, literally every horoscope/chart calculation failed with
"Exception has been thrown by the target of an invocation," which is what the client-side
retry logic reads as an infinite loop.

Confirmed via a temporary `Console.WriteLine(e.ToString())` added to `OpenAPI.cs`'s
`Calculate` catch block (added, captured the full stack trace below, then reverted):
```
System.TypeInitializationException: The type initializer for 'VedAstro.Library.CacheManager' threw an exception.
 ---> System.NullReferenceException: Object reference not set to an instance of an object.
    at VedAstro.Library.CacheManager..cctor() in .../CacheManager.cs:line 342
    at VedAstro.Library.CacheManager.getMethodCache(String methodName)
    at VedAstro.Library.CacheManager.GetCache[T](CacheKey key, Func`1 heavyComputation)
    at VedAstro.Library.Time.FromUrl(String url) in .../Library/Data/Time.cs:line 546
```

**Fix (matches `Localhost_Setup.md` change #18 exactly, which had never actually been applied):**
```csharp
private static readonly Func<MemoryCache, object> GetEntriesCollection =
    typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance)
        ?.GetGetMethod(true) is { } m
        ? Delegate.CreateDelegate(typeof(Func<MemoryCache, object>), m) as Func<MemoryCache, object>
        : null;
```
Null-safe: if the property is genuinely absent, `GetEntriesCollection` is just `null`
instead of throwing at class load. The only consumer, `CacheManager.GetKeys(this IMemoryCache)`,
is unused anywhere else in the codebase (verified via search), so a null delegate there is safe.

#### 7. `Library/Logic/AzureCache.cs` — fixed a second permanent static-constructor crash blocking Add/Delete Person

Old code called `Secrets.Get("CentralStorageConnectionString")` — again, no such field
exists anywhere (checked the whole repo), so every touch of `AzureCache` (including
`PersonAPI.AddPerson`'s unconditional call to `AzureCache.DeleteCacheRelatedToPerson(...)`)
threw and permanently broke the class the same way as change #5 above. Confirmed live:
after fixing `CacheManager`, `AddPerson` still failed with
`"The type initializer for 'VedAstro.Library.AzureCache' threw an exception."`

**Fix:**
- Static constructor now uses `Secrets.VedAstroCentralStorageConnStr` (the same nullable,
  env-var-backed property used for `AzureTable.cs`) instead of the broken `Secrets.Get(...)`
  reflection call, wrapped in try/catch, and calls `blobContainerClient.CreateIfNotExists()`.
- `blobContainerClient` field changed to nullable (`BlobContainerClient?`).
- Added null-guards (return empty/false/early) to `ListBlobs`, `IsExist`, and both
  `DeleteCacheRelatedToPerson` overloads (`Person` and `string personId` variants) so a
  missing/misconfigured connection string degrades gracefully instead of throwing.

**Verified end-to-end after this fix**, via direct calls to the running local API:
```
GET /api/Calculate/AddPerson/...      → {"Status":"Pass","Payload":"TestUser32024"}
GET /api/Calculate/GetPersonList/...  → {"Status":"Pass","Payload":[{ ...real person from Azurite... }]}
GET /api/Calculate/PlanetConstellation/... → {"Status":"Pass","Payload":{}}
```

---

### What could NOT be changed, and why

`Localhost_Setup.md` documents 22 changes against an older snapshot of this codebase. Several
of those changes reference files, classes, or methods that no longer exist in their
documented form, because the codebase has since been refactored. These were **not**
mechanically reapplied — doing so would have reintroduced regressions or simply not
compiled:

- **`Library/Logic/Calculate/Location.cs` (doc changes #19 provider list, #20, #21)** — this
  file has been renamed/rewritten as `Library/Logic/Calculate/LocationManager.cs`. The new
  version already has its own, more advanced provider architecture: every provider call in
  `GeoLocationToTimezone`/`AddressToGeoLocation` is already wrapped in try/catch, and it
  already ships built-in `APIProvider.LocalFile` and `APIProvider.CPU` (last-resort, offline,
  no-network) providers — a superset of what the doc's "planned, not yet implemented"
  Nominatim/TimeApiIo/seed-file proposal (change #21) was trying to achieve. No action
  needed; re-implementing the doc's plan here would have been a regression against
  already-better code.

- **`Library/Logic/Secrets.cs` (doc change #8)** — the doc's proposed fix (add three
  properties directly onto `Secrets.cs`) has already been done, just via a separate partial
  class file, `Library/Logic/SecretsEnv.cs`, rather than by editing `Secrets.cs` in place.
  Confirmed present and used (relied on for the `AzureTable.cs` and `AzureCache.cs` fixes
  above). No action needed.

- **`API/CallTracker.cs` and `API/LogBook.cs` (doc changes #11, #12)** — neither file exists
  in the current tree anymore (confirmed via repo-wide search); this logic was merged into
  or replaced by other files during refactoring. Could not apply a fix to a file that no
  longer exists, and no equivalent hardcoded-URI crash pattern was found elsewhere to
  redirect the fix to.

- **`Library/Logic/ApiStatistic.cs` (doc change #14)** — has been substantially restructured
  since the doc was written; its current form doesn't match the doc's described
  hardcoded-URI-based version at all (it now has several `TableServiceClient`/`TableClient`
  fields that are declared but never assigned or used — effectively dead code, not the
  crash-on-construct pattern the doc describes). Left unchanged: it is not on the
  Horoscope/Person-add code path that was actually broken, and rewriting unused dead fields
  was out of scope for this fix.

- **`ChatAPI.cs` LOCAL_LLM routing (doc change #22)** — not investigated this session; out of
  scope since it only affects the horoscope chat/LLM feature, not the Person-add/Horoscope
  calculation loop that was reported. `LOCAL_LLM_BASE_URL` / `LOCAL_LLM_API_KEY` are already
  present in `API/local.settings.json` from earlier setup, unverified whether the C# routing
  code itself was ever wired up.

- **Broader `Secrets.Get("...")` call sites** (`API_STORAGE`, `WEB_STORAGE`,
  `WEBSITE_STORAGE` in `Tools.cs`; `CentralStorageAccountName`/`CentralStorageKey` reused in
  `LocationManager.cs`'s own constructor; various LLM API keys in `ChatAPI.cs`;
  `GoogleAPIKey`/`AzureMapsAPIKey`/`IpDataAPIKey` in `LocationManager.cs`) — a repo-wide
  search found ~40 more call sites using the same broken reflection-based `Secrets.Get(key)`
  pattern for keys that don't exist as literal private fields. Most of these are already
  individually wrapped in try/catch by their callers (e.g. every provider in
  `LocationManager`'s provider dictionaries, `ChatAPI.cs`'s LLM calls), so a missing key
  degrades that one provider/feature rather than crashing a whole static class. These were
  **not** touched — only the two call sites that were unguarded and directly blocking the
  reported bug (`AzureTable.cs`, `AzureCache.cs`) were fixed. A full audit/fix of every
  `Secrets.Get` call site was out of scope for this session.
