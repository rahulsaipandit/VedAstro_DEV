import { useEffect } from 'react';
import * as AuthSession from 'expo-auth-session';
import { GOOGLE_CLIENT_ID_WEB, GOOGLE_DISCOVERY } from './config';

/**
 * Replaces the old Google Identity Services JS SDK flow (JS.OnGoogleSignInSuccessHandler
 * in SignInButton.razor) with expo-auth-session's generic AuthRequest primitives — the
 * built-in expo-auth-session/providers/google helper is deprecated, so this talks to
 * Google's OAuth endpoints directly, same as the old JS SDK effectively did.
 * Returns the same shape of thing the old code had: a raw ID token (JWT) to send to
 * /api/SignInGoogle/Token/{token}.
 */
export function useGoogleSignIn(onIdToken: (idToken: string) => void) {
  const redirectUri = AuthSession.makeRedirectUri();
  const [request, response, promptAsync] = AuthSession.useAuthRequest(
    {
      clientId: GOOGLE_CLIENT_ID_WEB,
      responseType: AuthSession.ResponseType.IdToken,
      scopes: ['openid', 'profile', 'email'],
      redirectUri,
      extraParams: {
        nonce: Math.random().toString(36).slice(2),
      },
    },
    GOOGLE_DISCOVERY
  );

  useEffect(() => {
    if (response?.type === 'success' && response.params.id_token) {
      onIdToken(response.params.id_token);
    }
  }, [response, onIdToken]);

  return { request, promptAsync };
}
