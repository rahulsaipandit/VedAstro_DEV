/**
 * Mirrors Library/Logic/URL.cs. ApiUrlDirect switches on debugMode (see
 * src/store/useAppStore.ts) exactly like the old URL(isBetaRuntime, debugMode)
 * constructor did — local API for dev, deployed API otherwise.
 */
export const WebStable = 'https://vedastro.org';
export const ApiStable = 'https://api.vedastro.org/api';
export const ApiBeta = 'https://beta.api.vedastro.org/api';
export const ApiLocalDebug = 'http://localhost:7071/api';

export const GitHubRepo = 'https://github.com/VedAstro/VedAstro';
export const SlackInviteURL = 'https://join.slack.com/t/vedastro/shared_invite';

export function getApiUrlDirect(debugMode: boolean, isBetaRuntime = false): string {
  if (debugMode) return ApiLocalDebug;
  return isBetaRuntime ? ApiBeta : ApiStable;
}

/** Endpoint builders — each is "{apiUrlDirect}/{EndpointName}", mirroring URL.cs's instance properties. */
export function apiEndpoints(apiUrlDirect: string) {
  return {
    getPersonList: `${apiUrlDirect}/Calculate/GetPersonList`,
    addPerson: `${apiUrlDirect}/Calculate/AddPerson`,
    deletePerson: `${apiUrlDirect}/Calculate/DeletePerson`,
    updatePerson: `${apiUrlDirect}/Calculate/UpdatePerson`,
    getPerson: `${apiUrlDirect}/Calculate/GetPerson`,
    getNewPersonId: `${apiUrlDirect}/GetNewPersonId`,

    findMatch: `${apiUrlDirect}/FindMatch`,
    getMatchReportList: `${apiUrlDirect}/GetMatchReportList`,
    getMatchReport: `${apiUrlDirect}/GetMatchReport`,
    saveMatchReport: `${apiUrlDirect}/SaveMatchReport`,

    getEventsChart: `${apiUrlDirect}/GetEventsChart`,
    getEvents: `${apiUrlDirect}/GetEvents`,

    horoscopePredictions: `${apiUrlDirect}/Calculate/HoroscopePredictions`,

    login: `${apiUrlDirect}/Login`,
  } as const;
}

/** SignInAPI.cs routes take the token as a path segment, not a fixed endpoint. */
export function signInGoogleUrl(apiUrlDirect: string, idToken: string): string {
  return `${apiUrlDirect}/SignInGoogle/Token/${encodeURIComponent(idToken)}`;
}

export function signInFacebookUrl(apiUrlDirect: string, accessToken: string): string {
  return `${apiUrlDirect}/SignInFacebook/Token/${encodeURIComponent(accessToken)}`;
}

/** WebsiteNative's actual sign-in path — verifies a Firebase ID token (see API/FrontDesk/SignInAPI.cs). */
export function signInFirebaseUrl(apiUrlDirect: string, firebaseIdToken: string): string {
  return `${apiUrlDirect}/SignInFirebase/Token/${encodeURIComponent(firebaseIdToken)}`;
}
