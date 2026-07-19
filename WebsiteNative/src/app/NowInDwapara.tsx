import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/NowInDwapara.razor — Sri Yukteswar's Dwapara Yuga writeup, quoted verbatim. */
export default function NowInDwaparaScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Now In Dwapara</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Rejoice dear reader, for Kali is over. A new age stands before us, new ways of doing
          things.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">By Yukteswar — Serampore 1870s</ThemedText>
          <ThemedText style={styles.paragraph}>
            A short discussion with mathematical calculations of the yugas or ages will explain the
            fact that the present age for the world is Dwapara Yuga, and that 194 years of the Yuga
            have now (A.D. 1894) passed away, bringing a rapid development in man&apos;s knowledge.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            We learn from Oriental astronomy that moons revolve around their planets, and planets
            turning on their axes revolve with their moons round the sun; and the sun, with its
            planets and their moons, takes some star for its dual and revolves round it in about
            24,000 years of our earth — a celestial phenomenon which causes the backward movement
            of the equinoctial points around the zodiac. The sun also has another motion by which it
            revolves round a grand center called Vishnunabhi, which is the seat of the creative
            power, Brahma, the universal magnetism. Brahma regulates dharma, the mental virtue of
            the internal world.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            When the sun in its revolution round its dual comes to the place nearest to this grand
            center, the seat of Brahma (an event which takes place when the Autumnal Equinox comes
            to the first point of Aries), dharma, the mental virtue, becomes so much developed that
            man can easily comprehend all, even the mysteries of Spirit.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            The Autumnal Equinox will be falling, at the beginning of the twentieth century, among
            the fixed stars of the Virgo constellation, and in the early part of the Ascending
            Dwapara Yuga. After 12,000 years, when the sun goes to the place in its orbit which is
            farthest from Brahma, the grand center (an event which takes place when the Autumnal
            Equinox is on the first point of Libra), dharma, the mental virtue, comes to such a
            reduced state that man cannot grasp anything beyond the gross material creation. Again,
            in the same manner, when the sun in its course of revolution begins to advance toward
            the place nearest to the grand center, dharma, the mental virtue, begins to develop;
            this growth is gradually completed in another 12,000 years.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            Each of these periods of 12,000 years brings a complete change, both externally in the
            material world, and internally in the intellectual or electric world, and is called one
            of the Daiva Yugas or Electric Couple. Thus, in a period of 24,000 years, the sun
            completes the revolution around its dual and finishes one electric cycle consisting of
            12,000 years in an ascending arc and 12,000 years in a descending arc.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            Development of dharma, the mental virtue, is but gradual and is divided into four
            different stages in a period of 12,000 years. The time of 1200 years during which the
            sun passes through a 1/20th portion of its orbit is called Kali Yuga. Dharma, the mental
            virtue, is then in its first stage and is only a quarter developed; the human intellect
            cannot comprehend anything beyond the gross material of this ever-changing creation, the
            external world.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            The period of 2400 years during which the sun passes through the 2/20th portion of its
            orbit is called Dwapara Yuga. Dharma, the mental virtue, is then in the second stage of
            development and is but half complete; the human intellect can then comprehend the fine
            matters or electricities and their attributes which are the creating principles of the
            external world.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            The period of 3600 years during which the sun passes through the 3/20th part of its
            orbit is called Treta Yuga. Dharma, the mental virtue, is then in the third stage; the
            human intellect becomes able to comprehend the divine magnetism, the source of all
            electrical forces on which the creation depends for its existence. The period of 4800
            years during which the sun passes through the remaining 4/20th portion of its orbit is
            called Satya Yuga. Dharma, the mental virtue, is then in its fourth stage and completes
            its full development; the human intellect can comprehend all, even God the Spirit
            beyond this visible world.
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
  section: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
});
