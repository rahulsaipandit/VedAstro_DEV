# Design: Hindu Calendar Feature

Status: draft
Reference apps (algorithms to adapt into C#, **not** to port verbatim — this repo already has
its own Panchang/ephemeris engine and must stay internally consistent with it):
- https://github.com/Vedic-Panchanga/Shri-Jagannath-Panchang (Python) — daily Panchang element
  formulas, festival/vrat determination logic.
- https://github.com/vishalnagda1/choghadiya (JS) — Choghadiya table generation.

Target UX is the "Hindu Calendar" app shown in the reference screenshots: a Panchang home screen,
Month View, Festivals/Vrat list with detail sheets, and a Muhurt screen with
Choghadiya/Hora/Lagna tabs. This doc scopes what's genuinely new engineering work versus what
already exists in this codebase, since a lot of the underlying calculation layer is already built.

## 1. What already exists (do not rebuild)

| Screenshot feature | Existing code |
|---|---|
| Daily Panchang (Tithi, Nakshatra, Yoga, Karana, Vaar, sunrise/sunset) | `Calculate.DailyPanchang(Time)` → [Panchang.cs](../Library/Logic/Calculate/Panchang.cs), record [DailyPanchang.cs](../Library/Data/DailyPanchang.cs) |
| Choghadiya table (day/night, 8+8 periods, quality) | `Calculate.ChoghadiyaPeriods(Time)` → record [ChoghadiyaPeriod.cs](../Library/Data/ChoghadiyaPeriod.cs), [ChoghadiyaName.cs](../Library/Data/Enum/ChoghadiyaName.cs) |
| Festival dates (Diwali, Holi, MahaShivaratri, MakarSankranti, etc.) | `Calculate.FestivalDate` / `FestivalCalendar` → [FestivalCalendar.cs](../Library/Logic/Calculate/FestivalCalendar.cs), [FestivalName.cs](../Library/Data/Enum/FestivalName.cs) |
| Month View + Festival list + day detail sheet UI | `WebsiteNative/src/app/FestivalCalendar.tsx`, `WebsiteNative/src/components/PanchangDetailSheet.tsx`, API client `WebsiteNative/src/lib/api/festivalCalendar.ts` |
| Electional/Muhurat verdicts per activity (travel, marriage, medical, agriculture, buying/selling, etc.) | `Calculate.*` in [Muhurtha.cs](../Library/Logic/Calculate/Muhurtha.cs) (`CalculatorResult` per activity, Lagna-based) |
| Weekday-hour "Hora lord" for a single instant | `Calculate.HoraAtBirth(Time)` → [Core.cs](../Library/Logic/Calculate/Core.cs) |
| Ayanamsa, sunrise/sunset, planet longitudes, chart rendering | [CoreTime.cs](../Library/Logic/Calculate/CoreTime.cs), [Core.cs](../Library/Logic/Calculate/Core.cs) (Swiss Ephemeris-backed) |
| Kundli (birth chart) viewer | `WebsiteNative/src/components/IndianChart.tsx`, `SkyChartViewer.tsx`, screen `WebsiteNative/src/app/Horoscope/[personId].tsx` |
| Auto-exposure to frontend | `API/FrontDesk/OpenAPI.cs` reflects over `Calculate`'s public static methods — any new method added there is automatically callable at `GET /api/Calculate/{calculatorName}/{*fullParamString}`, **no new API plumbing needed** |

Given this, the new work is: (a) a handful of missing calculation methods, (b) new/extended
data enums, and (c) new frontend screens/components that call the (mostly already-existing)
API surface.

## 2. New calculation work (`Library/Logic/Calculate/`)

### 2.1 Hora periods (day-long table, not just "hora at an instant")

`HoraAtBirth` only answers "which hora lord governs this one instant." The Muhurt screen needs
the full day's 24 sequential hora windows (12 day + 12 night, each ~1/12 of day/night length,
weekday-lord-ordered per the fixed planetary sequence Sun→Venus→Mercury→Moon→Saturn→
Jupiter→Mars→Sun...), the same day/night split already used by `ChoghadiyaPeriods`.

Add to `Panchang.cs` (sibling to `ChoghadiyaPeriods`, reusing its sunrise/sunset/day-length
plumbing):

```csharp
public static List<HoraPeriod> HoraPeriods(Time date)
```

New record `Library/Data/HoraPeriod.cs`: `record struct HoraPeriod(PlanetName Lord, Time Start, Time End, bool IsDayPeriod)`.
`PlanetName` is already the repo's planet enum, so no new enum needed for the lord.

### 2.2 Rahu Kaal, Gulika Kaal, Yamaganda Kaal

Not present under any name (verified: only `HoraAtBirth` matches "Hora/RahuKaal/Gulika" in
`Library/`). These are each one fixed 1/8-of-daytime (and for Gulika/Yamaganda, one 1/8-of-night)
slot per weekday, looked up from a static weekday→slot-index table — same
shape as Choghadiya's per-weekday start tables in `Panchang.cs`, just simpler (a single window,
not 8). Add:

```csharp
public static TimeRange RahuKaal(Time date)
public static TimeRange GulikaKaal(Time date)
public static TimeRange YamagandaKaal(Time date)
```

These are inauspicious-only windows (no "quality" concept — they're binary avoid-windows,
shown separately from Choghadiya's 7-quality-level table in the reference app's Muhurt tab).
Weekday-index tables to adapt from `vishalnagda1/choghadiya`'s Rahu Kaal constants (that repo
already encodes the standard 1/8-segment indices per weekday; verify against a second source
before trusting, per this repo's existing practice of cross-checking against multiple reference
implementations — see doc comments in `Panchang.cs`).

### 2.3 Gand Mool / Panchaka (nakshatra-based inauspicious spans)

Gand Mool occurs when the Moon transits one of 6 specific nakshatras (Ashwini, Ashlesha, Magha,
Jyeshtha, Moola, Revati). Since `ConstellationAtLongitude` and Moon longitude tracking already
exist (`Core.cs`/`CoreTime.cs`), this is a lookup + start/end-of-transit search, structurally
identical to `FestivalCalendar.cs`'s `FindTithiInstant` Newton-convergence pattern but searching
for Moon ingress/egress of a nakshatra boundary instead of a tithi boundary. Add:

```csharp
public static List<TimeRange> GandMoolPeriods(Time date, int daysAhead)
```

### 2.4 Chaturmas tracking

Chaturmas (the ~4-month Vishnu-sleep period, Ashadha Shukla Ekadashi to Kartika Shukla Ekadashi)
is a single yearly span, computed the same way `FestivalCalendar.cs` finds a named tithi instant
in a given lunar month — two tithi-instant searches (start, end) rather than a new algorithm.
Add as a `FestivalName`-like single-value helper rather than a whole new subsystem:

```csharp
public static TimeRange ChaturmasPeriod(int year, GeoLocation location)
```

### 2.5 Ekadashi + Vrat calendar (extends `FestivalCalendar.cs`, not a new file)

`FestivalName` currently has 8 solar/lunar festivals but no Ekadashi (24-26/year) or other Vrats
(Sankashti Chaturthi, Pradosh, Sankranti-adjacent vrats, etc. per the screenshot's Festivals/My
Tithi/Vrat tabs). Ekadashi is tithi #11 of each paksha — same `FindTithiInNijaMonth` machinery
`FestivalCalendar.cs` already uses for e.g. Diwali (tithi #30 Krishna), just run once per paksha
instead of once per year. Plan:

- **Implemented.** `Calculate.EkadashiCalendar(year, location)` (a paksha-iterating sibling of
  `FestivalCalendar`, not a fork of it) returns ~24-26 `EkadashiOccurrence { Name, Date }` values
  per year. Resolved: names are attached via a classical-name lookup keyed by this codebase's
  Amanta month convention (`EkadashiNamesByAmantaMonth` in `FestivalCalendar.cs`), converting
  through the same Amanta/Purnimanta shift `FestivalDate`'s doc comment already describes for
  Diwali/Holi/etc. Cross-checked against 3 convention-independent anchor names (Nirjala, Mokshada,
  Rama) and pinned by test (`FestivalCalendarTests.cs`); not independently verified against a full
  published year-calendar beyond that. `FestivalName` was **not** extended with Ekadashi values -
  it doesn't fit that enum's once-per-year-per-name shape. Other Vrats (Sankashti Chaturthi,
  Pradosh Vrat, Sankranti-adjacent vrats) are still unimplemented.
- **Implemented, client-only.** "My Tithi" (screenshot: user adds/tracks a custom personal tithi,
  e.g. a death anniversary tithi, and the app surfaces its next occurrence) needed **no backend
  change at all** - `Calculate.VedicBirthDate(Time referenceDate, int year)` (already built for
  VedicBirthday.tsx) *is* the generic "find this tithi's recurrence in a given year" primitive,
  since a birth date is just a reference date carrying a tithi+lunar-month like any other. A saved
  entry is `{ label, referenceDate }`, resolved by calling `VedicBirthDate` for the current year
  (and next year if that occurrence has already passed). Persistence is local-device-only
  (`useMyTithiStore`, zustand + AsyncStorage) - not synced to the server or attached to a
  `Person`, so there's no cross-device sync yet if that's ever wanted.

### 2.6 Lagna tab in Muhurt screen

The screenshot's Muhurt "Lagna" tab shows the ascendant sign changing through the day (Mesh →
Vrishabha → ...) with change-times, i.e. a day-long table of Lagna-change instants — same shape
as Hora/Choghadiya (2.1), computed from `HouseLongitude`/ascendant already in `Core.cs`, just
walked forward from sunrise to next sunrise finding each sign-boundary crossing. Add:

```csharp
public static List<LagnaPeriod> LagnaPeriods(Time date)
```

`record struct LagnaPeriod(ZodiacName Sign, Time Start, Time End)` in `Library/Data/`.

## 3. Out of scope for this doc

- **Kundli / birth chart rendering** — already fully built (`IndianChart.tsx`,
  `SkyChartViewer.tsx`, `CalculateHoroscope.cs`). The Hindu Calendar app's "Kundali" tile is just
  a navigation entry into the existing Horoscope screen for the active `Person`, not new work.
- **Match Making / Guna Milan** — compatibility scoring is a separate, larger feature
  (Ashtakoota matching) not covered by either reference repo; needs its own design doc if pursued.
- **Multi-language (10 Indian languages)** — an i18n/string-resource concern orthogonal to the
  calculation work above; not addressed here.
- **Offline support** — the screenshots advertise "works offline." This repo's calculations are
  already pure functions over `Time`/`GeoLocation` with no network dependency once ephemeris data
  is loaded, so offline is primarily a WebsiteNative bundling/caching concern (ship ephemeris
  files with the app, cache API responses), not a backend design question.

## 4. Frontend work (`WebsiteNative`)

Model new screens directly on the existing Festival/Horoscope screens rather than inventing new
patterns:

- **Panchang home / Muhurt screen**: new screen alongside `FestivalCalendar.tsx`, tabbed
  (Choghadiya | Hora | Lagna | Rahu Kaal etc.) per the screenshot, each tab a simple table driven
  by one of `ChoghadiyaPeriods` / `HoraPeriods` (2.1) / `LagnaPeriods` (2.6) / `RahuKaal`+siblings
  (2.2) via `festivalCalendar.ts`-style typed API client methods.
- **Festivals screen**: extend existing `FestivalCalendar.tsx` tabs to add "My Tithi" and "Vrat"
  tabs (screenshot shows these as tab pills alongside "Festivals"), backed by the extended
  `FestivalName` enum (2.5) plus the new `MyTithi` client-tracked list.
  `PanchangDetailSheet.tsx`'s existing per-day detail sheet already matches the reference app's
  "tap a festival for its story and dates" screen — extend its content, don't replace it.
- **Radial day-clock view** (screenshot 6: sunrise/sunset arc with Tithi/Nakshatra/Yoga/Karana/
  Choghadiya/Hora/Lagna/Gand Mool listed around it) is a new visualization component consuming
  the same `DailyPanchang` + period-list endpoints; no new backend data needed once 2.1–2.6 exist.
- **Tithi display**: UI should render Tithi by name (e.g. "Chaturdashi", "Amavasya"), not as a
  bare digit/index — matching the reference screenshots, which never show a tithi number. The
  `LunarDay` data already carries the named value (`Calculate.DailyPanchang`'s `Tithi.ToJson()`);
  this is purely a display-formatting choice in the UI layer, not a backend change.
- **Tithi tooltip**: wherever Tithi is shown, add an info tooltip/help icon explaining the
  sunrise-vs-instantaneous distinction:
  > For simplicity, the convention has been to use the tithi etc at sunrise. But nowadays we can
  > actually calculate instantaneous tithi. So in Sankalp mantra, the tithi prevalent at that
  > exact instant is given. This is accurate as Sankalp mantra is a declaration that you pledge
  > at so and so time, and you are using the actual tithi of the time.

  `DailyPanchang.Tithi` is the sunrise-anchored convention value (matches the rest of the
  Panchang, and Choghadiya/Hora's day/night split, which are all sunrise-referenced). If an
  instantaneous-tithi-at-a-given-time display is ever added (e.g. for Sankalp use), it needs a
  separate calculation call — evaluate `LunarDay` at the queried `Time` directly rather than at
  `SunriseTime(date)` — not a reinterpretation of the existing sunrise-based value.
- **Sunrise/Sunset reminders**: let the user set a daily reminder/alarm for sunrise and/or
  sunset at their saved location. This is genuinely new — no notification/reminder/alarm code
  exists anywhere in `WebsiteNative` today (checked: no `expo-notifications` dependency, no
  reminder/alarm screen or store). Needed pieces:
  - Add `expo-notifications` (local, on-device scheduled notifications — no push/server backend
    needed, since sunrise/sunset times are already computable client-side via `SunriseTime`/
    `SunsetTime` through the existing `/api/Calculate/...` client, or could be precomputed for
    the next N days and cached for offline use per the offline note above).
  - Because sunrise/sunset drift day to day, this can't be a single fixed-time OS alarm — it
    needs either (a) a daily background task that re-schedules "tomorrow's" notification once
    today's fires, or (b) scheduling a batch of the next ~30 days' sunrise/sunset notifications
    at once and refreshing the batch periodically (simpler, no background-task reliability
    concerns; prefer this).
  - New small persisted setting (`remindersEnabled: { sunrise: bool, sunset: bool }`) alongside
    the app's existing `useAppStore` (`WebsiteNative/src/store/useAppStore.ts`), and a settings UI
    toggle — most naturally placed on the same screen as the existing debug-mode toggle, or on
    the Panchang home screen this doc adds.
  - No backend change needed beyond what already exists (`SunriseTime`/`SunsetTime` in
    `Core.cs`); this is purely a `WebsiteNative` feature.
- **Visual design**: the reference screenshots are only a layout/information reference, not a
  style target. Do not copy their dark brown/gold theme or ornate serif-ish numerals — build
  against this app's own existing theme and system fonts (`WebsiteNative/src/constants/
  theme.ts`'s `Colors.light`/`Colors.dark` + `Fonts`), aiming for a clean, clear, legible view
  consistent with the rest of the app rather than a themed calendar-app look. New screens must
  support both light and dark mode the same way existing screens do (reading from `Colors[theme]`
  via whatever hook/provider `FestivalCalendar.tsx`/`Horoscope/[personId].tsx` already use for
  theme switching), not just the light theme.

## 4a. Day Clock screen (added after initial build)

The reference screenshots' detailed radial "day clock" view (hour-ticked 24h face with the 8
classical period names Usha/Purvaahna/Madhyahna/Aparahna/Saayankal/Pradosh/Nishith/Triyaam, plus a
table where each Panchang limb shows its current value, when it changes, and what's next) and a
compact "at a glance" widget version of the same idea (small arc + current time + Tithi + current
Choghadiya + Dur Muhurta + Chaturmas/Gand Mool status) needed backend work beyond section 2:

- **Per-limb transition finding** — `Calculate.TithiTransition` / `NakshatraTransition` /
  `YogaTransition` / `KaranaTransition` (all in `Panchang.cs`), each returning
  `{ Current, EndTime, Next }`. Implemented via one shared generic hour-stepping-then-bisecting
  helper (`FindNextTransition`, reusing the same `BisectBoundary` GandMoolPeriods/LagnaPeriods
  already use) rather than a per-limb closed-form formula, since it works uniformly regardless of
  each limb's non-constant angular rate. Vaar/Choghadiya/Hora/Lagna needed no new calculator -
  their existing period-list endpoints already carry start/end per period, so "current + when it
  changes" is just picking whichever period contains "now" client-side.
- **Dur Muhurta** — `Calculate.DurMuhurtaPeriods` (`Panchang.cs`): 1-2 fixed ~48-minute
  inauspicious windows per weekday, distinct from Rahu/Gulika/Yamaganda Kaal. The weekday table
  came from web search (2 independently-phrased sources agreeing on the same offsets), cross
  validated by confirming all 8 stated offsets resolve to exact 1/15-of-a-day muhurta boundaries -
  flagged in the code comment as best-effort pending a from-first-principles source, same caveat
  applied to Ekadashi naming.
- **Vikram Samvat** — `Calculate.VikramSamvatYear` (`Panchang.cs`): Gregorian year + 57 from that
  year's own Chaitra Shukla Pratipada onward, +56 before it, reusing `FindTithiInNijaMonth`. A
  well-defined, verifiable era conversion (unlike Dur Muhurta), so no accuracy caveat needed.
- Frontend: `FullDayClock.tsx` (the detailed radial view, a more elaborate sibling of
  `RadialDayClock.tsx`), `DayClockWidget.tsx` (the compact card, embedded at the top of
  `Muhurt.tsx`), and a new `DayClock.tsx` screen for the full detail table.
- Not implemented: the reference app's exact "(2 Mi) Magha, ..." parenthetical month/nakshatra
  notation above the Vara/Samvat line - its meaning wasn't confidently identifiable, so it was
  omitted rather than guessed at.

## 4b. Full Panchang detail list (added after 4a)

A further reference screenshot showed a much richer flat Panchang list (not the radial clock) -
Purnimanta month, Moon/Sun Zodiac (with transition times for the Moon), Ritu, Ayana, 3 more
calendar eras beyond Vikram Samvat, a North-Indian Samvatsara variant, and Moonrise/Moonset.
Added to `DayClock.tsx`'s existing detail table (same file, more rows) rather than a new screen,
since the row format already matched exactly:

- **`Calculate.PurnimantaMonth`** (`Panchang.cs`) — derives Purnimanta from the existing Amanta
  `LunarMonth` using the same Krishna-paksha-shifts-one-month-ahead rule `FestivalDate`'s doc
  comment already documents. Verified against Diwali (Amanta Aaswayuja → Purnimanta Kaarteeka,
  matching the popular "Kartik Amavasya" name).
- **`Calculate.Ritu`** — classical 2-lunar-months-per-season table (Chaitra+Vaisaakha=Vasanta,
  etc.), cross-checked against 3 independent sources. Note: one reference screenshot showed
  "Sharad" for a date/Sun-position this table and every source found puts in "Varsha" - that
  screenshot value wasn't trusted over 3 corroborating documented sources, so a discrepancy with
  that one example is expected and was not chased.
- **`Calculate.Ayana`** — Sun's sidereal longitude vs. the Karka/Makara Sankranti boundaries
  (90°/270°) - unambiguous, no accuracy caveat.
- **`Calculate.SamvatsaraName` / `SamvatsaraNameNorth`** — the 60-year Jupiter-cycle name.
  `SamvatsaraName` (South Indian, mechanical civil count) is pinned against a published
  Vikram-Samvat-to-name table (Wikipedia) and reproduces it exactly. `SamvatsaraNameNorth` uses a
  fixed +14-name offset calibrated against a single confirmed example (VS 2083: south=Parabhava,
  north=Raudri) rather than tracking actual historical Jupiter-resync skip years - flagged
  best-effort in the code comment.
- **`Calculate.GujaratiSamvatYear` / `SakaSamvatYear` / `KaliSamvatYear`** — same
  `FindTithiInNijaMonth`-based New Year search as `VikramSamvatYear`, different epoch/rollover
  rule each (Gujarati rolls over at Kartika Shukla Pratipada instead of Chaitra; Saka is epoch
  -78; Kali is epoch +3101/3102). All 3 reproduce the exact values from the reference screenshot's
  worked example (1948 Saka, 5127 Kali for the same date shown).
- **`Calculate.MoonriseTime` / `MoonsetTime`** (`Core.cs`) — the same Swiss-Ephemeris
  `swe_rise_trans` approach as `SunriseTime`/`SunsetTime`, duplicated rather than refactored into
  a shared helper to avoid any risk of regressing those two heavily-used existing methods.
- **`Calculate.MoonZodiacTransition`** — reuses the `FindNextTransition` helper from 4a.
- Not implemented: **Solar Tithi** (a solar-calendar day count, e.g. Bengali/Odia-style). The
  screenshot's own example was internally inconsistent (paired a solar month name with a Sun
  Zodiac sign that don't correspond under any interpretation checked), and no reliable
  documented source for this app's specific regional-solar-calendar convention was found - rather
  than guess, this was left out.

## 4c. Vrat & minor-festival list (added after 4b)

A further reference screenshot showed a combined Vrat/minor-festival list (Festivals | My Tithi |
Vrat | Sankranti tabs, a "Chaturmas Nd left" banner, per-row "in N days" countdowns, and multi-day
ranged events with sub-items). Added to `FestivalCalendar.tsx` (renamed its "Ekadashi & Vrat" tab
to "Vrat" and merged everything into one combined, sorted, future-only list) and a new "Sankranti"
tab, rather than a new screen:

- **3 new named festivals** — `HartalikaTeej`, `GaneshChaturthi`, `AnantChaturdashi` added to
  `FestivalName`/`FestivalDate` (all Bhaadrapada Shukla, tithis 3/4/14 respectively) - well-known,
  no accuracy caveat.
- **4 new monthly-recurring Vrat calendars** — `PurnimaVratCalendar` (tithi 15), `AmavasyaVratCalendar`
  (tithi 30), `SankashtiChaturthiCalendar` (tithi 19), `PradoshVratCalendar` (tithi 13 of each
  paksha) - all built on a new shared `RecurringTithiCalendar` helper extracted from
  `EkadashiCalendar` (which was refactored to use it too, behavior-unchanged, tests still pass).
  Mechanically identical to how Ekadashi already worked, so no accuracy caveat.
- **`Calculate.PitruPakshaPeriod`** — Bhaadrapada Purnima to the following Amavasya, same pattern
  as `ChaturmasPeriod`. No accuracy caveat.
- **`Calculate.JyeshthaGauriAvahana` / `JyeshthaGauriPoojan` / `JyeshthaGauriVisarjan`** — the one
  genuinely new *kind* of search in this round: these 3 days aren't a fixed tithi at all, they're
  whichever sunrise within Bhaadrapada Shukla paksha has the Moon in Anuradha/Jyeshtha/Moola
  nakshatra respectively (a day-by-day nakshatra-at-sunrise scan, not a tithi-instant search).
  Verified against a real published 2026 date (17/18/19 September) that this reproduces exactly.
- **`Calculate.SankrantiCalendar`** — generalizes the existing `MakarSankrantiDate` search to all
  12 Rashis for a year, seeded from the validated Makar anchor. Cross-checked against
  `FestivalDate(MakarSankranti)` for consistency (same value via two different code paths).
- Frontend: `FestivalCalendar.tsx` now has 4 tabs, a Chaturmas countdown banner (using the
  `ChaturmasPeriod` calculator added in section 4, previously computed but never surfaced in any
  UI), and per-row day countdowns. Countdown/filtering logic captures "now" once per Calculate
  press (an event handler) rather than calling `Date.now()` during render, per the
  `react-hooks/purity` lint rule.
- Not implemented: a reference-date picker for the Vrat tab (countdowns are relative to whenever
  Calculate was pressed, not an arbitrary picked date - the Festivals/Ekadashi tabs don't have a
  date picker either, only a Year field, so this matches existing screen conventions rather than
  the one reference screenshot's date-picker affordance).

## 5. Open questions

1. **Rahu/Gulika/Yamaganda Kaal weekday tables**: which convention (there are minor variants
   across regional systems) should be the default — needs a decision before implementing 2.2,
   ideally cross-checked against both reference repos plus a third source as this codebase's
   existing Panchang doc-comments already do for Choghadiya.

Resolved:
- **"My Tithi" persistence** → local device storage only, no backend touch. Implemented as
  `useMyTithiStore` (zustand + AsyncStorage); no `Data/Migrations` change, nothing added to
  `Person`. Cross-device sync isn't available as a result — a future ask, not in scope here.
- **Ekadashi naming/scope** → full classical names, not a generic date-only list. Implemented via
  `EkadashiNamesByAmantaMonth` in `FestivalCalendar.cs` (see 2.5) rather than extending
  `FestivalName`, since that enum's once-per-year-per-value shape doesn't fit a fortnightly
  recurrence.
