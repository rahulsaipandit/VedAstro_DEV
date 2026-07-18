import { useEffect, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet } from 'react-native';
import { useLocalSearchParams } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { useAppStore } from '@/store/useAppStore';
import { getMatchReport, type MatchReport } from '@/lib/api/match';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Ported (as a functional MVP, not a full port) from Website/Pages/Calculator/Match/Report.razor.
 * Calls the new live-computed /api/GetMatchReport endpoint (see API/FrontDesk/MatchAPI.cs) — the
 * old page's "save/share this report" section is skipped since that backend feature
 * (GetMatchReportList/SaveMatchReport) was never ported to this ASP.NET Core API and has no
 * Postgres persistence yet, so nothing to wire up there honestly.
 */
export default function MatchReportScreen() {
  const { maleId, femaleId } = useLocalSearchParams<{ maleId: string; femaleId: string }>();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());

  const [report, setReport] = useState<MatchReport | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    setError(null);
    getMatchReport(apiUrlDirect, maleId, femaleId)
      .then(setReport)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load match report'))
      .finally(() => setLoading(false));
  }, [apiUrlDirect, maleId, femaleId]);

  if (loading) {
    return (
      <ThemedView style={styles.centered}>
        <ActivityIndicator />
      </ThemedView>
    );
  }

  if (error || !report) {
    return (
      <ThemedView style={styles.centered}>
        <ThemedText themeColor="textSecondary">{error ?? 'No report available'}</ThemedText>
      </ThemedView>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">
          {report.male.name} & {report.female.name}
        </ThemedText>

        <ThemedView style={[styles.scoreBox, { backgroundColor: report.summary.scoreColor }]}>
          <ThemedText type="title" style={styles.scoreBoxText}>
            {Math.round(report.kutaScore)}%
          </ThemedText>
          <ThemedText style={styles.scoreBoxText}>{report.summary.scoreSummary}</ThemedText>
        </ThemedView>

        <ThemedView style={styles.predictionList}>
          {report.predictionList.map((prediction, index) => (
            <ThemedView key={`${prediction.name}-${index}`} style={styles.predictionCard}>
              <ThemedText type="smallBold">{prediction.name}</ThemedText>
              <ThemedText type="small" themeColor="textSecondary">
                {prediction.description}
              </ThemedText>
            </ThemedView>
          ))}
        </ThemedView>
      </ThemedView>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: Spacing.five,
  },
  scrollContent: {
    alignItems: 'center',
  },
  page: {
    width: '100%',
    maxWidth: MaxContentWidth,
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    paddingBottom: Spacing.six,
    gap: Spacing.four,
  },
  scoreBox: {
    borderRadius: 16,
    padding: Spacing.four,
    alignItems: 'center',
    gap: Spacing.one,
  },
  scoreBoxText: {
    color: '#ffffff',
  },
  predictionList: {
    gap: Spacing.three,
  },
  predictionCard: {
    gap: Spacing.one,
  },
});
