/**
 * Backs FestivalCalendar.tsx. Calls Calculate.FestivalCalendar/FestivalDate
 * (Library/Logic/Calculate/FestivalCalendar.cs) for a year's festival dates, and
 * Calculate.ChoghadiyaPeriods/DailyPanchang (Library/Logic/Calculate/Panchang.cs) for the
 * day-detail panel's two selectable Panchang views.
 */
import { geoLocationToUrl, type GeoLocation } from '@/lib/api/geo';
import { timeToUrl, type BirthTimeJson } from '@/lib/time';

export const FESTIVAL_NAMES = [
  'Ramnavami',
  'Holi',
  'Rakshabandhan',
  'KarwaChauth',
  'Chhath',
  'Diwali',
  'MahaShivaratri',
  'MakarSankranti',
] as const;

export type FestivalName = (typeof FESTIVAL_NAMES)[number];

export type FestivalCalendarResult = Partial<Record<FestivalName, BirthTimeJson>>;

export async function getFestivalCalendar(apiUrlDirect: string, year: number, location: GeoLocation): Promise<FestivalCalendarResult> {
  const url = `${apiUrlDirect}/Calculate/FestivalCalendar/Year/${year}${geoLocationToUrl(location)}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate festival calendar');
  return json.Payload as FestivalCalendarResult;
}

export type ChoghadiyaName = 'Udveg' | 'Chal' | 'Labh' | 'Amrit' | 'Kaal' | 'Shubh' | 'Rog';
export type ChoghadiyaQuality = 'Auspicious' | 'Neutral' | 'Inauspicious';

export type ChoghadiyaPeriod = {
  name: ChoghadiyaName;
  quality: ChoghadiyaQuality;
  start: BirthTimeJson;
  end: BirthTimeJson;
  isDayPeriod: boolean;
};

export async function getChoghadiyaPeriods(apiUrlDirect: string, date: BirthTimeJson): Promise<ChoghadiyaPeriod[]> {
  const response = await fetch(`${apiUrlDirect}/Calculate/ChoghadiyaPeriods${timeToUrl(date)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Choghadiya periods');
  return (json.Payload as any[]).map((p) => ({
    name: p.Name,
    quality: p.Quality,
    start: p.Start,
    end: p.End,
    isDayPeriod: p.IsDayPeriod,
  }));
}

export type DailyPanchang = {
  tithiName: string;
  paksha: string;
  lunarMonth: string;
  vara: string;
  nakshatra: string;
  yoga: string;
  karana: string;
  sunrise: BirthTimeJson;
  sunset: BirthTimeJson;
};

export async function getDailyPanchang(apiUrlDirect: string, date: BirthTimeJson): Promise<DailyPanchang> {
  const response = await fetch(`${apiUrlDirect}/Calculate/DailyPanchang${timeToUrl(date)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate daily Panchang');
  const payload = json.Payload;
  return {
    tithiName: payload.Tithi?.Name,
    paksha: payload.Tithi?.Paksha,
    lunarMonth: payload.LunarMonth,
    vara: payload.Vara,
    nakshatra: payload.Nakshatra?.Name ?? payload.Nakshatra,
    yoga: payload.Yoga?.Name,
    karana: payload.Karana,
    sunrise: payload.Sunrise,
    sunset: payload.Sunset,
  };
}
