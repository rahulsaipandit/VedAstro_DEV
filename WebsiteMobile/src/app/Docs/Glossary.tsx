import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { UnderConstructionNotice } from '@/components/UnderConstructionNotice';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Docs/Glossary.razor — an "Under Construction" stub in the original too. */
export default function GlossaryScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Glossary & Reference</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Technical Vedic terms explained in simple English, for easier understanding across
          cultures.
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
