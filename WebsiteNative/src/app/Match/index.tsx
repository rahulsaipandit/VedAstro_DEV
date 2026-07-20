import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { InfoBox } from '@/components/InfoBox';
import { PersonSelector } from '@/components/PersonSelector';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';
import { confirm } from '@/lib/confirm';
import { showErrorToast } from '@/lib/toast';
import type { Person } from '@/lib/api/person';

/** Ported from Website/Pages/Calculator/Match/Index.razor. */
export default function MatchScreen() {
  const router = useRouter();
  const [male, setMale] = useState<Person | null>(null);
  const [female, setFemale] = useState<Person | null>(null);

  async function handleCalculate() {
    if (!male) {
      showErrorToast("How to check match if you don't select a person?");
      return;
    }
    if (!female) {
      showErrorToast('We need two people minimum to tango.');
      return;
    }
    if (male.id === female.id) {
      const proceed = await confirm(
        'Are you sure?',
        "You selected the same person for both. Seriously, what's the point of checking match then?"
      );
      if (!proceed) return;
    }
    if (male.gender === 'Female' && female.gender === 'Female') {
      const proceed = await confirm('Are you sure?', `${male.name} is Female but selected as Male!`);
      if (!proceed) return;
    }

    router.push(`/${PageRoute.MatchReport}/${male.id}/${female.id}` as never);
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Match Checker</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Check compatibility between two people. Easily predict your relationship's future.
        </ThemedText>

        <ThemedView style={styles.selectorRow}>
          <PersonSelector label="Male" selectedPerson={male} onSelectPerson={setMale} />
          <PersonSelector label="Female" selectedPerson={female} onSelectPerson={setFemale} />
        </ThemedView>

        <ThemedView style={styles.actionRow}>
          <Pressable onPress={handleCalculate} style={styles.calculateButton}>
            <ThemedText type="smallBold" themeColor="background">
              Calculate
            </ThemedText>
          </Pressable>
          <Pressable onPress={() => router.push(`/${PageRoute.SavedMatchReports}` as never)}>
            <ThemedText type="link" themeColor="textSecondary">
              View Saved Matches
            </ThemedText>
          </Pressable>
        </ThemedView>

        <ThemedView style={styles.infoRow}>
          <InfoBox
            icon="search"
            title="Find Perfect Match"
            description="Your soulmate is out there. Start a search in our global database."
          />
          <InfoBox
            icon="heart-plus"
            title="Full Check"
            description="16 astrological factors used to make this accurate prediction."
          />
        </ThemedView>

        <ThemedView style={styles.articleBlock}>
          <ThemedText type="subtitle">Marriage Karma</ThemedText>
          <ThemedText style={styles.articleText}>
            In each life the probability for marriage comes and goes multiple times. During the high period, your
            mind and environment around you will be geared towards romance and marriage. When this happens you will
            be attracted to people for partnership, even if they are not in-tune.
          </ThemedText>
          <ThemedText style={styles.articleText}>
            If this "marriage karma" lasted forever, there would be no problem. But in reality this period ends, so
            that next periods can come. When this happens, if your life partner is not in-tune with you, then comes
            the fights, broken hearts, and divorces.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.articleBlock}>
          <ThemedText type="subtitle">Imagine Perfect Marriages</ThemedText>
          <ThemedText style={styles.articleText}>
            Is it not high time for us as a human species to stop hunting for partners blindly based on our senses
            and circumstances, but rather use intelligence guided by cosmic laws that guarantee a perfect union.
          </ThemedText>
          <ThemedText style={styles.articleText}>
            Just imagine a world with no divorces, a world where happy marriages is a common sight.
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
    marginBottom: Spacing.two,
  },
  selectorRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.four,
  },
  actionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.four,
  },
  calculateButton: {
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  infoRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
  },
  articleBlock: {
    gap: Spacing.two,
  },
  articleText: {
    lineHeight: 22,
  },
});
