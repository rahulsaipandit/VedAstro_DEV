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
