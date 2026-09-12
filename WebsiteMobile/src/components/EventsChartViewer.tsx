import { useEffect, useMemo, useRef, useState } from 'react';
import { ActivityIndicator, Platform, ScrollView, StyleSheet, View, type GestureResponderEvent } from 'react-native';
import { SvgXml } from 'react-native-svg';

import { Icon } from './Icon';
import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import {
  buildSmartSummary,
  getEventsChartSvg,
  getEventsChartSvgCustomRange,
  parseContentPadding,
  parseEventRects,
  type CustomRange,
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
  customRange,
  eventTagsCsv,
  algorithmNamesCsv,
  ayanamsaName,
  daysPerPixelOverride,
}: {
  apiUrlDirect: string;
  person: Person;
  /** Ignored when `customRange` is given. */
  preset: TimeRangePreset;
  /** GoodTimeFinder's "Custom" option — an explicit Year/Month range, bypassing `preset` entirely. */
  customRange?: CustomRange;
  /** Defaults to LifePredictor's PD1-PD7 dasa levels if omitted — pass explicitly for any other screen. */
  eventTagsCsv?: string;
  algorithmNamesCsv?: string;
  ayanamsaName?: string;
  daysPerPixelOverride?: number;
}) {
  const [svg, setSvg] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [cursorX, setCursorX] = useState<number | null>(null);
  const [chartHeight, setChartHeight] = useState(0);
  const [scrollOffsetX, setScrollOffsetX] = useState(0);
  const [viewportWidth, setViewportWidth] = useState(0);
  const containerRef = useRef<View>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    setCursorX(null);
    const options = { eventTagsCsv, algorithmNamesCsv, ayanamsaName, daysPerPixelOverride };
    const request = customRange
      ? getEventsChartSvgCustomRange(apiUrlDirect, person, customRange, options)
      : getEventsChartSvg(apiUrlDirect, person, preset, options);
    request
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
  }, [apiUrlDirect, person, preset, customRange, eventTagsCsv, algorithmNamesCsv, ayanamsaName, daysPerPixelOverride]);

  const eventRects = useMemo(() => (svg ? parseEventRects(svg) : []), [svg]);
  // Every <rect>'s raw x/y is visually shifted on-screen by this much (EventsChartFactory.cs's
  // contentHead <g transform>) - the actual on-screen pixel position under the pointer/finger is
  // rect.x + contentPadding, so subtract it back off before comparing against raw rect.x.
  const contentPadding = useMemo(() => (svg ? parseContentPadding(svg) : 0), [svg]);

  const rectsAtCursor: EventRect[] = useMemo(() => {
    if (cursorX == null) return [];
    const x = cursorX - contentPadding;
    return eventRects.filter((rect) => x >= rect.x && x < rect.x + rect.width);
  }, [eventRects, cursorX, contentPadding]);

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

  // The tooltip renders OUTSIDE the horizontal ScrollView, in normal (non-absolute) document
  // flow, deliberately: a horizontal-only ScrollView clips vertical overflow at its own content
  // height, so a tooltip placed below the chart via absolute positioning *inside* the scrolled
  // content was being silently clipped - present in the tree, invisible on screen. Since it's now
  // a sibling, its horizontal position has to account for how far the chart has been scrolled
  // (cursorX is relative to the scrolled content; the tooltip's own container isn't scrolled).
  const tooltipWidth = 220;
  const tooltipLeft =
    cursorX != null
      ? Math.min(Math.max(0, cursorX - scrollOffsetX - tooltipWidth / 2), Math.max(0, viewportWidth - tooltipWidth))
      : 0;

  return (
    <View onLayout={(e) => setViewportWidth(e.nativeEvent.layout.width)}>
      <ScrollView
        horizontal
        style={styles.scroll}
        onScroll={(e) => setScrollOffsetX(e.nativeEvent.contentOffset.x)}
        scrollEventThrottle={16}>
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
        </View>
      </ScrollView>

      {summaryText && activeRect && (
        <View style={styles.tooltipRow}>
          <View pointerEvents="none" style={[styles.tooltip, { marginLeft: tooltipLeft, width: tooltipWidth }]}>
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
        </View>
      )}
    </View>
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
  tooltipRow: {
    marginTop: -Spacing.one,
  },
  tooltip: {
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
