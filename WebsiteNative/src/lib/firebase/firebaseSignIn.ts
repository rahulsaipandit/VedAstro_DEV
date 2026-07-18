import { FacebookAuthProvider, GoogleAuthProvider, signInWithCredential } from 'firebase/auth';
import { auth } from './client';

/**
 * Exchanges the raw provider token obtained via expo-auth-session (see
 * src/lib/auth/useGoogleSignIn.ts / useFacebookSignIn.ts) for a real Firebase user +
 * Firebase ID token, using signInWithCredential - this works cross-platform without
 * needing native Firebase modules or a dev-client build, since the OAuth dance itself
 * is already done by expo-auth-session; Firebase is just asked to mint a session from
 * the resulting token.
 */
export async function firebaseSignInWithGoogleIdToken(googleIdToken: string): Promise<string> {
  const credential = GoogleAuthProvider.credential(googleIdToken);
  const userCredential = await signInWithCredential(auth, credential);
  return userCredential.user.getIdToken();
}

export async function firebaseSignInWithFacebookAccessToken(facebookAccessToken: string): Promise<string> {
  const credential = FacebookAuthProvider.credential(facebookAccessToken);
  const userCredential = await signInWithCredential(auth, credential);
  return userCredential.user.getIdToken();
}
