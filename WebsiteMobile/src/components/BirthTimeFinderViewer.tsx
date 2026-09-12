import { useEffect, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet } from 'react-native';
import { SvgXml } from 'react-native-svg';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { getBirthTimeFinderSvg, type BirthTimeFinderOptions } from '@/lib/api/birthTimeFinder';
import { Spacing } from '@/constants/theme';

/**
 * RN port of the Console app's "Find Birth Time - Life Predictor - Person" tool
 * (Console/Program.cs's FindBirthTimeEventsChartPerson) — renders the combined SVG of
 * life-event charts for every candidate birth time in the scanned hour range, one below
 * another, so the user can visually compare which candidate best matches remembered life
 * events. Backed directly by API/FrontDesk/BirthTimeFinderAPI.cs (synchronous, no job-poll).
 */
export function BirthTimeFinderViewer({
  apiUrlDirect,
  personId,
  options,
}: {
  apiUrlDirect: string;
  personId: string;
  options: BirthTimeFinderOptions;
}) {
  const [svg, setSvg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    getBirthTimeFinderSvg(apiUrlDirect, personId, options)
      .then((result) => {
        if (!cancelled) setSvg(result);
      })
      .catch((e) => {
        if (!cancelled) setError(e instanceof Error ? e.message : 'Failed to generate chart');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiUrlDirect, personId, JSON.stringify(options)]);

  if (loading) {
    return (
      <ThemedView style={styles.centered}>
        <ActivityIndicator />
        <ThemedText type="small" themeColor="textSecondary">
          Scanning possible birth times… this can take a while for finer precision or wider
          hour ranges.
        </ThemedText>
      </ThemedView>
    );
  }

  if (error || !svg) {
    return (
      <ThemedView style={styles.centered}>
        <ThemedText themeColor="textSecondary">{error ?? 'Chart unavailable.'}</ThemedText>
      </ThemedView>
    );
  }

  return (
    <ScrollView horizontal style={styles.scroll}>
      <ScrollView>
        <SvgXml xml={svg} />
      </ScrollView>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  centered: {
    alignItems: 'center',
    gap: Spacing.two,
    paddingVertical: Spacing.five,
  },
  scroll: {
    borderRadius: 12,
  },
});
