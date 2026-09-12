import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { UnderConstructionNotice } from '@/components/UnderConstructionNotice';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Calculator/Match/Profile.razor — an "Under Construction" stub in the original too. */
export default function MatchProfileScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Match Profile</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          This profile is shown to your soulmate when they are found. Give what info you think is
          relevant and useful, maybe a way to contact you would be useful.
        </ThemedText>
        <UnderConstructionNotice />
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
    gap: Spacing.three,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
});
