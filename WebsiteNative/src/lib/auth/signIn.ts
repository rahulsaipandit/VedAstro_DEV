import { signInFacebookUrl, signInFirebaseUrl, signInGoogleUrl } from '@/constants/urls';

/** Mirrors the {Status: "Pass"|"Fail", Payload} envelope written by APITools.PassMessageJson/FailMessageJson. */
export type SignInResult =
  | { pass: true; id: string; name: string }
  | { pass: false; message: string };

async function callSignInEndpoint(url: string): Promise<SignInResult> {
  const response = await fetch(url);
  const json = await response.json();

  if (json.Status === 'Pass') {
    return { pass: true, id: json.Payload.Id, name: json.Payload.Name };
  }
  return { pass: false, message: typeof json.Payload === 'string' ? json.Payload : 'Login failed' };
}

/** WebsiteNative's actual sign-in path — verifies a Firebase ID token, see /api/SignInFirebase. */
export function signInFirebase(apiUrlDirect: string, firebaseIdToken: string): Promise<SignInResult> {
  return callSignInEndpoint(signInFirebaseUrl(apiUrlDirect, firebaseIdToken));
}

// Kept for reference/parity with the old Blazor flow (ViewComponents/Components/SignInButton.razor),
// which still calls these directly — not used by WebsiteNative's own SignInButton (see firebaseSignIn.ts).
export function signInGoogle(apiUrlDirect: string, idToken: string): Promise<SignInResult> {
  return callSignInEndpoint(signInGoogleUrl(apiUrlDirect, idToken));
}

export function signInFacebook(apiUrlDirect: string, accessToken: string): Promise<SignInResult> {
  return callSignInEndpoint(signInFacebookUrl(apiUrlDirect, accessToken));
}
