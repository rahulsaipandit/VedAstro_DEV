import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { MaxContentWidth, Spacing } from '@/constants/theme';

function preciseAge(): string {
  const birthDate = new Date(2014, 11, 1).getTime();
  const years = (Date.now() - birthDate) / (1000 * 60 * 60 * 24 * 365.25);
  return `${years.toFixed(2)} Years Old`;
}

/** Port of Website/Pages/About.razor. */
export default function AboutScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">About</ThemedText>

        <ThemedText style={styles.lead}>
          Anybody who has studied Vedic Astrology knows well how accurate it can be. But also how
          complex it can get to make accurate predictions. It takes decades of experience to be
          able to make accurate prediction. Thus, this knowledge only reaches a limited people.
          This project is an effort to change that.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Goal</ThemedText>
          <ThemedText style={styles.paragraph}>
            Our goal is to make Vedic Astrology easily accessible to anybody, so that people can
            use it in their daily lives for their benefit. Vedic Astrology in Sanskrit means
            &quot;Light&quot; — it lights our future so we can change it, and it lights our past,
            to understand our mistakes.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">How</ThemedText>
          <ThemedText style={styles.paragraph}>
            Using modern computational technologies & methods we can simplify the complexity of
            Vedic Astrology. Calculating planet strength (Bhava Bala) used to take hours; now with
            computers we can calculate it in milliseconds — allowing accurate predictions with
            little to no knowledge.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">{preciseAge()}</ThemedText>
          <ThemedText style={styles.paragraph}>
            The first line of code for this project was written in late 2014 at Itä-Pasila. Back
            then it was a simple desktop software, with no UI and only text display. With
            continued support from users, this project has steadily developed to what it is
            today, helping people from all over the world.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Credits & Reference</ThemedText>
          <ThemedText style={styles.paragraph}>
            Thanks to B.V. Raman and his grandfather B. Suryanarain Rao for pioneering easy to read
            astrology books. Credit also goes to St. Jean-Baptiste de La Salle for proving the
            efficacy of free and open work for the benefit of all.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            Astronomical calculation was made possible by NASA JPL data via &quot;Swiss
            Ephemeris&quot;, ported by SwissEphNet. Last but not least, we thank users like you who
            keep this project going.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Myth & Philosophy</ThemedText>
          <ThemedText style={styles.paragraph}>
            It is told in the stories of old, that a heathen god moved by the plight of a child
            gave onto him the knowledge of the stars as compensation for the tears. The child in
            turn passed that gift to every soul he knew, with no check on price or pedigree. And so
            astrology was born.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            As such this knowledge stands as an opportunity for every soul to realize that even
            though we play a role in the world, we are not made of it — that we are souls first,
            and the planets do not have a say on it.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            A means of escape from the thralldom of cosmic illusion is what it really is. It&apos;s
            their gift to us, the &quot;world folk&quot;.
          </ThemedText>
          <ThemedView style={styles.quote}>
            <ThemedText type="smallBold" style={styles.quoteText}>
              &quot;Mechanical features in the law of karma, can be skillfully adjusted by the
              fingers of wisdom.&quot;
            </ThemedText>
            <ThemedText type="small" themeColor="textSecondary">
              — Yukteswar
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
  lead: {
    fontSize: 18,
    lineHeight: 26,
  },
  section: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
  quote: {
    gap: Spacing.one,
    paddingVertical: Spacing.three,
  },
  quoteText: {
    lineHeight: 22,
  },
});
