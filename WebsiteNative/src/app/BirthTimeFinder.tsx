import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { UnderConstructionNotice } from '@/components/UnderConstructionNotice';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Calculator/BirthTimeFinder.razor — stub + theory writeup, no working calculator yet. */
export default function BirthTimeFinderScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Birth Time Finder</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Find forgotten or lost birth time using advanced algorithms. Currently under
          development. Help out if you can.
        </ThemedText>
        <UnderConstructionNotice />

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Theory</ThemedText>
          <ThemedText style={styles.paragraph}>
            One may wonder how this is even possible. Well the answer is simple we do a
            &quot;dictionary attack&quot; on time. To put it plainly, we generate every possible
            human birth and check which life snapshot matches your life the closest. Ancient Nadi
            palm leafs is a perfect example that this methodology works in practice. This is only
            theory atm, much remains to be done! Checkout our GitHub progress on this calculator.
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
