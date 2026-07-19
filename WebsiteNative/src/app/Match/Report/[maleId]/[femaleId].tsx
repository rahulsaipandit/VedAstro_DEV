import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet } from 'react-native';
import { useLocalSearchParams } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { useAppStore } from '@/store/useAppStore';
import { getMatchReport, saveMatchReport, type MatchReport } from '@/lib/api/match';
import { showErrorToast, showSuccessToast } from '@/lib/toast';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Ported from Website/Pages/Calculator/Match/Report.razor. Calls the live-computed
 * /api/GetMatchReport endpoint (see API/FrontDesk/MatchAPI.cs) for the report itself, plus the
 * new /api/SaveMatchReport endpoint for the "save this report" action - the old Blazor site's
 * SaveMatchReport call never had a real backend, this is genuinely new persistence (see
 * SavedMatchReportEntity), not a port of pre-existing behavior.
 */
export default function MatchReportScreen() {
  const { maleId, femaleId } = useLocalSearchParams<{ maleId: string; femaleId: string }>();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());

  const [report, setReport] = useState<MatchReport | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setLoading(true);
    setError(null);
    getMatchReport(apiUrlDirect, maleId, femaleId)
      .then(setReport)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load match report'))
      .finally(() => setLoading(false));
  }, [apiUrlDirect, maleId, femaleId]);

  async function handleSave() {
    setSaving(true);
    try {
      await saveMatchReport(apiUrlDirect, effectiveOwnerId, maleId, femaleId, report?.notes ?? '');
      showSuccessToast('Match report saved!');
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Failed to save match report');
    } finally {
      setSaving(false);
    }
  }

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

        <Pressable onPress={handleSave} disabled={saving} style={styles.saveButton}>
          <ThemedText type="smallBold" themeColor="background">
            {saving ? 'Saving…' : 'Save Report'}
          </ThemedText>
        </Pressable>

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
  saveButton: {
    backgroundColor: '#1a9c4c',
    alignSelf: 'center',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  predictionList: {
    gap: Spacing.three,
  },
  predictionCard: {
    gap: Spacing.one,
  },
});
