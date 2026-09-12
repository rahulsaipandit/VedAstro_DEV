import { useMemo } from 'react';
import { Pressable, ScrollView, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { InfoBox } from '@/components/InfoBox';
import { SignInButton } from '@/components/SignInButton';
import { useAppStore } from '@/store/useAppStore';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const FIRST_LOGIN_GREETINGS = [
  "To prove you're a human from Earth",
  "To prove you're not a robot",
  "To prove you're not from the Machine World",
  'To authenticate yourself as a biological entity',
];

/** Ported from Website/Pages/Account/Login.razor. */
export default function LoginScreen() {
  const router = useRouter();
  const previousLoginMethod = useAppStore((s) => s.previousLoginMethod);

  const memoryHelperText = useMemo(() => {
    if (!previousLoginMethod) {
      return FIRST_LOGIN_GREETINGS[Math.floor(Math.random() * FIRST_LOGIN_GREETINGS.length)];
    }
    return `On your last visit, you used "${previousLoginMethod}"`;
  }, [previousLoginMethod]);

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedView style={styles.header}>
          <ThemedText type="title">Login</ThemedText>
          <ThemedText themeColor="textSecondary">{memoryHelperText}</ThemedText>
        </ThemedView>

        <SignInButton onSignInSuccess={() => (router.canGoBack() ? router.back() : router.replace('/'))} />

        {/* Guest mode: sign-in is never mandatory - every feature works without an account,
            scoped to a per-device visitor ID instead (see useAppStore's effectiveOwnerId). */}
        <Pressable onPress={() => (router.canGoBack() ? router.back() : router.replace('/'))}>
          <ThemedText type="link" themeColor="textSecondary" style={styles.guestLink}>
            Continue as Guest
          </ThemedText>
        </Pressable>

        <ThemedText type="subtitle" style={styles.whyLoginTitle}>
          Why login?
        </ThemedText>
        <ThemedView style={styles.infoRow}>
          <InfoBox title="Secure" description="Safe & fast with Google or Facebook authentication." />
          <InfoBox title="Storage" description="Free cloud storage for charts, reports and horoscopes." />
          <InfoBox title="Privacy" description="We don't collect data or connect to your Google or Facebook account." />
        </ThemedView>
      </ThemedView>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scrollContent: {
    alignItems: 'center',
  },
  page: {
    width: '100%',
    maxWidth: MaxContentWidth,
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    paddingBottom: Spacing.six,
    gap: Spacing.five,
  },
  header: {
    alignItems: 'center',
    gap: Spacing.one,
  },
  guestLink: {
    textAlign: 'center',
    textDecorationLine: 'underline',
  },
  whyLoginTitle: {
    textAlign: 'center',
  },
  infoRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
  },
});
