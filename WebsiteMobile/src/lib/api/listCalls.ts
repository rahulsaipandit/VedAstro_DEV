/**
 * Backs APIBuilder.tsx and TableGenerator.tsx — both are dynamic UIs driven by the same
 * reflection metadata endpoint, GET /api/ListAllCalls (API/FrontDesk/OpenAPI.cs), which lists
 * every Calculate/{name} method reachable through the generic dispatcher along with its
 * parameters. Confirmed live: DefaultValue is essentially never populated (only one parameter
 * across ~370 methods has a non-empty DefaultValue in a real response) so there's no reliable way
 * to tell "optional" from "compulsory" params from this metadata — every parameter is therefore
 * treated as required to fill in, which is always valid anyway (an explicit value overrides
 * whatever the server-side default would have been).
 */
export type CallParameter = {
  name: string;
  description: string;
  parameterType: string; // full .NET type name, e.g. "VedAstro.Library.PlanetName"
};

export type CallMetadata = {
  name: string;
  signature: string;
  description: string;
  lineNumber: string;
  parameters: CallParameter[];
};

function callMetadataFromJson(json: any): CallMetadata {
  const methodInfo = json.MethodInfo ?? {};
  return {
    name: methodInfo.Name ?? '',
    signature: json.Signature ?? '',
    description: (json.Description ?? '').trim(),
    lineNumber: json.LineNumber ?? '',
    parameters: Array.isArray(methodInfo.Parameters)
      ? methodInfo.Parameters.map((p: any) => ({
          name: p.Name,
          description: p.Description ?? '',
          parameterType: p.ParameterType ?? 'System.String',
        }))
      : [],
  };
}

let cachedCalls: CallMetadata[] | null = null;
let cachedForApiUrl: string | null = null;

/** GET /api/ListAllCalls — cached per apiUrlDirect since the list rarely changes within a session. */
export async function getAllCalls(apiUrlDirect: string): Promise<CallMetadata[]> {
  if (cachedCalls && cachedForApiUrl === apiUrlDirect) return cachedCalls;
  const response = await fetch(`${apiUrlDirect}/ListAllCalls`);
  const json = await response.json();
  if (json.Status !== 'Pass') return [];
  const calls = (json.Payload as any[]).map(callMetadataFromJson).filter((c) => c.name);
  cachedCalls = calls;
  cachedForApiUrl = apiUrlDirect;
  return calls;
}
