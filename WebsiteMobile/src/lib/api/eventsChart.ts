import type { BirthTimeJson } from '@/lib/time';

/**
 * Backs EventsChartViewer.tsx — the RN port of ViewComponents/Components/EventsChartViewer.razor.
 * That component's core mechanism is simple despite its size: GET /api/EventsChart/{specs} on
 * API/FrontDesk/EventsChartAPI.cs returns a raw SVG string (Content-Type: image/svg+xml, no JSON
 * envelope, confirmed by reading the endpoint directly), which the Blazor side threw at a
 * hand-rolled JS charting library (EventsChart.js) for interactivity (zoom, tippy tooltips,
 * highlight-by-keyword, Google Calendar export, bookmarking, share). None of that JS layer is
 * ported — react-native-svg's SvgXml renders the same raw SVG directly, which is most of the
 * actual value (seeing the chart) without needing a JS charting library port.
 */

type PersonLike = { id: string; birthTime: BirthTimeJson };

/** Mirrors Library/Data/EventsChart.cs's ToUrl()/FromUrl() exactly (parts[0..14], see EventsChart.cs). */
function eventsChartUrl(
  personId: string,
  start: { hh: string; mm: string; dd: string; mo: string; yyyy: string },
  end: { hh: string; mm: string; dd: string; mo: string; yyyy: string },
  offset: string,
  daysPerPixel: number,
  eventTagsCsv: string,
  algorithmNamesCsv: string
): string {
  return (
    `/${personId}` +
    `/Start/${start.hh}:${start.mm}/${start.dd}/${start.mo}/${start.yyyy}` +
    `/End/${end.hh}:${end.mm}/${end.dd}/${end.mo}/${end.yyyy}` +
    `/${offset}` +
    `/${daysPerPixel}` +
    `/${eventTagsCsv}` +
    `/${algorithmNamesCsv}`
  );
}

function parseStdTime(stdTime: string) {
  // "HH:mm dd/MM/yyyy zzz"
  const match = /^(\d{2}):(\d{2}) (\d{2})\/(\d{2})\/(\d{4}) ([+-]\d{2}:\d{2})$/.exec(stdTime);
  if (!match) throw new Error(`Unrecognized StdTime format: ${stdTime}`);
  return { hh: match[1], mm: match[2], dd: match[3], mo: match[4], yyyy: match[5], offset: match[6] };
}

/**
 * Mirrors Calculate.AutoCalculateTimeRange's preset math (Library/Logic/Calculate/CoreMisc.cs)
 * — pure date arithmetic, no API call needed. Only the subset of presets LifePredictor/
 * GoodTimeFinder actually default to is implemented (full life + a few multi-year spans); the
 * free-text "age1to10"/"1990-2000" forms aren't exposed in the simplified RN UI.
 *
 * "FullLife" is birth-anchored (birth → birth+100 years, a whole-life overview). Every other
 * preset ("1year"/"3year"/"5year"/"10year") is anchored on the CURRENT moment, not birth — e.g.
 * "+1 Year" means the next 12 months starting today, not the person's first year of life. This
 * matches what a Muhurta/electional tool and a near-term life forecast both actually need
 * (looking ahead from now), and is what a live bug report against the deployed Blazor site
 * confirmed as the expected behavior for "+1 Year" (birth 01/01/1980, clicked today → expected
 * today..+1 year, not 01/01/1980..01/01/1981).
 */
export const TIME_RANGE_PRESETS = ['FullLife', '1year', '3year', '5year', '10year'] as const;
export type TimeRangePreset = (typeof TIME_RANGE_PRESETS)[number];

function presetToYears(preset: TimeRangePreset): number {
  if (preset === 'FullLife') return 100;
  return parseInt(preset, 10);
}

/** Days between two calendar dates (ignoring time-of-day), for the daysPerPixel precision formula. */
function daysBetween(startYyyy: number, startMo: number, startDd: number, endYyyy: number, endMo: number, endDd: number): number {
  const start = Date.UTC(startYyyy, startMo - 1, startDd);
  const end = Date.UTC(endYyyy, endMo - 1, endDd);
  return Math.abs(end - start) / (1000 * 60 * 60 * 24);
}

/** Mirrors Library/Logic/Factory/EventsChartFactory.cs's EventsChart.GetDayPerPixel (days between / canvas width), rounded to 3dp like the old DayPerPixelInput default. */
function roundDaysPerPixel(days: number, maxWidth: number): number {
  return Math.round((days / maxWidth) * 1000) / 1000;
}

/** "+HH:mm"/"-HH:mm" -> signed minutes offset from UTC. */
function parseOffsetMinutes(offset: string): number {
  const match = /^([+-])(\d{2}):(\d{2})$/.exec(offset);
  if (!match) return 0;
  const sign = match[1] === '-' ? -1 : 1;
  return sign * (parseInt(match[2], 10) * 60 + parseInt(match[3], 10));
}

/** The current wall-clock date/time as displayed at the given UTC offset (e.g. the birth location's timezone). */
function nowAtOffset(offset: string): { hh: string; mm: string; dd: string; mo: string; yyyy: string } {
  const shifted = new Date(Date.now() + parseOffsetMinutes(offset) * 60 * 1000);
  return {
    hh: String(shifted.getUTCHours()).padStart(2, '0'),
    mm: String(shifted.getUTCMinutes()).padStart(2, '0'),
    dd: String(shifted.getUTCDate()).padStart(2, '0'),
    mo: String(shifted.getUTCMonth() + 1).padStart(2, '0'),
    yyyy: String(shifted.getUTCFullYear()),
  };
}

/** Resolves a TimeRangePreset's literal start/end date parts — "FullLife" from birth, everything else forward from now. */
function resolvePresetRange(person: PersonLike, preset: TimeRangePreset) {
  const birth = parseStdTime(person.birthTime.StdTime);
  const years = presetToYears(preset);

  if (preset === 'FullLife') {
    const startYear = parseInt(birth.yyyy, 10);
    return { start: birth, end: { ...birth, yyyy: String(startYear + years) }, offset: birth.offset };
  }

  const start = nowAtOffset(birth.offset);
  const end = { ...start, yyyy: String(parseInt(start.yyyy, 10) + years) };
  return { start, end, offset: birth.offset };
}

/** Auto-fill for GoodTimeFinder's Precision field when the preset (not custom range) path is used. */
export function computeDaysPerPixelForPreset(person: PersonLike, preset: TimeRangePreset, maxWidth = 940): number {
  const { start, end } = resolvePresetRange(person, preset);
  return roundDaysPerPixel(daysBetween(+start.yyyy, +start.mo, +start.dd, +end.yyyy, +end.mo, +end.dd), maxWidth);
}

/** Auto-fill for GoodTimeFinder's Precision field when a custom Year/Month range is used. */
export function computeDaysPerPixelForCustomRange(range: CustomRange, maxWidth = 940): number {
  const endDay = lastDayOfMonth(range.endYear, range.endMonth);
  return roundDaysPerPixel(daysBetween(range.startYear, range.startMonth, 1, range.endYear, range.endMonth, endDay), maxWidth);
}

/** Calculate/EventsChart/{specs} — fetches the raw SVG chart text for a person over a time range preset. */
export async function getEventsChartSvg(
  apiUrlDirect: string,
  person: PersonLike,
  preset: TimeRangePreset,
  options?: { maxWidth?: number; eventTagsCsv?: string; algorithmNamesCsv?: string; ayanamsaName?: string; daysPerPixelOverride?: number }
): Promise<string> {
  const maxWidth = options?.maxWidth ?? 940;
  const eventTagsCsv = options?.eventTagsCsv ?? 'PD1,PD2,PD3,PD4,PD5,PD6,PD7';
  const algorithmNamesCsv = options?.algorithmNamesCsv ?? 'General';
  const ayanamsaName = options?.ayanamsaName ?? 'Raman';

  const { start, end, offset } = resolvePresetRange(person, preset);

  const daysPerPixel = options?.daysPerPixelOverride ?? computeDaysPerPixelForPreset(person, preset, maxWidth);

  const url =
    `${apiUrlDirect}/EventsChart` +
    eventsChartUrl(person.id, start, end, offset, daysPerPixel, eventTagsCsv, algorithmNamesCsv) +
    `/Ayanamsa/${ayanamsaName}`;

  return pollForSvg(url);
}

/**
 * Year/Month range for GoodTimeFinder's "Custom" option — mirrors
 * ViewComponents/Components/MonthYearTimeRangeSelector.razor exactly (start = 00:00 on the 1st of
 * startMonth/startYear, end = 00:00 on the last day of endMonth/endYear, system-local timezone
 * offset, not the birth location's). This is a literal Start/End date pair, so it bypasses
 * Calculate.AutoCalculateTimeRange's preset math entirely — confirmed valid against
 * Library/Data/EventsChart.cs's FromUrl `processType1` branch, which parses explicit Start/End
 * segments with no preset involved.
 */
export type CustomRange = { startYear: number; startMonth: number; endYear: number; endMonth: number };

/** Last calendar day of a 1-indexed (Jan=1) month, accounting for leap years. */
function lastDayOfMonth(year: number, month1to12: number): number {
  return new Date(year, month1to12, 0).getDate();
}

/** Device-local UTC offset formatted as "+HH:mm"/"-HH:mm" — the client-side equivalent of Tools.GetSystemTimezoneStr(). */
function getLocalTimezoneOffset(): string {
  const offsetMinutes = -new Date().getTimezoneOffset();
  const sign = offsetMinutes >= 0 ? '+' : '-';
  const abs = Math.abs(offsetMinutes);
  const hh = String(Math.floor(abs / 60)).padStart(2, '0');
  const mm = String(abs % 60).padStart(2, '0');
  return `${sign}${hh}:${mm}`;
}

/** Calculate/EventsChart/{specs} — fetches the raw SVG chart text for a person over an explicit Year/Month range. */
export async function getEventsChartSvgCustomRange(
  apiUrlDirect: string,
  person: PersonLike,
  range: CustomRange,
  options?: { maxWidth?: number; eventTagsCsv?: string; algorithmNamesCsv?: string; ayanamsaName?: string; daysPerPixelOverride?: number }
): Promise<string> {
  const maxWidth = options?.maxWidth ?? 940;
  const eventTagsCsv = options?.eventTagsCsv ?? 'General,Personal';
  const algorithmNamesCsv = options?.algorithmNamesCsv ?? 'General';
  const ayanamsaName = options?.ayanamsaName ?? 'Raman';
  const offset = getLocalTimezoneOffset();

  const endDay = lastDayOfMonth(range.endYear, range.endMonth);
  const start = { hh: '00', mm: '00', dd: '01', mo: String(range.startMonth).padStart(2, '0'), yyyy: String(range.startYear) };
  const end = { hh: '00', mm: '00', dd: String(endDay).padStart(2, '0'), mo: String(range.endMonth).padStart(2, '0'), yyyy: String(range.endYear) };

  const daysPerPixel = options?.daysPerPixelOverride ?? computeDaysPerPixelForCustomRange(range, maxWidth);

  const url =
    `${apiUrlDirect}/EventsChart` +
    eventsChartUrl(person.id, start, end, offset, daysPerPixel, eventTagsCsv, algorithmNamesCsv) +
    `/Ayanamsa/${ayanamsaName}`;

  return pollForSvg(url);
}

/**
 * The endpoint is job-based, not request/response: a first call kicks off server-side generation
 * and immediately replies 200 with an empty body and a `Call-Status: Running` header (see
 * ChartCache.CacheExecute in Library/Logic/ChartCache.cs — confirmed live: polling the same URL
 * every few seconds eventually returns `Call-Status: Pass` with the full SVG body once the
 * server-side cache entry is ready). `Call-Status: Fail` means generation errored.
 */
async function pollForSvg(url: string, maxAttempts = 30, intervalMs = 2000): Promise<string> {
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    const response = await fetch(url);
    const status = response.headers.get('Call-Status');
    if (status === 'Fail') throw new Error('Failed to generate events chart');
    const text = await response.text();
    if (status !== 'Running' && text.length > 0) return stripScripts(text);
    await new Promise((resolve) => setTimeout(resolve, intervalMs));
  }
  throw new Error('Timed out waiting for events chart to generate');
}

/**
 * Strips <script> tags (jQuery/svg.js/VedAstro.js loader tags plus an inline ecmascript block for
 * the old tippy-tooltip interactivity) before handing the SVG to react-native-svg's SvgXml, which
 * only understands renderable SVG elements, not script execution.
 */
function stripScripts(svg: string): string {
  return svg.replace(/<script[\s\S]*?(?:\/>|<\/script>)/gi, '');
}

/** One life-event <rect> as emitted by EventsChartFactory.GenerateMultipleRowSvg (Library/Logic/Factory/EventsChartFactory.cs). */
export type EventRect = {
  x: number;
  width: number;
  eventName: string;
  eventDescription: string;
  natureScore: number;
  /** [{ category: 'Family', nature: 'Good' | 'Bad', weight: number }], parsed from the "summarycategories" attribute. */
  categories: { category: string; nature: 'Good' | 'Bad'; weight: number }[];
  age: string;
  stdTime: string;
};

const RECT_ATTR_REGEX = /<rect\b[^>]*\beventname="([^"]*)"[^>]*\/>/gi;
function attr(rectXml: string, name: string): string {
  const match = new RegExp(`\\b${name}="([^"]*)"`, 'i').exec(rectXml);
  return match ? match[1] : '';
}

/**
 * The root <svg>'s "contentpadding" attribute (EventsChartFactory.cs's contentHead) — every event
 * <rect>'s raw x/y attributes are visually shifted by this much via a wrapping <g transform>, so
 * any on-screen pixel coordinate (mouse/touch position) needs this added back before comparing
 * against a rect's raw x, or every hit-test misses by exactly this offset.
 */
export function parseContentPadding(svg: string): number {
  const match = /<svg\b[^>]*\bcontentpadding="(-?[\d.]+)"/i.exec(svg);
  return match ? parseFloat(match[1]) : 0;
}

/**
 * Extracts every life-event data-rect out of the raw chart SVG (attributes documented at the
 * emission site, EventsChartFactory.cs's GenerateMultipleRowSvg) — this is the same per-event data
 * the old EventsChart.js's tippy tooltips read client-side, now used to drive the Smart Summary
 * hover/touch tooltip instead of a DOM-dependent JS library.
 */
export function parseEventRects(svg: string): EventRect[] {
  const rects: EventRect[] = [];
  let match: RegExpExecArray | null;
  RECT_ATTR_REGEX.lastIndex = 0;
  while ((match = RECT_ATTR_REGEX.exec(svg))) {
    const rectXml = match[0];
    const categoriesRaw = attr(rectXml, 'summarycategories');
    const categories = categoriesRaw
      ? categoriesRaw.split(',').map((part) => {
          const [category, nature, weight] = part.split(':');
          return { category, nature: nature as 'Good' | 'Bad', weight: parseFloat(weight) || 0 };
        })
      : [];

    rects.push({
      x: parseFloat(attr(rectXml, 'x')) || 0,
      width: parseFloat(attr(rectXml, 'width')) || 0,
      eventName: match[1],
      eventDescription: attr(rectXml, 'eventdescription'),
      natureScore: parseFloat(attr(rectXml, 'naturescore')) || 0,
      categories,
      age: attr(rectXml, 'age'),
      stdTime: attr(rectXml, 'stdtime'),
    });
  }
  return rects;
}

const CATEGORY_PHRASES: Record<string, { good: string; bad: string }> = {
  Mind: { good: 'mental clarity', bad: 'mental stress' },
  Studies: { good: 'academic success', bad: 'academic struggles' },
  Family: { good: 'family growth', bad: 'family tension' },
  Money: { good: 'financial gains', bad: 'financial setbacks' },
  Love: { good: 'romantic success', bad: 'relationship challenges' },
  Body: { good: 'good health', bad: 'health concerns' },
};

/** Joins ["a", "b", "c"] into "a, b, and c" (Oxford comma), or "a and b" for two, or "a" for one. */
function joinPhrases(phrases: string[]): string {
  if (phrases.length === 0) return '';
  if (phrases.length === 1) return phrases[0];
  if (phrases.length === 2) return `${phrases[0]} and ${phrases[1]}`;
  return `${phrases.slice(0, -1).join(', ')}, and ${phrases[phrases.length - 1]}`;
}

/**
 * Synthesizes a one-line "Smart Summary" sentence (see LifePredictor screenshot) from the
 * SpecializedSummary categories of every event active at a hovered/touched x position — mirrors
 * the aggregation the old EventsChart.js did client-side per column, but condensed into a single
 * readable sentence instead of a per-event legend list.
 */
export function buildSmartSummary(rectsAtPosition: EventRect[]): string {
  const weightByCategory = new Map<string, { good: number; bad: number }>();
  for (const rect of rectsAtPosition) {
    for (const { category, nature, weight } of rect.categories) {
      const entry = weightByCategory.get(category) ?? { good: 0, bad: 0 };
      const effectiveWeight = weight > 0 ? weight : 1;
      if (nature === 'Good') entry.good += effectiveWeight;
      else if (nature === 'Bad') entry.bad += effectiveWeight;
      weightByCategory.set(category, entry);
    }
  }

  const positives: { phrase: string; weight: number }[] = [];
  const negatives: { phrase: string; weight: number }[] = [];
  for (const [category, { good, bad }] of weightByCategory) {
    const phrases = CATEGORY_PHRASES[category];
    if (!phrases) continue;
    if (good > bad) positives.push({ phrase: phrases.good, weight: good });
    else if (bad > good) negatives.push({ phrase: phrases.bad, weight: bad });
  }
  positives.sort((a, b) => b.weight - a.weight);
  negatives.sort((a, b) => b.weight - a.weight);

  const positivePhrases = joinPhrases(positives.slice(0, 3).map((p) => p.phrase));
  const negativePhrases = joinPhrases(negatives.slice(0, 2).map((p) => p.phrase));

  if (positivePhrases && negativePhrases) {
    return `Positive ${positivePhrases}, despite ${negativePhrases}.`;
  }
  if (positivePhrases) {
    return `Positive ${positivePhrases} ahead.`;
  }
  if (negativePhrases) {
    return `Watch out for ${negativePhrases}.`;
  }
  return 'A quiet, neutral period.';
}
