import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Remedy.razor. Product photo cards (astro bangles) not carried over — no product imagery bundled in WebsiteNative yet; article text is a full port. */
export default function RemedyScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Astro Remedy</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Overcome a planet&apos;s negative pull with artificial light from gems and metals.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Why?</ThemedText>
          <ThemedText style={styles.paragraph}>
            It is only when a traveler has reached his goal that he is justified in discarding his
            maps. During the journey, he takes advantage of any convenient shortcut. The ancient
            rishis discovered many ways to curtail the period of man&apos;s exile in delusion.
            There are certain mechanical features in the law of karma which can be skillfully
            adjusted by the fingers of wisdom.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            By a number of means — prayer, will power, yoga meditation, consultation with saints,
            and the use of astrological bangles — the adverse effects of past wrongs can be
            minimized or nullified.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">When needed?</ThemedText>
          <ThemedText style={styles.paragraph}>
            Just as a house can be fitted with a copper rod to absorb the shock of lightning, so
            the bodily temple can be benefited by various protective measures. Ages ago our yogis
            discovered that pure metals emit an astral light which is powerfully counteractive to
            negative pulls of the planets.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            This problem received attention from our rishis; they found helpful not only a
            combination of metals, but also of plants and — most effective of all — faultless
            jewels of not less than two carats. One little-known fact is that the proper jewels,
            metals, or plant preparations are valueless unless the required weight is secured, and
            unless these remedial agents are worn next to the skin.
          </ThemedText>
          <ThemedView style={styles.quote}>
            <ThemedText type="smallBold" style={styles.quoteText}>
              &quot;...will not be missed by those for whom it is meant.&quot;
            </ThemedText>
            <ThemedText type="small" themeColor="textSecondary">
              — Yukteswar Giri
            </ThemedText>
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
    gap: Spacing.four,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  section: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
  quote: {
    gap: Spacing.one,
    paddingVertical: Spacing.two,
  },
  quoteText: {
    lineHeight: 22,
  },
});
