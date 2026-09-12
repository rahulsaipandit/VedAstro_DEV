import { Linking, Platform, Pressable, ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { Icon } from '@/components/Icon';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';

const SOURCE_REPO_URL = 'https://github.com/Vedic-Panchanga/Shri-Jagannath-Panchang';
const REFERENCE_COPY_WEB_PATH = '/reference/ShriJagannathPanchang.html';

/**
 * Explains, at a high level, how VedAstro's Panchang/tithi/festival engine differs from
 * github.com/Vedic-Panchanga/Shri-Jagannath-Panchang's SJPL_5.08.html, and links to an
 * unmodified, attributed copy of that file (WebsiteNative/public/reference/ShriJagannathPanchang.html)
 * so visitors can try both side-by-side. See docs/vedAstroArchitecture.md's "Adhika-Masa Detection
 * & Festival Calendar Generator" section for the full technical comparison this page summarizes.
 * That reference copy is a static asset only (Expo Router's public/ web passthrough) - no tests,
 * no API, not wired into VedAstro's own calculators in any way.
 */
export default function ComparePanchangEnginesScreen() {
  const theme = useTheme();

  function openReferenceCopy() {
    // public/ is a web-only static-serving mechanism (Expo Router) - there's no equivalent
    // bundled asset on iOS/Android, so native platforms open the GitHub source instead.
    Linking.openURL(Platform.OS === 'web' ? REFERENCE_COPY_WEB_PATH : SOURCE_REPO_URL);
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Compare Panchang Engines</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          VedAstro's Festival Calendar and day-detail Panchang were built after comparing our
          approach against an existing, independent Panchang calculator. This page summarizes
          what's different and links to an unmodified copy of that calculator so you can try both
          yourself.
        </ThemedText>

        <ThemedView style={[styles.card, { borderColor: theme.backgroundSelected }]}>
          <ThemedText type="smallBold">Source</ThemedText>
          <Pressable onPress={() => Linking.openURL(SOURCE_REPO_URL)}>
            <ThemedText style={styles.link}>Vedic-Panchanga/Shri-Jagannath-Panchang</ThemedText>
          </Pressable>
          <ThemedText type="small" themeColor="textSecondary">
            File SJPL_5.08.html, commit e442e8d (13 Oct 2022, "version 5.08"). Credited by that
            repo's own README to Sanjay Prabhakaran's earlier work, with routines adapted from the
            Maitreya astrology software (C). No license was declared in the source repository at
            the time this copy was made.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="smallBold">Ephemeris</ThemedText>
          <ThemedText themeColor="textSecondary">
            Their engine uses a custom analytic series (manda/sighra epicycle parameters, in the
            classical Surya Siddhanta lineage) for planetary positions. VedAstro uses Swiss
            Ephemeris throughout, which is accurate to arc-seconds against JPL's DE ephemerides -
            meaningfully more precise than a manual epicycle model, especially further from its
            reference epoch.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="smallBold">Finding the exact tithi moment</ThemedText>
          <ThemedText themeColor="textSecondary">
            Their engine estimates tithi start/end with a single linear interpolation from one
            instantaneous Moon/Sun-speed reading. VedAstro iterates a Newton-style search until the
            error is under 0.0005°, which handles the Moon's non-uniform orbital speed
            (~11.8-15.4°/day) more robustly than one linear step.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="smallBold">Adhika (leap) month detection</ThemedText>
          <ThemedText themeColor="textSecondary">
            Their engine already had real Adhika-masa detection - the one place it was ahead of
            VedAstro's own code before this work. VedAstro's version derives it directly from which
            sidereal zodiac sign the Sun occupies at each amanta month's start/end, verified against
            real dates (2023's widely-reported "Adhik Shravan", and Ganesh Chaturthi 2023 landing
            correctly one month later than usual because of it).
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="smallBold">Festival dates &amp; architecture</ThemedText>
          <ThemedText themeColor="textSecondary">
            Neither engine hardcodes a per-year festival date table - both compute Diwali, Ramnavami
            etc. fresh from tithi/month rules. Their calculator is a single ~588KB client-side
            HTML/JS file with no tests and no backend. VedAstro's engine lives in a tested C#
            library behind a REST API, with a typed frontend on top (this app).
          </ThemedText>
        </ThemedView>

        <Pressable onPress={openReferenceCopy} style={styles.openButton}>
          <Icon name="globe" size={16} color="#ffffff" />
          <ThemedText type="smallBold" themeColor="background">
            Open the reference calculator
          </ThemedText>
        </Pressable>
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
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    paddingBottom: Spacing.six,
    gap: Spacing.four,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  card: {
    borderWidth: 1,
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.one,
  },
  link: {
    color: '#2F6FED',
    textDecorationLine: 'underline',
  },
  section: {
    gap: Spacing.one,
  },
  openButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.two,
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
});
