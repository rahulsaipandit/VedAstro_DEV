import { Pressable, StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { useAppStore } from '@/store/useAppStore';
import { useGoogleSignIn } from '@/lib/auth/useGoogleSignIn';
import { useFacebookSignIn } from '@/lib/auth/useFacebookSignIn';
import { signInFirebase, type SignInResult } from '@/lib/auth/signIn';
import { firebaseSignInWithFacebookAccessToken, firebaseSignInWithGoogleIdToken } from '@/lib/firebase/firebaseSignIn';
import { showErrorToast, showSuccessToast } from '@/lib/toast';

/**
 * Port of ViewComponents/Components/SignInButton.razor. The old Google/Facebook JS SDK
 * buttons become expo-auth-session-driven Pressables that hand their raw provider token
 * to Firebase Auth (signInWithCredential), then verify the resulting Firebase ID token
 * against the API (/api/SignInFirebase) — see src/lib/firebase and src/lib/auth/config.ts
 * for the redirect-URI caveat that needs a one-time Google/Facebook console update.
 */
export function SignInButton({ onSignInSuccess }: { onSignInSuccess?: () => void }) {
  const theme = useTheme();
  const currentUser = useAppStore((s) => s.currentUser);
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const setCurrentUser = useAppStore((s) => s.setCurrentUser);
  const setPreviousLoginMethod = useAppStore((s) => s.setPreviousLoginMethod);

  async function handleGoogleIdToken(googleIdToken: string) {
    try {
      const firebaseIdToken = await firebaseSignInWithGoogleIdToken(googleIdToken);
      await afterAuthResult('Google', await signInFirebase(apiUrlDirect, firebaseIdToken));
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Google sign-in failed');
    }
  }

  async function handleFacebookAccessToken(facebookAccessToken: string) {
    try {
      const firebaseIdToken = await firebaseSignInWithFacebookAccessToken(facebookAccessToken);
      await afterAuthResult('Facebook', await signInFirebase(apiUrlDirect, firebaseIdToken));
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Facebook sign-in failed');
    }
  }

  async function afterAuthResult(provider: string, result: SignInResult) {
    if (result.pass) {
      setPreviousLoginMethod(provider);
      setCurrentUser({ id: result.id, name: result.name, isGuest: false });
      showSuccessToast(`Signed in as ${result.name}`);
      onSignInSuccess?.();
    } else {
      showErrorToast(result.message);
    }
  }

  const google = useGoogleSignIn(handleGoogleIdToken);
  const facebook = useFacebookSignIn(handleFacebookAccessToken);

  if (!currentUser.isGuest) {
    return (
      <ThemedView style={[styles.box, { backgroundColor: theme.backgroundElement }]}>
        <ThemedText type="smallBold">Hi, {currentUser.name}</ThemedText>
      </ThemedView>
    );
  }

  return (
    <ThemedView style={[styles.box, { backgroundColor: theme.backgroundElement }]}>
      <Pressable
        disabled={!facebook.request}
        onPress={() => facebook.promptAsync()}
        style={[styles.button, { backgroundColor: theme.backgroundSelected }]}>
        <ThemedText type="smallBold">Sign in with Facebook</ThemedText>
      </Pressable>

      <Pressable
        disabled={!google.request}
        onPress={() => google.promptAsync()}
        style={[styles.button, { backgroundColor: theme.backgroundSelected }]}>
        <ThemedText type="smallBold">Sign in with Google</ThemedText>
      </Pressable>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  box: {
    borderRadius: 16,
    padding: Spacing.four,
    gap: Spacing.three,
    alignItems: 'center',
  },
  button: {
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.three,
    borderRadius: 30,
    width: 220,
    alignItems: 'center',
  },
});
