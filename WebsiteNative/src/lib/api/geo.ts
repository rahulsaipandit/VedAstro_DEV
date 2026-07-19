/**
 * Backs GeoLocationInput.tsx — the RN port of ViewComponents/Components/GeoLocationInput.razor.
 * Both endpoints are real, already-migrated (non-Azure) Calculate/* methods, confirmed by
 * reading Library/Logic/Calculate/CoreMisc.cs directly rather than assuming from the old
 * Blazor-side Tools.AddressToGeoLocation/GetTimezoneOffsetApi wrapper names:
 * - Calculate.AddressToGeoLocation(string) geocodes via the free Nominatim (OpenStreetMap) API,
 *   no key required (see its doc comment's own example URL: .../Calculate/AddressToGeoLocation/Address/Gaithersburg).
 * - Calculate.GeoLocationToTimezone(GeoLocation, DateTimeOffset) looks up the real (DST-aware)
 *   standard-time offset for a location at a given instant via timeapi.io, falling back to a
 *   longitude-based offset if unreachable.
 */
export type GeoLocation = {
  name: string;
  longitude: number;
  latitude: number;
};

function geoLocationFromJson(json: any): GeoLocation {
  return { name: json.Name, longitude: json.Longitude, latitude: json.Latitude };
}

/** Mirrors GeoLocation.cs's FromUrl()/ToUrl(): "/Location/{name}/Coordinates/{latitude},{longitude}". */
export function geoLocationToUrl(location: GeoLocation): string {
  return `/Location/${encodeURIComponent(location.name)}/Coordinates/${location.latitude},${location.longitude}`;
}

/** Mirrors Tools.cs's DateTimeOffsetFromUrl()/ToUrl(): "/Time/{HH:mm}/{dd}/{MM}/{yyyy}/{zzz}". */
export function dateTimeOffsetToUrl(date: Date, offsetMinutes: number): string {
  const shifted = new Date(date.getTime() + offsetMinutes * 60_000);
  const pad = (n: number) => String(n).padStart(2, '0');
  const hh = pad(shifted.getUTCHours());
  const mm = pad(shifted.getUTCMinutes());
  const dd = pad(shifted.getUTCDate());
  const mo = pad(shifted.getUTCMonth() + 1);
  const yyyy = shifted.getUTCFullYear();
  return `/Time/${hh}:${mm}/${dd}/${mo}/${yyyy}/${formatOffsetMinutes(offsetMinutes)}`;
}

/** "+HH:mm" / "-HH:mm", matching DateTimeOffset's "zzz" format used throughout Library/Data/Time.cs. */
export function formatOffsetMinutes(offsetMinutes: number): string {
  const sign = offsetMinutes < 0 ? '-' : '+';
  const abs = Math.abs(offsetMinutes);
  const hh = String(Math.floor(abs / 60)).padStart(2, '0');
  const mm = String(abs % 60).padStart(2, '0');
  return `${sign}${hh}:${mm}`;
}

/** Parses a "+HH:mm"/"-HH:mm" offset string (e.g. from GeoLocationToTimezone) back to minutes. */
export function parseOffsetMinutes(offset: string): number {
  const match = /^([+-])(\d{2}):(\d{2})$/.exec(offset.trim());
  if (!match) return 0;
  const sign = match[1] === '-' ? -1 : 1;
  return sign * (parseInt(match[2], 10) * 60 + parseInt(match[3], 10));
}

/** Calculate/AddressToGeoLocation/Address/{address} — returns null if not found/geocoding failed. */
export async function searchGeoLocation(apiUrlDirect: string, address: string): Promise<GeoLocation | null> {
  const response = await fetch(`${apiUrlDirect}/Calculate/AddressToGeoLocation/Address/${encodeURIComponent(address)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') return null;
  const location = geoLocationFromJson(json.Payload);
  if (!location.name || (location.longitude === 0 && location.latitude === 0)) return null;
  return location;
}

/**
 * Calculate/CoordinatesToGeoLocation/Latitude/{lat}/Longitude/{lon} — reverse geocoding (browser
 * GPS coords -> a friendly place name), backed by the free Nominatim (OpenStreetMap) API, same
 * as searchGeoLocation. Returns null if not found/geocoding failed, so callers can fall back to
 * showing the raw coordinates.
 */
export async function reverseGeocodeLocation(
  apiUrlDirect: string,
  latitude: number,
  longitude: number
): Promise<GeoLocation | null> {
  const response = await fetch(
    `${apiUrlDirect}/Calculate/CoordinatesToGeoLocation/Latitude/${latitude}/Longitude/${longitude}`
  );
  const json = await response.json();
  if (json.Status !== 'Pass') return null;
  const location = geoLocationFromJson(json.Payload);
  if (!location.name) return null;
  return location;
}

/** Calculate/GeoLocationToTimezone/{geoLocationUrl}{dateTimeOffsetUrl} — real standard-time offset ("+08:00"-style) at a location and instant. */
export async function getTimezoneOffsetForLocation(
  apiUrlDirect: string,
  location: GeoLocation,
  atInstant: Date
): Promise<string> {
  const url = `${apiUrlDirect}/Calculate/GeoLocationToTimezone${geoLocationToUrl(location)}${dateTimeOffsetToUrl(atInstant, 0)}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to get timezone offset');
  return json.Payload as string;
}
