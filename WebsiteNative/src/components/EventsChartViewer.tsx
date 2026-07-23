import { useEffect, useMemo, useRef, useState } from 'react';
import { ActivityIndicator, Platform, ScrollView, StyleSheet, View, type GestureResponderEvent } from 'react-native';
import { SvgXml } from 'react-native-svg';

import { Icon } from './Icon';
import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import {
  buildSmartSummary,
  getEventsChartSvg,
  parseEventRects,
  type EventRect,
  type TimeRangePreset,
} from '@/lib/api/eventsChart';
import { Spacing } from '@/constants/theme';
import type { Person } from '@/lib/api/person';

/**
 * Simplified port of ViewComponents/Components/EventsChartViewer.razor. The original's button
 * row (zoom, maximize, print, Google Calendar export, bookmark, share, email PDF, highlight-by-
 * keyword) all sit on top of one thing: a raw SVG string from the server, rendered via a
 * hand-rolled JS charting library (EventsChart.js). None of that JS layer is ported — react-native-
 * svg's SvgXml renders the same server SVG directly, which is the actual payoff (seeing the
 * chart); the interactive chrome around it is deferred (see migration.md) EXCEPT for the "Smart
 * Summary" hover/touch tooltip below, which is reimplemented from scratch (not DOM-dependent like
 * the original's tippy.js) reading the same per-event <rect> attributes the old EventsChart.js's
 * cursor-line legend read (eventname/eventdescription/naturescore/age/stdtime/summarycategories).
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
  const [cursorX, setCursorX] = useState<number | null>(null);
  const [chartHeight, setChartHeight] = useState(0);
  const containerRef = useRef<View>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    setCursorX(null);
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

  const eventRects = useMemo(() => (svg ? parseEventRects(svg) : []), [svg]);

  const rectsAtCursor: EventRect[] = useMemo(() => {
    if (cursorX == null) return [];
    return eventRects.filter((rect) => cursorX >= rect.x && cursorX < rect.x + rect.width);
  }, [eventRects, cursorX]);

  function moveCursorTo(clientOrLocationX: number) {
    setCursorX(Math.max(0, Math.round(clientOrLocationX)));
  }

  // Web gets real mouse hover; touch (mobile web + native) scrubs by dragging a finger along the
  // timeline, since touchscreens have no hover concept. nativeEvent.locationX is already relative
  // to this view, which lives inside the ScrollView's scrolling content - no manual scroll-offset
  // math needed (it moves with the content).
  function handleTouch(evt: GestureResponderEvent) {
    moveCursorTo(evt.nativeEvent.locationX);
  }

  function handleMouseMove(evt: { clientX: number }) {
    const node = containerRef.current as unknown as HTMLElement | null;
    if (!node?.getBoundingClientRect) return;
    const rect = node.getBoundingClientRect();
    moveCursorTo(evt.clientX - rect.left);
  }

  const webHoverHandlers =
    Platform.OS === 'web'
      ? {
          onMouseMove: handleMouseMove,
          onMouseLeave: () => setCursorX(null),
        }
      : {};

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

  const activeRect = rectsAtCursor[0];
  const summaryText = cursorX != null && rectsAtCursor.length > 0 ? buildSmartSummary(rectsAtCursor) : null;

  return (
    <ScrollView horizontal style={styles.scroll}>
      <View
        ref={containerRef}
        style={styles.chartContent}
        onLayout={(e) => setChartHeight(e.nativeEvent.layout.height)}
        onTouchStart={handleTouch}
        onTouchMove={handleTouch}
        onTouchEnd={() => setCursorX(null)}
        {...(webHoverHandlers as object)}>
        <SvgXml xml={svg} />

        {cursorX != null && (
          <View pointerEvents="none" style={[styles.cursorLine, { left: cursorX, height: chartHeight }]} />
        )}

        {summaryText && activeRect && (
          <View
            pointerEvents="none"
            style={[styles.tooltip, { left: Math.max(0, cursorX! - 110), top: chartHeight + Spacing.two }]}>
            {activeRect.stdTime ? (
              <ThemedText type="small" style={styles.tooltipTime}>
                {activeRect.stdTime.split(' ').slice(0, 2).join(' ')} AGE: {activeRect.age}
              </ThemedText>
            ) : null}
            <View style={styles.tooltipBox}>
              <Icon name="sparkles" size={14} color="#fff" />
              <ThemedText type="small" style={styles.tooltipText}>
                {summaryText}
              </ThemedText>
            </View>
          </View>
        )}
      </View>
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
  chartContent: {
    position: 'relative',
    alignSelf: 'flex-start',
  },
  cursorLine: {
    position: 'absolute',
    top: 0,
    width: 2,
    backgroundColor: '#0d6efd',
  },
  tooltip: {
    position: 'absolute',
    width: 220,
    gap: 4,
  },
  tooltipTime: {
    alignSelf: 'flex-start',
    color: '#fff',
    backgroundColor: '#0d6efd',
    paddingHorizontal: Spacing.two,
    paddingVertical: 2,
    borderRadius: 6,
    overflow: 'hidden',
  },
  tooltipBox: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: Spacing.one,
    backgroundColor: '#0d6efd',
    borderRadius: 8,
    padding: Spacing.two,
  },
  tooltipText: {
    flex: 1,
    color: '#fff',
    lineHeight: 18,
  },
});
