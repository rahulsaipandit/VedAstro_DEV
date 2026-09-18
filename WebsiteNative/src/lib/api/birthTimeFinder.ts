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
  /** defaults to "Raman" on the server - see AYANAMSA_GROUPS */
  ayanamsaName?: string;
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
  if (options?.ayanamsaName) params.set('ayanamsaName', options.ayanamsaName);

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

/**
 * Backs BirthTimeQuestionnaire.tsx. Mirrors API/FrontDesk/BirthTimeScoringAPI.cs's
 * GET /api/FindBirthTime/Questions and POST /api/FindBirthTime/Score/PersonId/{personId} -
 * the questionnaire-driven alternative to the SVG-stack flow above (see
 * docs/BirthTimeFinder.md for the design).
 */

export type BtrRuleTier = 'Classical' | 'Heuristic';

export type BtrQuestion = {
  id: number;
  text: string;
  category: string;
  tier: BtrRuleTier;
};

export type BtrAnswer = 'StronglyNo' | 'No' | 'Skip' | 'Yes' | 'StronglyYes';

export type BtrConfidenceBucket = 'Low' | 'Medium' | 'High';

export type BtrCandidateScore = {
  time: string;
  rawScore: number;
  confidencePercent: number;
  bucket: BtrConfidenceBucket;
};

/** Fetches the full BTR questionnaire (currently 95 questions) - single source of truth lives server-side in BtrQuestionBank. */
export async function getBtrQuestions(apiUrlDirect: string): Promise<BtrQuestion[]> {
  const response = await fetch(`${apiUrlDirect}/FindBirthTime/Questions`);
  const json = await response.json();

  if (!response.ok || json.Status !== 'Pass') {
    throw new Error(json?.Payload ?? 'Failed to load BTR questionnaire');
  }

  return (json.Payload as any[]).map((q) => ({
    id: q.Id,
    text: q.Text,
    category: q.Category,
    tier: q.Tier,
  }));
}

/** Scores every candidate time in the sweep against the given answers, ranked best-match first. */
export async function scoreBirthTimeCandidates(
  apiUrlDirect: string,
  personId: string,
  answers: { questionId: number; answer: BtrAnswer }[],
  options?: BirthTimeFinderOptions
): Promise<BtrCandidateScore[]> {
  const response = await fetch(`${apiUrlDirect}/FindBirthTime/Score/PersonId/${personId}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      answers,
      precisionInHours: options?.precisionInHours,
      startDate: options?.startDate,
      endDate: options?.endDate,
      startHour: options?.startHour,
      endHour: options?.endHour,
      ayanamsaName: options?.ayanamsaName,
    }),
  });
  const json = await response.json();

  if (!response.ok || json.Status !== 'Pass') {
    throw new Error(json?.Payload ?? 'Failed to score birth time candidates');
  }

  return (json.Payload as any[]).map((c) => ({
    time: c.Time,
    rawScore: c.RawScore,
    confidencePercent: c.ConfidencePercent,
    bucket: c.Bucket,
  }));
}
