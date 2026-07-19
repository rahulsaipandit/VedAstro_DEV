import { useEffect, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { GeoLocationInput, DEFAULT_GEO_LOCATION } from '@/components/GeoLocationInput';
import { useAppStore } from '@/store/useAppStore';
import { buildBirthTimeJson } from '@/lib/time';
import { getTimezoneOffsetForLocation, parseOffsetMinutes, type GeoLocation } from '@/lib/api/geo';
import { getSunriseTime, getSunsetTime } from '@/lib/api/timeTools';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Port of Website/Pages/Calculator/SunRiseSetTime.razor. The original auto-recalculates on every
 * render (OnAfterRender) - that's replicated here as a useEffect keyed on location, so it still
 * updates automatically whenever the location changes without a separate "Calculate" button.
 */
export default function SunRiseSetTimeScreen() {
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const [location, setLocation] = useState<GeoLocation>(DEFAULT_GEO_LOCATION);
  const [sunrise, setSunrise] = useState('');
  const [sunset, setSunset] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    (async () => {
      const now = new Date();
      const offset = await getTimezoneOffsetForLocation(apiUrlDirect, location, now);
      const time = buildBirthTimeJson(now, parseOffsetMinutes(offset), location);
      const [rise, set] = await Promise.all([
        getSunriseTime(apiUrlDirect, time),
        getSunsetTime(apiUrlDirect, time),
      ]);
      if (!cancelled) {
        setSunrise(rise);
        setSunset(set);
      }
    })().finally(() => {
      if (!cancelled) setLoading(false);
    });
    return () => {
      cancelled = true;
    };
  }, [apiUrlDirect, location]);

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Sunrise Time</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Accurate sunrise &amp; sunset time based on your coordinates. Time when sun&apos;s disc
          center meets the horizon.
        </ThemedText>

        {loading ? (
          <ActivityIndicator style={styles.loading} />
        ) : (
          <ThemedView style={styles.resultRow}>
            <ThemedView style={styles.resultCard} type="backgroundElement">
              <ThemedText type="smallBold">Sunrise</ThemedText>
              <ThemedText type="subtitle">{sunrise}</ThemedText>
            </ThemedView>
            <ThemedView style={styles.resultCard} type="backgroundElement">
              <ThemedText type="smallBold">Sunset</ThemedText>
              <ThemedText type="subtitle">{sunset}</ThemedText>
            </ThemedView>
          </ThemedView>
        )}

        <GeoLocationInput apiUrlDirect={apiUrlDirect} location={location} onChange={setLocation} />
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
    gap: Spacing.four,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  loading: {
    marginVertical: Spacing.four,
  },
  resultRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
  },
  resultCard: {
    flex: 1,
    minWidth: 160,
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.one,
  },
});
