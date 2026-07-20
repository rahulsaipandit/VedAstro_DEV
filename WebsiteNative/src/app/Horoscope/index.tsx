import { useState } from 'react';
import { ScrollView, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { InfoBox } from '@/components/InfoBox';
import { PersonSelector } from '@/components/PersonSelector';
import { showErrorToast } from '@/lib/toast';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import type { Person } from '@/lib/api/person';

/**
 * Port of the person-selection half of Website/Pages/Calculator/Horoscope.razor. The advanced
 * options (ayanamsa, chart style) live on the results screen (src/app/Horoscope/[personId].tsx)
 * instead, since here they'd have nothing to apply to yet.
 */
export default function HoroscopeScreen() {
  const router = useRouter();
  const [person, setPerson] = useState<Person | null>(null);

  function handleCalculate() {
    if (!person) {
      showErrorToast('Pick a person to generate their horoscope.');
      return;
    }
    router.push(`/${PageRoute.Horoscope}/${person.id}` as never);
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Horoscope</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Insight into a person&apos;s character, nature and general future. Over +370 planetary
          combinations are used.
        </ThemedText>

        <PersonSelector label="Person" selectedPerson={person} onSelectPerson={setPerson} />

        <ThemedView style={styles.calculateButtonWrap}>
          <ThemedText
            type="smallBold"
            themeColor="background"
            onPress={handleCalculate}
            style={styles.calculateButton}>
            Calculate
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.infoRow}>
          <InfoBox
            icon="heart-plus"
            title="Ask AI Chat"
            description="Ask AI astrologer about your life aspects and horoscope"
          />
          <InfoBox
            icon="user"
            title="Forgotten Time"
            description="Use advanced computation to find your lost birth time"
          />
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
    marginBottom: Spacing.two,
  },
  calculateButtonWrap: {
    alignItems: 'flex-start',
  },
  calculateButton: {
    backgroundColor: '#0d6efd',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
    overflow: 'hidden',
  },
  infoRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
  },
});
