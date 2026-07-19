import { useEffect, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet } from 'react-native';
import { SvgXml } from 'react-native-svg';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { getEventsChartSvg, type TimeRangePreset } from '@/lib/api/eventsChart';
import { Spacing } from '@/constants/theme';
import type { Person } from '@/lib/api/person';

/**
 * Simplified port of ViewComponents/Components/EventsChartViewer.razor. The original's button
 * row (zoom, maximize, print, Google Calendar export, bookmark, share, email PDF, highlight-by-
 * keyword) all sit on top of one thing: a raw SVG string from the server, rendered via a
 * hand-rolled JS charting library (EventsChart.js). None of that JS layer is ported — react-native-
 * svg's SvgXml renders the same server SVG directly, which is the actual payoff (seeing the
 * chart); the interactive chrome around it is deferred (see migration.md).
 */
export function EventsChartViewer({
  apiUrlDirect,
  person,
  preset,
}: {
  apiUrlDirect: string;
  person: Person;
  preset: TimeRangePreset;
}) {
  const [svg, setSvg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    getEventsChartSvg(apiUrlDirect, person, preset)
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
  }, [apiUrlDirect, person, preset]);

  if (loading) {
    return (
      <ThemedView style={styles.centered}>
        <ActivityIndicator />
        <ThemedText type="small" themeColor="textSecondary">
          Generating chart… this can take a little while for long time ranges.
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
      <SvgXml xml={svg} />
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
