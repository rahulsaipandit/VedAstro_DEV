/**
 * Backs BirthTimeFinder.tsx. Mirrors API/FrontDesk/BirthTimeFinderAPI.cs's
 * GET /api/FindBirthTime/EventsChart/PersonId/{personId} endpoint (the RN port of the
 * Console app's "Find Birth Time - Life Predictor - Person" dictionary-attack tool, see
 * Console/Program.cs's FindBirthTimeEventsChartPerson). Unlike /api/EventsChart, this endpoint
 * is synchronous - no job-poll `Call-Status` header, it just returns the combined SVG directly.
 */

export type BirthTimeFinderOptions = {
  maxWidth?: number;
  precisionInHours?: number;
  /** dd/MM/yyyy, defaults to the person's birth date on the server */
  startDate?: string;
  /** dd/MM/yyyy, defaults to birth date + 100 years on the server */
  endDate?: string;
  /** HH:mm, defaults to 00:00 on the server */
  startHour?: string;
  /** HH:mm, defaults to 23:59 on the server */
  endHour?: string;
};

/** Fetches the combined "possible birth times" SVG for a person from the server. */
export async function getBirthTimeFinderSvg(
  apiUrlDirect: string,
  personId: string,
  options?: BirthTimeFinderOptions
): Promise<string> {
  const params = new URLSearchParams();
  if (options?.maxWidth !== undefined) params.set('maxWidth', String(options.maxWidth));
  if (options?.precisionInHours !== undefined) params.set('precisionInHours', String(options.precisionInHours));
  if (options?.startDate) params.set('startDate', options.startDate);
  if (options?.endDate) params.set('endDate', options.endDate);
  if (options?.startHour) params.set('startHour', options.startHour);
  if (options?.endHour) params.set('endHour', options.endHour);

  const query = params.toString();
  const url = `${apiUrlDirect}/FindBirthTime/EventsChart/PersonId/${personId}${query ? `?${query}` : ''}`;

  const response = await fetch(url);
  const contentType = response.headers.get('Content-Type') ?? '';
  const text = await response.text();

  if (!response.ok || !contentType.includes('svg')) {
    // server errors come back as a `{Status:"Fail", Payload:"<message>"}` JSON envelope
    let message = 'Failed to generate birth time chart';
    try {
      const json = JSON.parse(text);
      if (json.Payload) message = json.Payload;
    } catch {
      // not JSON, keep the generic message
    }
    throw new Error(message);
  }

  return stripScripts(text);
}

function stripScripts(svg: string): string {
  return svg.replace(/<script[\s\S]*?(?:\/>|<\/script>)/gi, '');
}
