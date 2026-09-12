import { useEffect, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { GeoLocationInput, DEFAULT_GEO_LOCATION } from '@/components/GeoLocationInput';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { buildBirthTimeJson, lmtTextForInstant, longitudeToLmtOffsetMinutes } from '@/lib/time';
import { formatOffsetMinutes, getTimezoneOffsetForLocation, parseOffsetMinutes, type GeoLocation } from '@/lib/api/geo';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Port of Website/Pages/Calculator/LocalMeanTime.razor's first two tabs ("Time Now" and
 * "Longitude to Timezone") — both clean, well-defined calculations. The original's other two
 * tabs are deliberately NOT ported:
 * - "LMT to STD": Calculate.LmtToStd's semantics here are genuinely confusing even in the
 *   original (it re-tags a naive LMT wall-clock reading with the LMT offset of a *different*
 *   longitude input and calls the result "STD", which isn't a real timezone conversion) - this
 *   is also why `LMTToSTDTest` is flagged in migration.md's "Known remaining items" as one of
 *   the 10 structurally-unfixable-as-written LibraryTests failures. Not chased further here.
 * - "Location to Timezone": functionally redundant with what "Time Now" already computes
 *   under the hood (GeoLocationToTimezone) — same value, no separate UI need.
 */
export default function LocalMeanTimeScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());

  const [location, setLocation] = useState<GeoLocation>(DEFAULT_GEO_LOCATION);
  const [lmtText, setLmtText] = useState('');
  const [stdText, setStdText] = useState('');
  const [loading, setLoading] = useState(true);

  const [longitudeInput, setLongitudeInput] = useState('103.8198');
  const lmtOffsetMinutes = longitudeToLmtOffsetMinutes(parseFloat(longitudeInput) || 0);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    (async () => {
      const now = new Date();
      const stdOffset = await getTimezoneOffsetForLocation(apiUrlDirect, location, now);
      if (cancelled) return;
      const stdOffsetMinutes = parseOffsetMinutes(stdOffset);
      setStdText(buildBirthTimeJson(now, stdOffsetMinutes, location).StdTime);
      setLmtText(lmtTextForInstant(now, location.longitude));
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
        <ThemedText type="title">Local Mean Time (LMT)</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          LMT is the real time at a place based on its longitude. At 12PM LMT the Sun will be
          directly overhead.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Time Now</ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            Local and Standard time now at a given location.
          </ThemedText>
          {loading ? (
            <ActivityIndicator style={styles.loading} />
          ) : (
            <>
              <ThemedText type="smallBold">LMT: {lmtText}</ThemedText>
              <ThemedText type="smallBold">STD: {stdText}</ThemedText>
            </>
          )}
          <GeoLocationInput apiUrlDirect={apiUrlDirect} location={location} onChange={setLocation} />
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Longitude to LMT Offset</ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            Input a longitude to see the LMT offset used for that location.
          </ThemedText>
          <ThemedText type="smallBold">LMT Offset: {formatOffsetMinutes(lmtOffsetMinutes)}</ThemedText>
          <ThemedView style={[styles.inputRow, { borderColor: theme.backgroundSelected }]}>
            <ThemedText type="small" themeColor="textSecondary">
              Longitude
            </ThemedText>
            <TextInput
              value={longitudeInput}
              onChangeText={setLongitudeInput}
              keyboardType="numeric"
              placeholder="-180 to 180"
              placeholderTextColor={theme.textSecondary}
              style={[styles.input, { color: theme.text }]}
            />
          </ThemedView>
        </ThemedView>
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
    gap: Spacing.five,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  section: {
    gap: Spacing.two,
  },
  loading: {
    alignSelf: 'flex-start',
    marginVertical: Spacing.one,
  },
  inputRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
  },
  input: {
    flex: 1,
    paddingVertical: Spacing.two,
  },
});
