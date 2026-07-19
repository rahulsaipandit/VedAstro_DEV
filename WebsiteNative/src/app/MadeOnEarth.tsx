import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/MadeOnEarth.razor. */
export default function MadeOnEarthScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedView style={styles.quote}>
          <ThemedText type="subtitle" style={styles.quoteText}>
            &quot;There is not a single person in the world who could make a pencil.&quot;
          </ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            — Milton Friedman, Nobel Prize &apos;76
          </ThemedText>
        </ThemedView>

        <ThemedText type="title" style={styles.centerTitle}>
          Made On Earth Movement
        </ThemedText>

        <ThemedText style={styles.paragraph}>
          Everywhere on so many things we see &quot;Made in USA&quot;, &quot;Made in Great
          Britain&quot;, &quot;Made in China&quot;, &quot;Made in Sweden&quot; — made here & made
          there. We would love to put such a label on this project, but where to begin?
        </ThemedText>
        <ThemedText style={styles.paragraph}>
          As Milton Friedman put it, even to manufacture a humble &quot;pencil&quot; it takes the
          accumulated might of industry spread across the earth — from graphite mines in Brazil to
          rubber trees in Indonesia, just to make a pencil!
        </ThemedText>
        <ThemedText style={styles.paragraph}>
          The point we&apos;re trying to make here is this: VedAstro is only possible because of
          people and resources from many countries spread across the earth and time. As such, the
          only valid label for this project is:
        </ThemedText>

        <ThemedText type="subtitle" style={styles.centerTitle}>
          &quot;Made on Earth&quot;
        </ThemedText>
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
    gap: Spacing.three,
  },
  quote: {
    alignItems: 'center',
    gap: Spacing.one,
    paddingVertical: Spacing.four,
  },
  quoteText: {
    textAlign: 'center',
  },
  centerTitle: {
    textAlign: 'center',
  },
  paragraph: {
    lineHeight: 22,
  },
});
