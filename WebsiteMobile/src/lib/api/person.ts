/**
 * Mirrors Library/Data/Person.cs's ToJson()/FromJson() wire shape and
 * ViewComponents/Code/API/PersonTools.cs's GetPersonList/GetPublicPersonList calls
 * (see API/FrontDesk/PersonAPI.cs, reached via the Calculate/{calculatorName} reflection
 * dispatcher — confirmed by API/API.IntegrationTests/PersonEndpointsTests.cs).
 */
import { timeToUrl, type BirthTimeJson } from '@/lib/time';
import { lifeEventFromJson, lifeEventToJson, type LifeEvent } from '@/lib/api/lifeEvent';

export type Person = {
  id: string;
  name: string;
  notes: string;
  gender: 'Male' | 'Female';
  ownerId: string;
  birthTime: BirthTimeJson;
  lifeEventList: LifeEvent[];
};

export function personFromJson(json: any): Person {
  return {
    id: json.PersonId,
    name: json.Name,
    notes: json.Notes ?? '',
    gender: json.Gender,
    ownerId: json.OwnerId,
    birthTime: json.BirthTime,
    lifeEventList: Array.isArray(json.LifeEventList) ? json.LifeEventList.map(lifeEventFromJson) : [],
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

/**
 * Calculate/AddPerson/OwnerId/{ownerId}{timeUrl}/PersonName/{name}/Gender/{gender}/Notes/{notes}
 * (see API/FrontDesk/PersonAPI.cs's AddPerson doc-comment for the exact URL shape). Returns the
 * new person's server-generated ID.
 */
export async function addPerson(
  apiUrlDirect: string,
  ownerId: string,
  birthTime: BirthTimeJson,
  name: string,
  gender: 'Male' | 'Female',
  notes = ''
): Promise<string> {
  const timeUrl = timeToUrl(birthTime);
  const url = `${apiUrlDirect}/Calculate/AddPerson/OwnerId/${encodeURIComponent(ownerId)}${timeUrl}/PersonName/${encodeURIComponent(name)}/Gender/${gender}/Notes/${encodeURIComponent(notes)}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Failed to add person');
  return json.Payload as string;
}

/** POST /api/UpdatePerson — JSON body is Person.ToJson()'s exact shape (see Library/Data/Person.cs). */
export async function updatePerson(apiUrlDirect: string, person: Person): Promise<void> {
  const body = {
    PersonId: person.id,
    Name: person.name,
    Notes: person.notes,
    BirthTime: { StdTime: person.birthTime.StdTime, Location: person.birthTime.Location },
    Gender: person.gender,
    OwnerId: person.ownerId,
    LifeEventList: person.lifeEventList.map(lifeEventToJson),
  };
  const response = await fetch(`${apiUrlDirect}/UpdatePerson`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Failed to update person');
}

/** GET /api/DeletePerson/OwnerId/{ownerId}/PersonId/{personId}. */
export async function deletePerson(apiUrlDirect: string, ownerId: string, personId: string): Promise<void> {
  const response = await fetch(`${apiUrlDirect}/DeletePerson/OwnerId/${encodeURIComponent(ownerId)}/PersonId/${encodeURIComponent(personId)}`);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Failed to delete person');
}
