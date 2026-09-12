/**
 * Backs VedicBirthday.tsx. Calls Calculate.VedicBirthDate (Library/Logic/Calculate/Core.cs) to
 * find the Gregorian date, in a given calendar year, that a person's Vedic-calendar "birthday"
 * (the recurrence of their birth tithi) falls on - the same way a Hindu festival like Ramnavami
 * lands on a different Gregorian date every year. The matched date's own tithi/paksha is then
 * fetched via the existing Calculate.LunarDay endpoint, keeping with this API's granular,
 * one-fact-per-endpoint composition style (see Horoscope/[personId].tsx's Promise.allSettled).
 */
import { timeToUrl, type BirthTimeJson } from '@/lib/time';

export type LunarDayInfo = {
  name: string;
  paksha: string;
  date: string;
  day: string;
  phase: string;
};

export async function getVedicBirthDate(apiUrlDirect: string, birthTime: BirthTimeJson, year: number): Promise<BirthTimeJson> {
  const url = `${apiUrlDirect}/Calculate/VedicBirthDate${timeToUrl(birthTime)}/Year/${year}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate Vedic birth date');
  return json.Payload as BirthTimeJson;
}

export async function getLunarDay(apiUrlDirect: string, time: BirthTimeJson): Promise<LunarDayInfo> {
  const response = await fetch(`${apiUrlDirect}/Calculate/LunarDay${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate lunar day');
  const payload = json.Payload;
  return { name: payload.Name, paksha: payload.Paksha, date: payload.Date, day: payload.Day, phase: payload.Phase };
}
