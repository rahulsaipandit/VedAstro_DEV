/**
 * Mirrors Library/Data/Person.cs's ToJson()/FromJson() wire shape and
 * ViewComponents/Code/API/PersonTools.cs's GetPersonList/GetPublicPersonList calls
 * (see API/FrontDesk/PersonAPI.cs, reached via the Calculate/{calculatorName} reflection
 * dispatcher — confirmed by API/API.IntegrationTests/PersonEndpointsTests.cs).
 */
import type { BirthTimeJson } from '@/lib/time';

export type Person = {
  id: string;
  name: string;
  notes: string;
  gender: 'Male' | 'Female';
  ownerId: string;
  birthTime: BirthTimeJson;
};

export function personFromJson(json: any): Person {
  return {
    id: json.PersonId,
    name: json.Name,
    notes: json.Notes ?? '',
    gender: json.Gender,
    ownerId: json.OwnerId,
    birthTime: json.BirthTime,
  };
}

async function callGetPersonList(url: string): Promise<Person[]> {
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') return [];
  return (json.Payload as any[]).map(personFromJson);
}

/**
 * ownerId should be useAppStore's effectiveOwnerId() (real user id once signed in, else the
 * per-device visitorId) — visitorId is always sent too so the API's SwapUserId step can
 * migrate visitor-owned data onto a real account the first time it sees both together.
 */
export function getPersonList(apiUrlDirect: string, ownerId: string, visitorId: string): Promise<Person[]> {
  return callGetPersonList(`${apiUrlDirect}/Calculate/GetPersonList/OwnerId/${ownerId}/VisitorId/${visitorId}`);
}

/** Owner "101" is the shared example/demo person list shown alongside a user's own list. */
export function getPublicPersonList(apiUrlDirect: string): Promise<Person[]> {
  return callGetPersonList(`${apiUrlDirect}/Calculate/GetPersonList/OwnerId/101`);
}
