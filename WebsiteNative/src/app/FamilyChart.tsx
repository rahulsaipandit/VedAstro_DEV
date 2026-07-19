import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { UnderConstructionNotice } from '@/components/UnderConstructionNotice';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Port of Website/Pages/Calculator/FamilyChart.razor — that page is itself nothing but an
 * "Under Construction" stub in the original Blazor site (no real feature was ever built there),
 * so this is a faithful 1:1 port, not a placeholder standing in for unported work.
 */
export default function FamilyChartScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Family Chart</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Analysis of family charts or the charts of people around you can lead to more accurate
          results.
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
