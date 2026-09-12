/**
 * Mirrors Library/Data/Time.cs's JSON shape (Time.ToJson()/FromJson()) and its ToUrl() format.
 * Person.birthTime (see src/lib/api/person.ts) is exactly this shape, serialized as a JSON
 * string in Person.ToJson()'s wire format, but the GetPersonList endpoint returns it already
 * parsed as an object, so this operates on the parsed object directly.
 */
export type BirthTimeJson = {
  StdTime: string; // "HH:mm dd/MM/yyyy zzz"
  Location: {
    Name: string;
    Longitude: number;
    Latitude: number;
  };
};

/**
 * Builds the "/Location/{name}/Time/{HH:mm}/{dd}/{MM}/{yyyy}/{zzz}" URL segment used by every
 * Calculate/* endpoint that takes a birth time, matching Time.cs's ToUrl() exactly (location
 * name with whitespace stripped, date/time slash-separated in place of the space-separated
 * StdTime string).
 */
export function timeToUrl(birthTime: unknown): string {
  const time = birthTime as BirthTimeJson;
  const locationName = (time.Location?.Name ?? '').replace(/\s/g, '');
  const formattedTime = (time.StdTime ?? '').replace(/ /g, '/');
  return `/Location/${locationName}/Time/${formattedTime}`;
}

/**
 * Mirrors Calculate.LongitudeToLMTOffset (Library/Logic/Calculate/CoreTime.cs) exactly, including
 * the rounding-to-whole-minute fix from that method (the Phase 1+2 migration's "every real-world
 * longitude threw an exception" bug) — pure arithmetic, no API call needed.
 */
export function longitudeToLmtOffsetMinutes(longitudeDeg: number): number {
  return Math.round(((longitudeDeg / 15) * 60) as number);
}

function formatStdTimeText(date: Date, offsetMinutes: number): string {
  const shifted = new Date(date.getTime() + offsetMinutes * 60_000);
  const pad = (n: number) => String(n).padStart(2, '0');
  const hh = pad(shifted.getUTCHours());
  const mm = pad(shifted.getUTCMinutes());
  const dd = pad(shifted.getUTCDate());
  const mo = pad(shifted.getUTCMonth() + 1);
  const yyyy = shifted.getUTCFullYear();
  const sign = offsetMinutes < 0 ? '-' : '+';
  const absMin = Math.abs(offsetMinutes);
  const offHh = pad(Math.floor(absMin / 60));
  const offMm = pad(absMin % 60);
  return `${hh}:${mm} ${dd}/${mo}/${yyyy} ${sign}${offHh}:${offMm}`;
}

/** Builds a BirthTimeJson for "this exact instant" at a location, given its real STD offset in minutes. */
export function buildBirthTimeJson(
  instant: Date,
  stdOffsetMinutes: number,
  location: { name: string; longitude: number; latitude: number }
): BirthTimeJson {
  return {
    StdTime: formatStdTimeText(instant, stdOffsetMinutes),
    Location: { Name: location.name, Longitude: location.longitude, Latitude: location.latitude },
  };
}

/** Local Mean Time text for an instant at a location, using the pure-math longitude-based offset (not the real STD offset). */
export function lmtTextForInstant(instant: Date, longitudeDeg: number): string {
  return formatStdTimeText(instant, longitudeToLmtOffsetMinutes(longitudeDeg));
}

/**
 * Builds a BirthTimeJson from a user-entered wall-clock date/time (already local to the given
 * location, not UTC) plus that location's resolved STD offset string (e.g. from
 * getTimezoneOffsetForLocation) — used by the person Add/Editor forms, where the user types the
 * actual birth date/time reading, not "now".
 */
export function buildBirthTimeJsonFromWallClock(
  dd: string,
  mm: string,
  yyyy: string,
  hh: string,
  min: string,
  offset: string,
  location: { name: string; longitude: number; latitude: number }
): BirthTimeJson {
  const pad = (n: string) => n.padStart(2, '0');
  return {
    StdTime: `${pad(hh)}:${pad(min)} ${pad(dd)}/${pad(mm)}/${yyyy} ${offset}`,
    Location: { Name: location.name, Longitude: location.longitude, Latitude: location.latitude },
  };
}
