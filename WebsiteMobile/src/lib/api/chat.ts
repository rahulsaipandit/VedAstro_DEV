import { timeToUrl, type BirthTimeJson } from '@/lib/time';

/**
 * Backs the ChatAPI screen. Confirmed real, synchronous (not job/polling like EventsChart)
 * endpoints reached via the generic Calculate/{name} reflection dispatcher — see
 * Library/Logic/Calculate/CoreRelationships.cs's HoroscopeChat/HoroscopeFollowUpChat/
 * HoroscopeChatFeedback wrappers around Library/Logic/Calculate/ChatAPI.cs's
 * SendMessageHoroscope/SendMessageHoroscopeFollowUp/HoroscopeChatFeedback, and
 * API/API.IntegrationTests/ChatEndpointsTests.cs for real example URLs/timings (a real local-LLM
 * generation can take minutes — this fetch has no client-side timeout, matching that test's own
 * 12-minute allowance).
 */
export type ChatReply = {
  sessionId: string;
  text: string;
  textHtml: string;
  textHash: string;
  followUpQuestions: string[];
  commands: string[];
};

function chatReplyFromJson(json: any): ChatReply {
  return {
    sessionId: json.SessionId ?? '',
    text: json.Text ?? '',
    textHtml: json.TextHtml ?? '',
    textHash: json.TextHash ?? '',
    followUpQuestions: Array.isArray(json.FollowUpQuestions) ? json.FollowUpQuestions : [],
    commands: Array.isArray(json.Commands) ? json.Commands : [],
  };
}

/** Calculate/HoroscopeChat{timeUrl}/UserQuestion/{q}/UserId/{userId}[/SessionId/{sessionId}] */
export async function askHoroscopeChat(
  apiUrlDirect: string,
  birthTime: BirthTimeJson,
  userQuestion: string,
  userId: string,
  sessionId?: string
): Promise<ChatReply> {
  let url = `${apiUrlDirect}/Calculate/HoroscopeChat${timeToUrl(birthTime)}/UserQuestion/${encodeURIComponent(userQuestion)}/UserId/${encodeURIComponent(userId)}`;
  if (sessionId) url += `/SessionId/${encodeURIComponent(sessionId)}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Chat request failed');
  return chatReplyFromJson(json.Payload);
}

/** Calculate/HoroscopeFollowUpChat{timeUrl}/FollowUpQuestion/{q}/PrimaryAnswerHash/{hash}/UserId/{userId}/SessionId/{sessionId} */
export async function askHoroscopeFollowUpChat(
  apiUrlDirect: string,
  birthTime: BirthTimeJson,
  followUpQuestion: string,
  primaryAnswerHash: string,
  userId: string,
  sessionId: string
): Promise<ChatReply> {
  const url =
    `${apiUrlDirect}/Calculate/HoroscopeFollowUpChat${timeToUrl(birthTime)}` +
    `/FollowUpQuestion/${encodeURIComponent(followUpQuestion)}` +
    `/PrimaryAnswerHash/${encodeURIComponent(primaryAnswerHash)}` +
    `/UserId/${encodeURIComponent(userId)}` +
    `/SessionId/${encodeURIComponent(sessionId)}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Chat request failed');
  return chatReplyFromJson(json.Payload);
}

/** Calculate/HoroscopeChatFeedback/AnswerHash/{hash}/FeedbackScore/{score} — score: -1 (bad) or 1 (good), matching thumbs down/up. */
export async function sendChatFeedback(apiUrlDirect: string, answerHash: string, feedbackScore: number): Promise<void> {
  const url = `${apiUrlDirect}/Calculate/HoroscopeChatFeedback/AnswerHash/${encodeURIComponent(answerHash)}/FeedbackScore/${feedbackScore}`;
  const response = await fetch(url);
  const json = await response.json();
  if (json.Status !== 'Pass') throw new Error(typeof json.Payload === 'string' ? json.Payload : 'Failed to send feedback');
}
