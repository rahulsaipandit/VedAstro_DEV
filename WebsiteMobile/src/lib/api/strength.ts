import { timeToUrl, type BirthTimeJson } from '@/lib/time';
import { PLANET_NAMES, HOUSE_NAMES, type PlanetName, type HouseNameStr } from '@/lib/api/horoscope';

/**
 * Backs StrengthChart.tsx — the RN port of ViewComponents/Components/StrengthChart.razor +
 * PlanetChart.razor. Both were deferred until this session's fix to
 * Library/Data/Shashtiamsa.cs: it had no public properties, so the reflection-dispatched
 * endpoints returning it (PlanetShadbalaPinda, HouseStrength) always serialized as an empty
 * `{}` (confirmed live before the fix). Now fixed, this uses:
 * - Calculate.PlanetPowerPercentage(PlanetName, Time) — already scaled 0-100 by the strongest/
 *   weakest planet at that time, no client-side normalization needed.
 * - Calculate.HouseStrength(HouseName, Time) — a Shashtiamsa (now {AsDouble, AsRupa}), normalized
 *   to 0-100 client-side against the max of the 12 houses (mirrors the spirit of the original's
 *   dynamic bar scaling in JS.DrawHouseStrengthChart).
 */

export type PlanetPowerRow = { planet: PlanetName; percentage: number };
export type HousePowerRow = { house: HouseNameStr; percentage: number; rupas: number };

async function fetchPercentage(apiUrlDirect: string, endpoint: string, birthTime: BirthTimeJson): Promise<number> {
  const response = await fetch(`${apiUrlDirect}/Calculate/${endpoint}${timeToUrl(birthTime)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') return 0;
  return json.Payload as number;
}

export async function getPlanetPowerList(apiUrlDirect: string, birthTime: BirthTimeJson): Promise<PlanetPowerRow[]> {
  return Promise.all(
    PLANET_NAMES.map(async (planet) => ({
      planet,
      percentage: await fetchPercentage(apiUrlDirect, `PlanetPowerPercentage/PlanetName/${planet}`, birthTime),
    }))
  );
}

async function fetchHouseStrengthRupas(apiUrlDirect: string, house: HouseNameStr, birthTime: BirthTimeJson): Promise<number> {
  const response = await fetch(`${apiUrlDirect}/Calculate/HouseStrength/HouseName/${house}${timeToUrl(birthTime)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') return 0;
  return (json.Payload?.AsRupa as number) ?? 0;
}

export async function getHousePowerList(apiUrlDirect: string, birthTime: BirthTimeJson): Promise<HousePowerRow[]> {
  const rupas = await Promise.all(HOUSE_NAMES.map((house) => fetchHouseStrengthRupas(apiUrlDirect, house, birthTime)));
  const max = Math.max(...rupas, 0.0001);
  return HOUSE_NAMES.map((house, index) => ({
    house,
    rupas: rupas[index],
    percentage: (rupas[index] / max) * 100,
  }));
}
