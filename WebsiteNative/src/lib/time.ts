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
