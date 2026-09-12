import { timeToUrl, type BirthTimeJson } from '@/lib/time';

/** Backs SunRiseSetTime.tsx. Calculate.SunriseTime(Time)/SunsetTime(Time) both return a Time — only StdTime is needed for display. */
async function callTimeCalculator(apiUrlDirect: string, endpoint: string, time: BirthTimeJson): Promise<string> {
  const response = await fetch(`${apiUrlDirect}/Calculate/${endpoint}${timeToUrl(time)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(`Failed to get ${endpoint}`);
  const payload = json.Payload?.[endpoint] ?? json.Payload;
  return payload.StdTime as string;
}

export const getSunriseTime = (apiUrlDirect: string, time: BirthTimeJson) =>
  callTimeCalculator(apiUrlDirect, 'SunriseTime', time);

export const getSunsetTime = (apiUrlDirect: string, time: BirthTimeJson) =>
  callTimeCalculator(apiUrlDirect, 'SunsetTime', time);
