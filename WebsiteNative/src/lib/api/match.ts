import { personFromJson, type Person } from './person';

/** Mirrors Library/Data/MatchPrediction.cs's ToJson(). */
export type MatchPrediction = {
  name: string;
  nature: string;
  maleInfo: string;
  femaleInfo: string;
  info: string;
  description: string;
};

/** Mirrors Library/Data/MatchReport.cs's ToJson(). */
export type MatchReport = {
  id: string;
  kutaScore: number;
  notes: string;
  male: Person;
  female: Person;
  predictionList: MatchPrediction[];
  summary: { heartIcon: string; scoreColor: string; scoreSummary: string };
};

function matchPredictionFromJson(json: any): MatchPrediction {
  return {
    name: json.Name ?? '',
    nature: json.Nature ?? '',
    maleInfo: json.MaleInfo ?? '',
    femaleInfo: json.FemaleInfo ?? '',
    info: json.Info ?? '',
    description: json.Description ?? '',
  };
}

function matchReportFromJson(json: any): MatchReport {
  return {
    id: json.Id,
    kutaScore: json.KutaScore,
    notes: json.Notes ?? '',
    male: personFromJson(json.Male),
    female: personFromJson(json.Female),
    predictionList: Array.isArray(json.PredictionList) ? json.PredictionList.map(matchPredictionFromJson) : [],
    summary: {
      heartIcon: json.Summary?.HeartIcon ?? '',
      scoreColor: json.Summary?.ScoreColor ?? '#888888',
      scoreSummary: json.Summary?.ScoreSummary ?? '',
    },
  };
}

/**
 * Live-computed report (API/FrontDesk/MatchAPI.cs's new /api/GetMatchReport endpoint, added
 * alongside FindMatch since the old Blazor site's "saved reports" pair — GetMatchReportList/
 * SaveMatchReport — was never ported to this API and has no Postgres persistence yet).
 */
export async function getMatchReport(apiUrlDirect: string, maleId: string, femaleId: string): Promise<MatchReport> {
  const response = await fetch(`${apiUrlDirect}/GetMatchReport/MaleId/${maleId}/FemaleId/${femaleId}`);
  const json = await response.json();
  if (json.Status !== 'Pass') {
    throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Failed to get match report');
  }
  return matchReportFromJson(json.Payload);
}

/**
 * Saves a couple's match report under an owner (real UserId or guest VisitorId), backed by
 * SavedMatchReportEntity/API/FrontDesk/MatchAPI.cs's new POST /api/SaveMatchReport - real
 * Postgres persistence, not a port (the old Blazor site's SaveMatchReport endpoint never
 * actually existed server-side). Re-saving the same couple updates Notes rather than duplicating.
 */
export async function saveMatchReport(
  apiUrlDirect: string,
  ownerId: string,
  maleId: string,
  femaleId: string,
  notes: string
): Promise<void> {
  const response = await fetch(`${apiUrlDirect}/SaveMatchReport`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ OwnerId: ownerId, MaleId: maleId, FemaleId: femaleId, Notes: notes }),
  });
  const json = await response.json();
  if (json.Status !== 'Pass') {
    throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Failed to save match report');
  }
}

/** Mirrors Library/Data/PersonKutaScore.cs's ToJsonList()/FromJsonList(). */
export type PersonKutaScore = {
  personId: string;
  personName: string;
  gender: 'Male' | 'Female';
  age: number;
  kutaScore: number;
};

function personKutaScoreFromJson(json: any): PersonKutaScore {
  return {
    personId: json.PersonId,
    personName: json.PersonName,
    gender: json.Gender,
    age: json.Age,
    kutaScore: json.KutaScore,
  };
}

/**
 * Global match search across the whole person database (GET /api/FindMatch/PersonId/{personId}),
 * backing Match/Finder.tsx — distinct from getMatchReport, which is a direct two-person report.
 */
export async function findMatchesForPerson(apiUrlDirect: string, personId: string): Promise<PersonKutaScore[]> {
  const response = await fetch(`${apiUrlDirect}/FindMatch/PersonId/${personId}`);
  const json = await response.json();
  if (json.Status !== 'Pass') return [];
  return (json.Payload as any[]).map(personKutaScoreFromJson);
}

/** Lists all match reports saved by an owner (GET /api/GetMatchReportList/OwnerId/{ownerId}). */
export async function getMatchReportList(apiUrlDirect: string, ownerId: string): Promise<MatchReport[]> {
  const response = await fetch(`${apiUrlDirect}/GetMatchReportList/OwnerId/${ownerId}`);
  const json = await response.json();
  if (json.Status !== 'Pass') {
    throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Failed to get saved match reports');
  }
  return (json.Payload as any[]).map(matchReportFromJson);
}
