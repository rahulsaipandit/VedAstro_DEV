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
  try {
    const response = await fetch(`${apiUrlDirect}/Calculate/HoroscopePredictions${timeToUrl(birthTime)}`);
    const json = await response.json();
    if (json.Status !== 'Pass') return [];
    return (json.Payload as any[]).map(predictionFromJson);
  } catch {
    return [];
  }
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

/** Constellation.ToJson() shape: {Name, Quarter, DegreesIn:{...}} — render as "Mrigasira - 3". */
function isConstellationShape(value: object): value is { Name: string; Quarter: number } {
  return 'Name' in value && 'Quarter' in value;
}

/** ZodiacSign.ToJson() shape: {Name, DegreesIn:{DegreeMinuteSecond,...}} — render as "Gemini 2° 0' 57". */
function isZodiacSignShape(value: object): value is { Name: string; DegreesIn: { DegreeMinuteSecond: string } } {
  return 'Name' in value && 'DegreesIn' in value && typeof (value as any).DegreesIn?.DegreeMinuteSecond === 'string';
}

function formatPayloadValue(value: unknown): string {
  if (value == null) return '—';
  if (Array.isArray(value)) return value.map(formatPayloadValue).join(', ');
  if (typeof value === 'object') {
    if (isConstellationShape(value as object)) {
      const v = value as { Name: string; Quarter: number };
      return `${v.Name} - ${v.Quarter}`;
    }
    if (isZodiacSignShape(value as object)) {
      const v = value as { Name: string; DegreesIn: { DegreeMinuteSecond: string } };
      return `${v.Name} ${v.DegreesIn.DegreeMinuteSecond}`;
    }
    return Object.values(value as object).map(String).join(', ');
  }
  return formatName(String(value));
}

/** Columns matching Horoscope.razor's `planetColumns` (the 6 Enabled ones). */
export const PLANET_TABLE_COLUMNS = [
  { endpoint: 'PlanetZodiacSign', name: 'Planet Rasi D1 Sign' },
  { endpoint: 'PlanetConstellation', name: 'Planet Constellation' },
  { endpoint: 'HousePlanetOccupies', name: 'House Planet Occupies Based On Sign' },
  { endpoint: 'HousesOwnedByPlanet', name: 'Houses Owned By Planet' },
  { endpoint: 'PlanetLordOfZodiacSign', name: 'Planet Lord Of Zodiac Sign' },
  { endpoint: 'PlanetLordOfConstellation', name: 'Planet Lord Of Constellation' },
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
 * Columns matching Horoscope.razor's `houseColumns` Enabled set, plus HouseConstellationLord
 * (Library/Logic/Calculate/Core.cs's HouseConstellationLord(HouseName,Time)).
 */
export const HOUSE_TABLE_COLUMNS = [
  { endpoint: 'HouseZodiacSign', name: 'House Rasi Sign' },
  { endpoint: 'HouseConstellation', name: 'House Constellation' },
  { endpoint: 'PlanetsInHouse', name: 'Planets In House Based On Sign' },
  { endpoint: 'LordOfHouse', name: 'Lord Of House' },
  { endpoint: 'HouseConstellationLord', name: 'House Constellation Lord' },
  { endpoint: 'PlanetsAspectingHouse', name: 'Planets Aspecting House' },
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
  try {
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
  } catch {
    return [];
  }
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
