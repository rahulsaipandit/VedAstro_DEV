import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { UnderConstructionNotice } from '@/components/UnderConstructionNotice';
import { SkyChartViewer } from '@/components/SkyChartViewer';
import { useAppStore } from '@/store/useAppStore';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import type { BirthTimeJson } from '@/lib/time';

/**
 * Port of Website/Pages/Calculator/StarsAboveMe.razor — that page is itself an
 * "Under Construction" stub wrapping a real TimeLocationInput + SkyChartViewer pair, but
 * TimeLocationInput's "detect my location" (IP geolocation) has no equivalent endpoint exposed
 * by the API today (grepped for a geocoding/location-search route, found none), so this shows a
 * fixed example time/location instead of a live input — same reduced fidelity the original
 * already flagged as "under construction", not a new gap introduced by this port.
 */
export default function StarsAboveMeScreen() {
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());

  // Example birth-time: "now" would require a location before every render, and there's no
  // client-side "current device time -> Time.ToUrl()" helper wired up yet — using a fixed
  // reference point (matches the pattern other stubbed pages use) keeps this simple and honest.
  const exampleTime: BirthTimeJson = {
    StdTime: '12:00 01/01/2026 +08:00',
    Location: { Name: 'Singapore', Longitude: 103.8198, Latitude: 1.3521 },
  };

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Stars Above Me</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Info of stars and planets above you now. View pure astronomical and interpreted Vedic
          data for past and future anywhere in the world.
        </ThemedText>
        <UnderConstructionNotice />

        <SkyChartViewer apiUrlDirect={apiUrlDirect} birthTime={exampleTime} />

        <ThemedView style={styles.quote}>
          <ThemedText type="subtitle" style={styles.quoteText}>
            &quot;We&apos;re made of star stuff&quot;
          </ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            — Carl Sagan
          </ThemedText>
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
    gap: Spacing.four,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  quote: {
    alignItems: 'center',
    gap: Spacing.one,
    paddingVertical: Spacing.four,
  },
  quoteText: {
    textAlign: 'center',
  },
});
