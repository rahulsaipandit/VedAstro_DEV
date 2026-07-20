import { Pressable, ScrollView, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/TrainAIAstrologer.razor. */
export default function TrainAIAstrologerScreen() {
  const router = useRouter();

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Train AI Astrologer</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Your feedback helps improve the Machine Learning model. Together we can create a very
          accurate AI based predictor.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">How It Works</ThemedText>
          <ThemedText style={styles.paragraph}>
            Using Vedic astrology data as a start point, we create a table of predictions for
            different aspects of a human life. For now we take 12 aspects, representing the 12
            houses — this isn&apos;t limited to 12 and will be expanded. During this process, we
            assume the data being fed into the model is 100% accurate. This is necessary in the
            beginning. We then use that trained model to make predictions for a new person not in
            the original data set, which can also be compared to predictions made by conventional
            astrology alone — giving further insight into improving the Vedic calculations.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Global Effort</ThemedText>
          <ThemedText style={styles.paragraph}>
            These predictions are then further improved by humans — ML training collected from
            people all over the world into one data-set, allowing the creation of a universal AI
            Astrologer. Pumping the input received from users back into the model training
            pipeline creates a self-correcting feedback loop. With time and continued improvement,
            this tool holds great potential for the benefit of all people.
          </ThemedText>
          <Pressable onPress={() => router.push(`/${PageRoute.JoinOurFamily}` as never)} style={styles.joinButton}>
            <ThemedText type="smallBold" themeColor="background">
              Join
            </ThemedText>
          </Pressable>
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
  joinButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#0d6efd',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
});
