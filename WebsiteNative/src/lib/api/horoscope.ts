import { timeToUrl, type BirthTimeJson } from '@/lib/time';

/**
 * Port of Website/Pages/Calculator/Horoscope.razor's data-fetching logic. The original page let
 * vedastro.js's GenerateAstroTable/GenerateAshtakvargaTable drive everything by reflection
 * metadata (one HTTP call per table cell, hitting whatever Calculate/{name} endpoint a column
 * declared) — here each column/chart is a plain typed fetch against the same
 * reflection-dispatched endpoints (see API/FrontDesk/OpenAPI.cs, Library/Logic/Calculate/*.cs),
 * since the endpoints themselves are just GET Calculate/{methodName}/{args...} routes returning
 * {Status, Payload}.
 */

export const PLANET_NAMES = [
  'Sun',
  'Moon',
  'Mars',
  'Mercury',
  'Jupiter',
  'Venus',
  'Saturn',
  'Rahu',
  'Ketu',
] as const;
export type PlanetName = (typeof PLANET_NAMES)[number];

export const HOUSE_NAMES = [
  'House1',
  'House2',
  'House3',
  'House4',
  'House5',
  'House6',
  'House7',
  'House8',
  'House9',
  'House10',
  'House11',
  'House12',
] as const;
export type HouseNameStr = (typeof HOUSE_NAMES)[number];

/** Default ayanamsa matches Horoscope.razor's _selectedAyanamsa default. */
export const DEFAULT_AYANAMSA = 'Raman';

/** Mirrors Library/Data/HoroscopePrediction.cs's ToJson(). */
export type HoroscopePrediction = {
  name: string;
  formattedName: string;
  description: string;
  relatedBody: unknown;
};

function formatName(name: string): string {
  // mirrors Format.FormatName: insert a space before each interior capital letter
  return name.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
}

function predictionFromJson(json: any): HoroscopePrediction {
  return {
    name: json.Name,
    formattedName: formatName(json.Name ?? ''),
    description: json.Description ?? '',
    relatedBody: json.RelatedBody,
  };
}

/** Calculate/HoroscopePredictions/{timeUrl} — used by both HoroscopeReferenceList and AIPrediction. */
export async function getHoroscopePredictions(
  apiUrlDirect: string,
  birthTime: BirthTimeJson
): Promise<HoroscopePrediction[]> {
  const response = await fetch(`${apiUrlDirect}/Calculate/HoroscopePredictions${timeToUrl(birthTime)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') return [];
  return (json.Payload as any[]).map(predictionFromJson);
}

/** One GET to a Calculate/{name} endpoint taking (PlanetName|HouseName, Time, Ayanamsa). */
async function callCalculate(
  apiUrlDirect: string,
  endpoint: string,
  subjectParamName: 'PlanetName' | 'HouseName',
  subjectValue: string,
  birthTime: BirthTimeJson,
  ayanamsa: string
): Promise<string> {
  const url = `${apiUrlDirect}/Calculate/${endpoint}/${subjectParamName}/${subjectValue}${timeToUrl(birthTime)}Ayanamsa/${ayanamsa}`;
  try {
    const response = await fetch(url);
    const json = await response.json();
    if (json.Status !== 'Pass') return '—';
    const payload = json.Payload?.[endpoint] ?? json.Payload;
    return formatPayloadValue(payload);
  } catch {
    return '—';
  }
}

function formatPayloadValue(value: unknown): string {
  if (value == null) return '—';
  if (Array.isArray(value)) return value.map(formatPayloadValue).join(', ');
  if (typeof value === 'object') return Object.values(value as object).map(String).join(', ');
  return formatName(String(value));
}

/** Columns matching Horoscope.razor's `planetColumns` (the 6 Enabled ones). */
export const PLANET_TABLE_COLUMNS = [
  { endpoint: 'PlanetZodiacSign', name: 'Sign' },
  { endpoint: 'PlanetConstellation', name: 'Constellation' },
  { endpoint: 'HousePlanetOccupies', name: 'Occupies' },
  { endpoint: 'HousesOwnedByPlanet', name: 'Owns' },
  { endpoint: 'PlanetLordOfZodiacSign', name: 'Sign Lord' },
  { endpoint: 'PlanetLordOfConstellation', name: 'Const. Lord' },
] as const;

export type PlanetTableRow = { planet: PlanetName; values: string[] };

export async function getPlanetTable(
  apiUrlDirect: string,
  birthTime: BirthTimeJson,
  ayanamsa: string
): Promise<PlanetTableRow[]> {
  return Promise.all(
    PLANET_NAMES.map(async (planet) => ({
      planet,
      values: await Promise.all(
        PLANET_TABLE_COLUMNS.map((col) => callCalculate(apiUrlDirect, col.endpoint, 'PlanetName', planet, birthTime, ayanamsa))
      ),
    }))
  );
}

/**
 * Columns matching Horoscope.razor's `houseColumns` Enabled set. "LordOfConstellation" is
 * skipped, same as the original (Enabled = false there). "Aspects"/PlanetsAspectingHouse is kept
 * even though no Calculate.PlanetsAspectingHouse(HouseName,Time) method actually exists in
 * Library today (only a documentation-only entry in OpenAPIStaticTable.cs, not a real
 * implementation) — callCalculate degrades to "—" per-cell on a failed/missing endpoint rather
 * than crashing the whole table, so this is a known gap rather than a silent one.
 */
export const HOUSE_TABLE_COLUMNS = [
  { endpoint: 'HouseZodiacSign', name: 'Sign' },
  { endpoint: 'HouseConstellation', name: 'Constellation' },
  { endpoint: 'PlanetsInHouse', name: 'Planets In' },
  { endpoint: 'LordOfHouse', name: 'Sign Lord' },
  { endpoint: 'PlanetsAspectingHouse', name: 'Aspects' },
] as const;

export type HouseTableRow = { house: HouseNameStr; values: string[] };

export async function getHouseTable(
  apiUrlDirect: string,
  birthTime: BirthTimeJson,
  ayanamsa: string
): Promise<HouseTableRow[]> {
  return Promise.all(
    HOUSE_NAMES.map(async (house) => ({
      house,
      values: await Promise.all(
        HOUSE_TABLE_COLUMNS.map((col) => callCalculate(apiUrlDirect, col.endpoint, 'HouseName', house, birthTime, ayanamsa))
      ),
    }))
  );
}

/** Row shape shared by Sarvashtakavarga/Bhinnashtakavarga (see Library/Data/Sarvashtakavarga.cs's ToJson()). */
export type AshtakvargaRow = { key: string; points: number[]; total: number };

async function fetchAshtakavargaChart(
  apiUrlDirect: string,
  endpoint: string,
  birthTime: BirthTimeJson,
  ayanamsa: string
): Promise<AshtakvargaRow[]> {
  const url = `${apiUrlDirect}/Calculate/${endpoint}${timeToUrl(birthTime)}Ayanamsa/${ayanamsa}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') return [];
  const payload = json.Payload?.[endpoint] ?? json.Payload;
  return Object.entries(payload as Record<string, any>).map(([key, value]) => {
    // SarvashtakavargaChart rows are {Total, Rows: number[12]}; BhinnashtakavargaChart rows are
    // a plain {ZodiacName: points} map with no precomputed total.
    if (value && typeof value === 'object' && Array.isArray(value.Rows)) {
      return { key, points: value.Rows, total: value.Total };
    }
    const points = Object.values(value as Record<string, number>);
    return { key, points, total: points.reduce((a, b) => a + b, 0) };
  });
}

/** Calculate/SarvashtakavargaChart/{timeUrl}Ayanamsa/{ayanamsa} — one row per planet + Sarvashtakavarga total. */
export function getSarvashtakavargaChart(apiUrlDirect: string, birthTime: BirthTimeJson, ayanamsa: string) {
  return fetchAshtakavargaChart(apiUrlDirect, 'SarvashtakavargaChart', birthTime, ayanamsa);
}

/** Calculate/BhinnashtakavargaChart/{timeUrl}Ayanamsa/{ayanamsa} — one row per planet. */
export function getBhinnashtakavargaChart(apiUrlDirect: string, birthTime: BirthTimeJson, ayanamsa: string) {
  return fetchAshtakavargaChart(apiUrlDirect, 'BhinnashtakavargaChart', birthTime, ayanamsa);
}

/** Calculate/SkyChart{timeUrl} — server-rendered image, no JSON (see SkyChartViewer.razor). */
export function getSkyChartImageUrl(apiUrlDirect: string, birthTime: BirthTimeJson): string {
  return `${apiUrlDirect}/Calculate/SkyChart${timeToUrl(birthTime)}`;
}

/** Calculate/{chartStyle}IndianChart{timeUrl} — server-rendered image (see IndianChart.razor). */
export function getIndianChartImageUrl(
  apiUrlDirect: string,
  birthTime: BirthTimeJson,
  chartStyle: 'South' | 'North'
): string {
  return `${apiUrlDirect}/Calculate/${chartStyle}IndianChart${timeToUrl(birthTime)}`;
}
