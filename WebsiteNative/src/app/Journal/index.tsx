import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet } from 'react-native';
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
 * Port of Website/Pages/Journal/Index.razor. Backed by real persistence now — see
 * migration.md: Person.ToAzureRow() never carried LifeEventList (a separate table), so
 * /api/UpdatePerson silently discarded every journal edit until this session's fix
 * (API/FrontDesk/PersonAPI.cs's SyncLifeEvents).
 */
export default function JournalScreen() {
  const router = useRouter();
  const [person, setPerson] = useState<Person | null>(null);

  function handleShow() {
    if (!person) {
      showErrorToast('Pick a person to view their journal.');
      return;
    }
    router.push(`/${PageRoute.Journal}/${person.id}` as never);
  }

  function handleAdd() {
    if (!person) {
      showErrorToast('Pick a person to add an event for.');
      return;
    }
    const newId = `evt${Date.now()}`;
    router.push(`/${PageRoute.JournalEditor}/${person.id}/${newId}` as never);
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Astro Journal</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Record your life events to understand the astrological reason behind them.
        </ThemedText>

        <PersonSelector label="Person" selectedPerson={person} onSelectPerson={setPerson} />

        <ThemedView style={styles.buttonRow}>
          <Pressable onPress={handleShow} style={styles.showButton}>
            <ThemedText type="smallBold">Show Journal</ThemedText>
          </Pressable>
          <Pressable onPress={handleAdd} style={styles.addButton}>
            <ThemedText type="smallBold" themeColor="background">
              Add Event
            </ThemedText>
          </Pressable>
        </ThemedView>

        <InfoBox
          icon="user"
          title="Quick Tip"
          description="After updating your journal, go to Life Predictor to view your events with astrological influence."
        />
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
  buttonRow: {
    flexDirection: 'row',
    gap: Spacing.three,
  },
  showButton: {
    backgroundColor: '#00000010',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  addButton: {
    backgroundColor: '#0d6efd',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
});
