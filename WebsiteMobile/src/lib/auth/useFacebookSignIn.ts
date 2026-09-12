import { useEffect } from 'react';
import * as AuthSession from 'expo-auth-session';
import { FACEBOOK_APP_ID, FACEBOOK_DISCOVERY } from './config';

/**
 * Replaces the old Facebook JS SDK flow (JS.facebookLogin / OnFacebookSignInSuccessHandler
 * in SignInButton.razor). Uses the implicit "token" response type to get a raw access token,
 * matching what the old code sent to /api/SignInFacebook/Token/{token}.
 */
export function useFacebookSignIn(onAccessToken: (accessToken: string) => void) {
  const redirectUri = AuthSession.makeRedirectUri();
  const [request, response, promptAsync] = AuthSession.useAuthRequest(
    {
      clientId: FACEBOOK_APP_ID,
      responseType: AuthSession.ResponseType.Token,
      scopes: ['public_profile', 'email'],
      redirectUri,
    },
    FACEBOOK_DISCOVERY
  );

  useEffect(() => {
    if (response?.type === 'success' && response.params.access_token) {
      onAccessToken(response.params.access_token);
    }
  }, [response, onAccessToken]);

  return { request, promptAsync };
}
