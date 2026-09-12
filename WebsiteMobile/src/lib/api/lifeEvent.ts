import type { BirthTimeJson } from '@/lib/time';

/** Mirrors Library/Data/LifeEvent.cs's ToJson()/FromJson(). */
export type LifeEvent = {
  id: string;
  personId: string;
  name: string;
  startTime: BirthTimeJson;
  description: string;
  nature: 'Good' | 'Neutral' | 'Bad';
  weight: 'Major' | 'Normal' | 'Minor';
};

export function lifeEventFromJson(json: any): LifeEvent {
  return {
    id: json.Id,
    personId: json.PersonId,
    name: json.Name,
    startTime: json.StartTime,
    description: json.Description ?? '',
    nature: json.Nature ?? 'Neutral',
    weight: json.Weight ?? 'Normal',
  };
}

export function lifeEventToJson(event: LifeEvent) {
  return {
    PersonId: event.personId,
    Id: event.id,
    Name: event.name,
    StartTime: { StdTime: event.startTime.StdTime, Location: event.startTime.Location },
    Description: event.description,
    Nature: event.nature,
    Weight: event.weight,
  };
}
