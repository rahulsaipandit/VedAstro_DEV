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

export async function getDurMuhurtaPeriods(apiUrlDirect: string, date: BirthTimeJson): Promise<TimeRange[]> {
  const response = await fetch(`${apiUrlDirect}/Calculate/DurMuhurtaPeriods${timeToUrl(date)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Dur Muhurta periods');
  return (json.Payload as any[]).map((p) => ({ start: p.Start, end: p.End }));
}

export async function getVikramSamvatYear(apiUrlDirect: string, date: BirthTimeJson): Promise<number> {
  const response = await fetch(`${apiUrlDirect}/Calculate/VikramSamvatYear${timeToUrl(date)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Vikram Samvat year');
  return json.Payload as number;
}

export type LunarDayInfo = {
  name: string;
  paksha: string;
  date: string;
  day: string;
  phase: string;
};

export type ConstellationInfo = {
  name: string;
  quarter: number;
};

export type YogaInfo = {
  name: string;
  description: string;
};

function lunarDayFromJson(json: any): LunarDayInfo {
  return { name: json.Name, paksha: json.Paksha, date: json.Date, day: json.Day, phase: json.Phase };
}

function constellationFromJson(json: any): ConstellationInfo {
  return { name: json.Name, quarter: json.Quarter };
}

function yogaFromJson(json: any): YogaInfo {
  return { name: json.Name, description: json.Description };
}

export type TithiTransition = { current: LunarDayInfo; endTime: BirthTimeJson; next: LunarDayInfo };

export async function getTithiTransition(apiUrlDirect: string, time: BirthTimeJson): Promise<TithiTransition> {
  const response = await fetch(`${apiUrlDirect}/Calculate/TithiTransition${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Tithi transition');
  const payload = json.Payload;
  return { current: lunarDayFromJson(payload.Current), endTime: payload.EndTime, next: lunarDayFromJson(payload.Next) };
}

export type NakshatraTransition = { current: ConstellationInfo; endTime: BirthTimeJson; next: ConstellationInfo };

export async function getNakshatraTransition(apiUrlDirect: string, time: BirthTimeJson): Promise<NakshatraTransition> {
  const response = await fetch(`${apiUrlDirect}/Calculate/NakshatraTransition${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Nakshatra transition');
  const payload = json.Payload;
  return { current: constellationFromJson(payload.Current), endTime: payload.EndTime, next: constellationFromJson(payload.Next) };
}

export type YogaTransition = { current: YogaInfo; endTime: BirthTimeJson; next: YogaInfo };

export async function getYogaTransition(apiUrlDirect: string, time: BirthTimeJson): Promise<YogaTransition> {
  const response = await fetch(`${apiUrlDirect}/Calculate/YogaTransition${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Yoga transition');
  const payload = json.Payload;
  return { current: yogaFromJson(payload.Current), endTime: payload.EndTime, next: yogaFromJson(payload.Next) };
}

export type KaranaTransition = { current: string; endTime: BirthTimeJson; next: string };

export async function getKaranaTransition(apiUrlDirect: string, time: BirthTimeJson): Promise<KaranaTransition> {
  const response = await fetch(`${apiUrlDirect}/Calculate/KaranaTransition${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Karana transition');
  const payload = json.Payload;
  return { current: payload.Current, endTime: payload.EndTime, next: payload.Next };
}

export type MoonZodiacTransition = { current: ZodiacName; endTime: BirthTimeJson; next: ZodiacName };

export async function getMoonZodiacTransition(apiUrlDirect: string, time: BirthTimeJson): Promise<MoonZodiacTransition> {
  const response = await fetch(`${apiUrlDirect}/Calculate/MoonZodiacTransition${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Moon zodiac transition');
  const payload = json.Payload;
  return { current: payload.Current, endTime: payload.EndTime, next: payload.Next };
}

export async function getSunZodiacSign(apiUrlDirect: string, time: BirthTimeJson): Promise<ZodiacName> {
  const response = await fetch(`${apiUrlDirect}/Calculate/PlanetZodiacSign/PlanetName/Sun${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Sun zodiac sign');
  return json.Payload.Name as ZodiacName;
}

export async function getPlanetNakshatra(apiUrlDirect: string, planet: PlanetName, time: BirthTimeJson): Promise<ConstellationInfo> {
  const response = await fetch(`${apiUrlDirect}/Calculate/PlanetConstellation/PlanetName/${planet}${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(`Failed to calculate ${planet} Nakshatra`);
  return constellationFromJson(json.Payload);
}

export async function getPurnimantaMonth(apiUrlDirect: string, time: BirthTimeJson): Promise<string> {
  const response = await fetch(`${apiUrlDirect}/Calculate/PurnimantaMonth${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Purnimanta month');
  return json.Payload as string;
}

export async function getRitu(apiUrlDirect: string, time: BirthTimeJson): Promise<string> {
  const response = await fetch(`${apiUrlDirect}/Calculate/Ritu${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Ritu');
  return json.Payload as string;
}

export async function getAyana(apiUrlDirect: string, time: BirthTimeJson): Promise<string> {
  const response = await fetch(`${apiUrlDirect}/Calculate/Ayana${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Ayana');
  return json.Payload as string;
}

export async function getSamvatsaraName(apiUrlDirect: string, time: BirthTimeJson): Promise<string> {
  const response = await fetch(`${apiUrlDirect}/Calculate/SamvatsaraName${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Samvatsara name');
  return json.Payload as string;
}

export async function getSamvatsaraNameNorth(apiUrlDirect: string, time: BirthTimeJson): Promise<string> {
  const response = await fetch(`${apiUrlDirect}/Calculate/SamvatsaraNameNorth${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Samvatsara name (North)');
  return json.Payload as string;
}

async function getSamvatYear(apiUrlDirect: string, calculatorName: 'GujaratiSamvatYear' | 'SakaSamvatYear' | 'KaliSamvatYear', time: BirthTimeJson): Promise<number> {
  const response = await fetch(`${apiUrlDirect}/Calculate/${calculatorName}${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(`Failed to calculate ${calculatorName}`);
  return json.Payload as number;
}

export const getGujaratiSamvatYear = (apiUrlDirect: string, time: BirthTimeJson) => getSamvatYear(apiUrlDirect, 'GujaratiSamvatYear', time);
export const getSakaSamvatYear = (apiUrlDirect: string, time: BirthTimeJson) => getSamvatYear(apiUrlDirect, 'SakaSamvatYear', time);
export const getKaliSamvatYear = (apiUrlDirect: string, time: BirthTimeJson) => getSamvatYear(apiUrlDirect, 'KaliSamvatYear', time);

async function getMoonOrSunRiseSet(apiUrlDirect: string, calculatorName: 'MoonriseTime' | 'MoonsetTime', time: BirthTimeJson): Promise<BirthTimeJson> {
  const response = await fetch(`${apiUrlDirect}/Calculate/${calculatorName}${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(`Failed to calculate ${calculatorName}`);
  return json.Payload as BirthTimeJson;
}

export const getMoonriseTime = (apiUrlDirect: string, time: BirthTimeJson) => getMoonOrSunRiseSet(apiUrlDirect, 'MoonriseTime', time);
export const getMoonsetTime = (apiUrlDirect: string, time: BirthTimeJson) => getMoonOrSunRiseSet(apiUrlDirect, 'MoonsetTime', time);

async function getRecurringVratCalendar(
  apiUrlDirect: string,
  calculatorName: 'PurnimaVratCalendar' | 'AmavasyaVratCalendar' | 'SankashtiChaturthiCalendar' | 'PradoshVratCalendar',
  year: number,
  location: GeoLocation
): Promise<BirthTimeJson[]> {
  const url = `${apiUrlDirect}/Calculate/${calculatorName}/Year/${year}${geoLocationToUrl(location)}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(`Failed to calculate ${calculatorName}`);
  return json.Payload as BirthTimeJson[];
}

export const getPurnimaVratCalendar = (apiUrlDirect: string, year: number, location: GeoLocation) => getRecurringVratCalendar(apiUrlDirect, 'PurnimaVratCalendar', year, location);
export const getAmavasyaVratCalendar = (apiUrlDirect: string, year: number, location: GeoLocation) => getRecurringVratCalendar(apiUrlDirect, 'AmavasyaVratCalendar', year, location);
export const getSankashtiChaturthiCalendar = (apiUrlDirect: string, year: number, location: GeoLocation) => getRecurringVratCalendar(apiUrlDirect, 'SankashtiChaturthiCalendar', year, location);
export const getPradoshVratCalendar = (apiUrlDirect: string, year: number, location: GeoLocation) => getRecurringVratCalendar(apiUrlDirect, 'PradoshVratCalendar', year, location);

export async function getPitruPakshaPeriod(apiUrlDirect: string, year: number, location: GeoLocation): Promise<TimeRange> {
  const response = await fetch(`${apiUrlDirect}/Calculate/PitruPakshaPeriod/Year/${year}${geoLocationToUrl(location)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Pitru Paksha period');
  const payload = json.Payload;
  return { start: payload.Start, end: payload.End };
}

export type JyeshthaGauriDates = { avahana: BirthTimeJson; poojan: BirthTimeJson; visarjan: BirthTimeJson };

export async function getJyeshthaGauriDates(apiUrlDirect: string, year: number, location: GeoLocation): Promise<JyeshthaGauriDates> {
  const yearLocation = `/Year/${year}${geoLocationToUrl(location)}`;
  const [avahanaR, poojanR, visarjanR] = await Promise.all([
    fetch(`${apiUrlDirect}/Calculate/JyeshthaGauriAvahana${yearLocation}`).then((r) => r.json()),
    fetch(`${apiUrlDirect}/Calculate/JyeshthaGauriPoojan${yearLocation}`).then((r) => r.json()),
    fetch(`${apiUrlDirect}/Calculate/JyeshthaGauriVisarjan${yearLocation}`).then((r) => r.json()),
  ]);
  if (avahanaR.Status !== 'Pass' || poojanR.Status !== 'Pass' || visarjanR.Status !== 'Pass') {
    throw new Error('Failed to calculate Jyeshtha Gauri dates');
  }
  return { avahana: avahanaR.Payload, poojan: poojanR.Payload, visarjan: visarjanR.Payload };
}

export async function getChaturmasPeriod(apiUrlDirect: string, year: number, location: GeoLocation): Promise<TimeRange> {
  const response = await fetch(`${apiUrlDirect}/Calculate/ChaturmasPeriod/Year/${year}${geoLocationToUrl(location)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Chaturmas period');
  const payload = json.Payload;
  return { start: payload.Start, end: payload.End };
}

export async function getSankrantiCalendar(apiUrlDirect: string, year: number, location: GeoLocation): Promise<Record<ZodiacName, BirthTimeJson>> {
  const response = await fetch(`${apiUrlDirect}/Calculate/SankrantiCalendar/Year/${year}${geoLocationToUrl(location)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Sankranti calendar');
  return json.Payload as Record<ZodiacName, BirthTimeJson>;
}
