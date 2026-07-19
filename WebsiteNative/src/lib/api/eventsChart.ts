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
 * Mirrors Calculate.AutoCalculateTimeRange's preset math exactly (Library/Logic/Calculate/CoreMisc.cs)
 * — pure date arithmetic off the birth date, no API call needed. Only the subset of presets
 * LifePredictor/GoodTimeFinder actually default to is implemented (full life + a few multi-year
 * spans); the free-text "age1to10"/"1990-2000" forms aren't exposed in the simplified RN UI.
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

/** Calculate/EventsChart/{specs} — fetches the raw SVG chart text for a person over a time range preset. */
export async function getEventsChartSvg(
  apiUrlDirect: string,
  person: PersonLike,
  preset: TimeRangePreset,
  options?: { maxWidth?: number; eventTagsCsv?: string; algorithmNamesCsv?: string; ayanamsaName?: string }
): Promise<string> {
  const maxWidth = options?.maxWidth ?? 940;
  const eventTagsCsv = options?.eventTagsCsv ?? 'PD1,PD2,PD3,PD4,PD5,PD6,PD7';
  const algorithmNamesCsv = options?.algorithmNamesCsv ?? 'General';
  const ayanamsaName = options?.ayanamsaName ?? 'Raman';

  const birth = parseStdTime(person.birthTime.StdTime);
  const years = presetToYears(preset);
  const startYear = parseInt(birth.yyyy, 10);
  const endYear = startYear + years;

  const start = birth;
  const end = { ...birth, yyyy: String(endYear) };

  const daysPerPixel = Math.round((daysBetween(startYear, +birth.mo, +birth.dd, endYear, +birth.mo, +birth.dd) / maxWidth) * 1000) / 1000;

  const url =
    `${apiUrlDirect}/EventsChart` +
    eventsChartUrl(person.id, start, end, birth.offset, daysPerPixel, eventTagsCsv, algorithmNamesCsv) +
    `/Ayanamsa/${ayanamsaName}`;

  return pollForSvg(url);
}

/**
 * The endpoint is job-based, not request/response: a first call kicks off server-side generation
 * and immediately replies 200 with an empty body and a `Call-Status: Running` header (see
 * AzureCache.CacheExecute in Library/Logic/AzureCache.cs — confirmed live: polling the same URL
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
