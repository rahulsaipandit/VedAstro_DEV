import { useCallback, useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, StyleSheet } from 'react-native';
import { useFocusEffect, useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Icon, heartIconFromIconify } from '@/components/Icon';
import { useAppStore } from '@/store/useAppStore';
import { getMatchReportList, type MatchReport } from '@/lib/api/match';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Port of Website/Pages/Calculator/Match/SavedReports.razor +
 * ViewComponents/Components/MatchReportListViewer.razor (the couple/score/notes table). Backed
 * by the new /api/GetMatchReportList endpoint - real Postgres persistence added specifically
 * to make this page possible (see SavedMatchReportEntity, migration.md's "Saved reports"
 * decision), not carried over from an existing implementation.
 */
export default function SavedMatchReportsScreen() {
  const router = useRouter();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());

  const [reports, setReports] = useState<MatchReport[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    getMatchReportList(apiUrlDirect, effectiveOwnerId)
      .then(setReports)
      .catch((e) => setError(e instanceof Error ? e.message : 'Failed to load saved match reports'))
      .finally(() => setLoading(false));
  }, [apiUrlDirect, effectiveOwnerId]);

  // Reload whenever this screen regains focus (e.g. coming back after saving a new report from
  // Match/Report), not just on first mount.
  useFocusEffect(useCallback(() => { load(); }, [load]));
  useEffect(() => { load(); }, [load]);

  if (loading) {
    return (
      <ThemedView style={styles.centered}>
        <ActivityIndicator />
      </ThemedView>
    );
  }

  return (
    <ThemedView style={styles.page}>
      <ThemedText type="title" style={styles.title}>
        Saved Matches
      </ThemedText>
      <ThemedText themeColor="textSecondary" style={styles.subtitle}>
        Match reports saved under your account
      </ThemedText>

      {error ? (
        <ThemedText themeColor="textSecondary">{error}</ThemedText>
      ) : reports.length === 0 ? (
        <ThemedText themeColor="textSecondary">No saved match reports yet.</ThemedText>
      ) : (
        <FlatList
          data={reports}
          keyExtractor={(item) => item.id}
          contentContainerStyle={styles.list}
          renderItem={({ item }) => (
            <ThemedView style={styles.row} type="backgroundElement">
              <ThemedView style={styles.couple} type="backgroundElement">
                <Icon name={heartIconFromIconify(item.summary.heartIcon)} size={18} color={item.summary.scoreColor} />
                <ThemedText type="smallBold">
                  {item.male.name} & {item.female.name}
                </ThemedText>
              </ThemedView>
              <ThemedText style={{ color: item.summary.scoreColor }} type="smallBold">
                {Math.round(item.kutaScore)}%
              </ThemedText>
              <ThemedText type="small" themeColor="textSecondary" style={styles.notes} numberOfLines={1}>
                {item.notes || '—'}
              </ThemedText>
              <Pressable
                onPress={() => router.push(`/${PageRoute.MatchReport}/${item.male.id}/${item.female.id}` as never)}
                style={styles.viewButton}>
                <ThemedText type="smallBold" themeColor="background">
                  View
                </ThemedText>
              </Pressable>
            </ThemedView>
          )}
        />
      )}
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  page: {
    flex: 1,
    width: '100%',
    maxWidth: MaxContentWidth,
    alignSelf: 'center',
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    gap: Spacing.two,
  },
  title: {
    marginBottom: Spacing.half,
  },
  subtitle: {
    marginBottom: Spacing.three,
  },
  list: {
    gap: Spacing.two,
    paddingBottom: Spacing.six,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.three,
  },
  couple: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.one,
    flex: 2,
  },
  notes: {
    flex: 2,
  },
  viewButton: {
    backgroundColor: '#1a9c4c',
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.one,
  },
});
