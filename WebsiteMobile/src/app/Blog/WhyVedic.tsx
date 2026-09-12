import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Blog/WhyVedic.razor. */
export default function WhyVedicScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Why not Mayan Astrology?</ThemedText>

        <ThemedText style={styles.paragraph}>
          Wise men of all ages across the world have spoken of astrology in one form or another.
          As such, astrology does not belong to one man, creed or country — it is the knowledge of
          the ages.
        </ThemedText>
        <ThemedText style={styles.paragraph}>
          So this begs the question: &quot;Why &apos;Vedic&apos; Astrology? Why not Mayan
          Astrology? Or Egyptian or Greek?&quot; The simple answer is, for some reason Vedic
          records survived the slaughter of time. Many do not realize how much ancient knowledge
          has been destroyed and lost — the burning of the Library of Alexandria and destruction
          of Mayan codices by Conquistadors are but a few examples. So we work with what we have
          left and continue from there.
        </ThemedText>
        <ThemedText style={styles.paragraph}>
          If you could compare side by side all the ancient texts from around the world available
          today, you&apos;ll find that texts from what we call the &quot;Vedic Age&quot; surpass
          any other.
        </ThemedText>
        <ThemedText style={styles.paragraph}>
          This period refers to text written in Sanskrit at a time between 1500 BC and 600 BC in
          Southern Asia. We say &quot;Southern Asia&quot; and not &quot;India&quot;, because
          evidence of Vedic culture has been found not only in India, but also in Cambodia,
          Vietnam, Thailand, Malaysia, Indonesia, Sri Lanka, Tibet and Nepal.
        </ThemedText>
        <ThemedText style={styles.paragraph}>
          Our current knowledge of Vedic Astrology is by no means perfect — there are holes in the
          ancient pages. It might surprise some to know there are ancient texts in South India that
          predict a person&apos;s life down to their name, found not by birth date but by
          thumbprint — texts called &quot;Naadi Palm Leaves&quot;, most unavailable to the public.
          For these reasons, even Vedic Astrology has not been immune to the ravages of time. Today
          we can predict accurately when a person will have a bad day, but cannot tell how we know
          — we are only counting and calculating what we read, not yet writing new astrological
          facts.
        </ThemedText>
        <ThemedText style={styles.paragraph}>
          Astrology is as real and as useful as a car. We as humanity have lost the key — that
          doesn&apos;t mean the car doesn&apos;t work. Slowly, through experimentation and global
          exchange of astrological data, we can as a human race not only bring back Astrology to
          its former glory but also advance it further.
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
  paragraph: {
    lineHeight: 22,
  },
});
