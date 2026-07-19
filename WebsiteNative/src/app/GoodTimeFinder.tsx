import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PersonSelector } from '@/components/PersonSelector';
import { EventsChartViewer } from '@/components/EventsChartViewer';
import { showErrorToast } from '@/lib/toast';
import { useAppStore } from '@/store/useAppStore';
import { TIME_RANGE_PRESETS, type TimeRangePreset } from '@/lib/api/eventsChart';
import type { Person } from '@/lib/api/person';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const PRESET_LABELS: Record<TimeRangePreset, string> = {
  FullLife: 'Full Life',
  '1year': '+1 Year',
  '3year': '+3 Years',
  '5year': '+5 Years',
  '10year': '+10 Years',
};

/**
 * Simplified port of Website/Pages/Calculator/GoodTimeFinder.razor. Same underlying mechanism as
 * LifePredictor (both call Calculate.AutoCalculateTimeRange + EventsChartViewer) — the original's
 * event-type checkboxes (General/Personal/Career/etc.), custom year range, and algorithm picker
 * aren't ported, same bar as LifePredictor's deferred options (see migration.md).
 */
export default function GoodTimeFinderScreen() {
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const [person, setPerson] = useState<Person | null>(null);
  const [preset, setPreset] = useState<TimeRangePreset>('1year');
  const [calculated, setCalculated] = useState<{ person: Person; preset: TimeRangePreset } | null>(null);

  function handleCalculate() {
    if (!person) {
      showErrorToast('Pick a person to find good times for.');
      return;
    }
    setCalculated({ person, preset });
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Good Time Finder</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Find the right time for wedding, job interview, buying house and etc. Muhurtha is
          sanskrit for Good Time or Electional Astrology.
        </ThemedText>

        <PersonSelector label="Person" selectedPerson={person} onSelectPerson={setPerson} />

        <ThemedView style={styles.presetRow}>
          <ThemedText type="small" themeColor="textSecondary">
            Time Range
          </ThemedText>
          <ThemedView style={styles.chipRow}>
            {TIME_RANGE_PRESETS.map((option) => (
              <Pressable
                key={option}
                onPress={() => setPreset(option)}
                style={[styles.chip, preset === option && styles.chipActive]}>
                <ThemedText type="small" themeColor={preset === option ? 'background' : 'text'}>
                  {PRESET_LABELS[option]}
                </ThemedText>
              </Pressable>
            ))}
          </ThemedView>
        </ThemedView>

        <Pressable onPress={handleCalculate} style={styles.calculateButton}>
          <ThemedText type="smallBold" themeColor="background">
            Calculate
          </ThemedText>
        </Pressable>

        {calculated && (
          <EventsChartViewer apiUrlDirect={apiUrlDirect} person={calculated.person} preset={calculated.preset} />
        )}
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
  presetRow: {
    gap: Spacing.one,
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.one,
  },
  chip: {
    borderRadius: 999,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.one,
    backgroundColor: '#00000010',
  },
  chipActive: {
    backgroundColor: '#1a9c4c',
  },
  calculateButton: {
    backgroundColor: '#1a9c4c',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
});
