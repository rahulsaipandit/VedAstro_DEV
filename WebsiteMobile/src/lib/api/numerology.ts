/** Backs Numerology.tsx. Both are plain string-input Calculate/* endpoints (see Library/Logic/Calculate/Numerology.cs). */

export async function getNameNumber(apiUrlDirect: string, name: string): Promise<number> {
  const response = await fetch(`${apiUrlDirect}/Calculate/NameNumber/InputText/${encodeURIComponent(name)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate name number');
  return json.Payload as number;
}

/** Mirrors NumerologyPrediction.cs's ToJson(). */
export async function getNameNumberPrediction(apiUrlDirect: string, name: string): Promise<string> {
  const response = await fetch(`${apiUrlDirect}/Calculate/NameNumberPrediction/FullName/${encodeURIComponent(name)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error('Failed to calculate name number prediction');
  return json.Payload.Prediction as string;
}
