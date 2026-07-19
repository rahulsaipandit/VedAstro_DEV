import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { InfoBox } from '@/components/InfoBox';
import { PersonSelector } from '@/components/PersonSelector';
import { useAppStore } from '@/store/useAppStore';
import { findMatchesForPerson, type PersonKutaScore } from '@/lib/api/match';
import type { Person } from '@/lib/api/person';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Port of Website/Pages/Calculator/Match/Finder.razor. The original's alternate "no login"
 * flow (email-capture via a SweetAlert2 popup -> XML AddMessageApi) was already dead/unused
 * code there (superseded by the real FindMatch call, kept only as `OnClickSearchButtonOLD`)
 * — not carried over, same as UnderConstructionNotice's dropped email capture.
 */
export default function MatchFinderScreen() {
  const router = useRouter();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const [person, setPerson] = useState<Person | null>(null);
  const [results, setResults] = useState<PersonKutaScore[] | null>(null);
  const [searching, setSearching] = useState(false);

  async function handleSearch() {
    if (!person) return;
    setSearching(true);
    try {
      setResults(await findMatchesForPerson(apiUrlDirect, person.id));
    } finally {
      setSearching(false);
    }
  }

  function handleViewReport(otherPersonId: string) {
    if (!person) return;
    const url =
      person.gender === 'Male'
        ? `/${PageRoute.MatchReport}/${person.id}/${otherPersonId}`
        : `/${PageRoute.MatchReport}/${otherPersonId}/${person.id}`;
    router.push(url as never);
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Match Finder</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Find your astrologically perfect match. Matching algorithm will find for your soulmate
          in our world wide database.
        </ThemedText>

        <ThemedView style={styles.selectorRow}>
          <PersonSelector label="Person" selectedPerson={person} onSelectPerson={setPerson} />
          <Pressable onPress={handleSearch} disabled={!person || searching} style={styles.searchButton}>
            {searching ? (
              <ActivityIndicator size="small" color="#ffffff" />
            ) : (
              <ThemedText type="smallBold" themeColor="background">
                Search
              </ThemedText>
            )}
          </Pressable>
        </ThemedView>

        <ThemedView style={styles.infoRow}>
          <InfoBox
            icon="heart-plus"
            title="Love Letter"
            description="Leave a love letter for your soulmate when they find you"
          />
          <InfoBox
            icon="cards-heart"
            title="Cute Pic"
            description="Let your soulmate see how you look when you send friend request"
          />
        </ThemedView>

        {results && (
          <ThemedView style={styles.section}>
            <ThemedText type="subtitle">Found Match</ThemedText>
            {results.length === 0 ? (
              <ThemedText themeColor="textSecondary">No matches found yet — check back soon.</ThemedText>
            ) : (
              <ThemedView style={styles.list}>
                {results.map((result) => (
                  <ThemedView key={result.personId} style={styles.row} type="backgroundElement">
                    <ThemedView style={styles.rowInfo}>
                      <ThemedText type="smallBold">{result.personName}</ThemedText>
                      <ThemedText type="small" themeColor="textSecondary">
                        {result.gender} · Age {result.age}
                      </ThemedText>
                    </ThemedView>
                    <ThemedText type="smallBold" style={{ color: '#1a9c4c' }}>
                      {Math.round(result.kutaScore)}%
                    </ThemedText>
                    <Pressable onPress={() => handleViewReport(result.personId)} style={styles.viewButton}>
                      <ThemedText type="smallBold" themeColor="background">
                        View
                      </ThemedText>
                    </Pressable>
                  </ThemedView>
                ))}
              </ThemedView>
            )}
          </ThemedView>
        )}

        <ThemedView style={styles.articleBlock}>
          <ThemedText type="subtitle">How it works?</ThemedText>
          <ThemedText style={styles.paragraph}>
            After you click &quot;Search&quot;, our servers will start crunching numbers, each
            profile in the database is scrutinized from every available Vedic astrological aspect
            to see if they match you perfectly. This process takes time, so sit back and relax.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.articleBlock}>
          <ThemedText type="subtitle">No match?</ThemedText>
          <ThemedText style={styles.paragraph}>
            Fear not, this is normal. Our global database is growing everyday, just because we
            could not find your match today, it does not mean we won&apos;t find out tomorrow.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.articleBlock}>
          <ThemedText type="subtitle">Privacy</ThemedText>
          <ThemedText style={styles.paragraph}>
            We take privacy seriously. Even though the project is open-source the database is
            locked and only accessible to the algorithm. None of the profiles submitted for match
            checking is shown publicly.
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
  selectorRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'flex-end',
    gap: Spacing.three,
  },
  searchButton: {
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  infoRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
  },
  section: {
    gap: Spacing.two,
  },
  list: {
    gap: Spacing.two,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.three,
  },
  rowInfo: {
    flex: 1,
    gap: Spacing.half,
  },
  viewButton: {
    backgroundColor: '#1a9c4c',
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.one,
  },
  articleBlock: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
});
