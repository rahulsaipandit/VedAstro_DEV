/**
 * Client IDs reused from the existing Blazor app so sign-in keeps hitting the
 * same Google/Facebook app registrations (see ViewComponents/Components/SignInButton.razor
 * and Website/wwwroot/index.html's FB.init call). These are public client IDs, not secrets.
 *
 * IMPORTANT (not done here — a deployment-time step): Google Cloud Console's
 * "Authorized redirect URIs" and the Facebook App's "Valid OAuth Redirect URIs"
 * only allow the web/native origins you register. The old web client ID is
 * registered for vedastro.org's origin; a new Expo dev/native origin
 * (AuthSession.makeRedirectUri() output) must be added there before sign-in
 * will actually complete end-to-end from this app.
 */
export const GOOGLE_CLIENT_ID_WEB = '19638836771-oflt5g9mnkft6chkl04vp4m5qpu5h569.apps.googleusercontent.com';
export const FACEBOOK_APP_ID = '2092940567762345';

export const GOOGLE_DISCOVERY = {
  authorizationEndpoint: 'https://accounts.google.com/o/oauth2/v2/auth',
  tokenEndpoint: 'https://oauth2.googleapis.com/token',
  revocationEndpoint: 'https://oauth2.googleapis.com/revoke',
};

export const FACEBOOK_DISCOVERY = {
  authorizationEndpoint: 'https://www.facebook.com/v19.0/dialog/oauth',
  tokenEndpoint: 'https://graph.facebook.com/v19.0/oauth/access_token',
};
