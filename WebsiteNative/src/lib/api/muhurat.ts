/**
 * Backs Muhurt.tsx and the "Ekadashi & Vrat" tab on FestivalCalendar.tsx. Calls the new
 * Hora/Kaal/Lagna/GandMool/Ekadashi calculators added to Library/Logic/Calculate/Panchang.cs and
 * Library/Logic/Calculate/FestivalCalendar.cs, following the same direct-Payload response shape
 * as the existing getChoghadiyaPeriods/getDailyPanchang in festivalCalendar.ts (these are sibling
 * methods added to the same two files).
 */
import { geoLocationToUrl, type GeoLocation } from '@/lib/api/geo';
import { timeToUrl, type BirthTimeJson } from '@/lib/time';

export type PlanetName = 'Sun' | 'Moon' | 'Mars' | 'Mercury' | 'Jupiter' | 'Venus' | 'Saturn';

export type HoraPeriod = {
  lord: PlanetName;
  start: BirthTimeJson;
  end: BirthTimeJson;
  isDayPeriod: boolean;
};

export async function getHoraPeriods(apiUrlDirect: string, date: BirthTimeJson): Promise<HoraPeriod[]> {
  const response = await fetch(`${apiUrlDirect}/Calculate/HoraPeriods${timeToUrl(date)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Hora periods');
  return (json.Payload as any[]).map((p) => ({
    lord: p.Lord,
    start: p.Start,
    end: p.End,
    isDayPeriod: p.IsDayPeriod,
  }));
}

export type ZodiacName =
  | 'Aries' | 'Taurus' | 'Gemini' | 'Cancer' | 'Leo' | 'Virgo'
  | 'Libra' | 'Scorpio' | 'Sagittarius' | 'Capricorn' | 'Aquarius' | 'Pisces';

export type LagnaPeriod = {
  sign: ZodiacName;
  start: BirthTimeJson;
  end: BirthTimeJson;
};

export async function getLagnaPeriods(apiUrlDirect: string, date: BirthTimeJson): Promise<LagnaPeriod[]> {
  const response = await fetch(`${apiUrlDirect}/Calculate/LagnaPeriods${timeToUrl(date)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Lagna periods');
  return (json.Payload as any[]).map((p) => ({
    sign: p.Sign,
    start: p.Start,
    end: p.End,
  }));
}

export type TimeRange = {
  start: BirthTimeJson;
  end: BirthTimeJson;
};

async function getKaalPeriod(apiUrlDirect: string, calculatorName: 'RahuKaal' | 'GulikaKaal' | 'YamagandaKaal', date: BirthTimeJson): Promise<TimeRange> {
  const response = await fetch(`${apiUrlDirect}/Calculate/${calculatorName}${timeToUrl(date)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(`Failed to calculate ${calculatorName}`);
  const payload = json.Payload;
  return { start: payload.Start, end: payload.End };
}

export const getRahuKaal = (apiUrlDirect: string, date: BirthTimeJson) => getKaalPeriod(apiUrlDirect, 'RahuKaal', date);
export const getGulikaKaal = (apiUrlDirect: string, date: BirthTimeJson) => getKaalPeriod(apiUrlDirect, 'GulikaKaal', date);
export const getYamagandaKaal = (apiUrlDirect: string, date: BirthTimeJson) => getKaalPeriod(apiUrlDirect, 'YamagandaKaal', date);

export async function getGandMoolPeriods(apiUrlDirect: string, date: BirthTimeJson, daysAhead: number): Promise<TimeRange[]> {
  const response = await fetch(`${apiUrlDirect}/Calculate/GandMoolPeriods${timeToUrl(date)}/DaysAhead/${daysAhead}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Gand Mool periods');
  return (json.Payload as any[]).map((p) => ({ start: p.Start, end: p.End }));
}

export type EkadashiOccurrence = {
  name: string;
  date: BirthTimeJson;
};

export async function getEkadashiCalendar(apiUrlDirect: string, year: number, location: GeoLocation): Promise<EkadashiOccurrence[]> {
  const url = `${apiUrlDirect}/Calculate/EkadashiCalendar/Year/${year}${geoLocationToUrl(location)}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Ekadashi calendar');
  return (json.Payload as any[]).map((o) => ({ name: o.Name, date: o.Date }));
}
