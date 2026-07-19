import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PersonSelector } from '@/components/PersonSelector';
import { showErrorToast } from '@/lib/toast';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import type { Person } from '@/lib/api/person';

const CALCULATORS = [
  { value: 'Horoscope', label: 'Horoscope' },
  { value: 'LifePredictor', label: 'Life Predictor' },
  { value: 'Match', label: 'Match Checker' },
  { value: 'GoodTimeFinder', label: 'Good Time Finder' },
] as const;

/** Port of Website/Pages/Docs/QuickGuide.razor. */
export default function QuickGuideScreen() {
  const router = useRouter();
  const [person, setPerson] = useState<Person | null>(null);
  const [calculator, setCalculator] = useState<(typeof CALCULATORS)[number]['value'] | null>(null);

  function handleGo() {
    if (!calculator) {
      showErrorToast('Select a calculator!');
      return;
    }
    const personId = person && person.id !== '101' ? person.id : '';
    switch (calculator) {
      case 'Horoscope':
        router.push(`/${PageRoute.Horoscope}${personId ? `/${personId}` : ''}` as never);
        break;
      case 'LifePredictor':
        router.push(`/${PageRoute.LifePredictor}` as never);
        break;
      case 'Match':
        router.push(`/${PageRoute.Match}` as never);
        break;
      case 'GoodTimeFinder':
        router.push(`/${PageRoute.GoodTimeFinder}` as never);
        break;
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Quick Guide</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          This is a web app to help you use Vedic astrology. It is designed to work like an app
          running in the cloud.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">How To Use?</ThemedText>
          <ThemedView style={styles.step}>
            <ThemedText type="smallBold">STEP 1 — Choose or add a person&apos;s horoscope</ThemedText>
            <PersonSelector label="Person" selectedPerson={person} onSelectPerson={setPerson} />
          </ThemedView>
          <ThemedView style={styles.step}>
            <ThemedText type="smallBold">STEP 2 — Choose calculator to use</ThemedText>
            <ThemedView style={styles.chipRow}>
              {CALCULATORS.map((option) => (
                <Pressable
                  key={option.value}
                  onPress={() => setCalculator(option.value)}
                  style={[styles.chip, calculator === option.value && styles.chipActive]}>
                  <ThemedText type="small" themeColor={calculator === option.value ? 'background' : 'text'}>
                    {option.label}
                  </ThemedText>
                </Pressable>
              ))}
            </ThemedView>
          </ThemedView>
          <ThemedView style={styles.step}>
            <ThemedText type="smallBold">
              STEP 3 — Click calculate to view predictions. Easy!
            </ThemedText>
            <Pressable onPress={handleGo} style={styles.goButton}>
              <ThemedText type="smallBold" themeColor="background">
                Goto Calculator
              </ThemedText>
            </Pressable>
          </ThemedView>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">What is a calculator?</ThemedText>
          <ThemedText style={styles.paragraph}>
            Each calculator shows an aspect of astrology. For example Match is a calculator for
            checking marriage or friendship compatibility. Life Predictor can help predict major
            life events decades ahead, and using Good Time Finder you can find the perfect time
            for important occasions like marriage, job interviews or starting a project.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Unexpected errors?</ThemedText>
          <ThemedText style={styles.paragraph}>
            Reload the app. This clears the old cached copy and solves most errors — VedAstro gets
            new updates almost every day, so it&apos;s important you&apos;re using the latest
            version.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Who is this for?</ThemedText>
          <ThemedText style={styles.paragraph}>
            Both an average person and an expert astrologer can find VedAstro useful. These
            calculators are constantly being improved, with the goal of making all tools easily
            usable by anybody.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Must I login to use?</ThemedText>
          <ThemedText style={styles.paragraph}>
            No. All tools are available without login. But if you don&apos;t login, we can&apos;t
            save the data you add — for example, a Person profile will only be available next
            time if you&apos;re signed in. We don&apos;t collect or connect to any data from your
            Google or Facebook account; it&apos;s only used to authenticate your access.
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
    gap: Spacing.five,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  section: {
    gap: Spacing.three,
  },
  step: {
    gap: Spacing.two,
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.one,
  },
  chip: {
    borderRadius: 999,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    backgroundColor: '#00000010',
  },
  chipActive: {
    backgroundColor: '#1a9c4c',
  },
  goButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
  paragraph: {
    lineHeight: 22,
  },
});
