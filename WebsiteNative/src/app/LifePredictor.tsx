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
import { Spacing } from '@/constants/theme';

const PRESET_LABELS: Record<TimeRangePreset, string> = {
  FullLife: 'Full Life',
  '1year': '+1 Year',
  '3year': '+3 Years',
  '5year': '+5 Years',
  '10year': '+10 Years',
};

/**
 * Simplified port of Website/Pages/Calculator/LifePredictor.razor. The original's algorithm
 * checkboxes, Dasa-level event checkboxes, custom year/age range, and saved-chart selector are
 * NOT ported — this uses the same fixed defaults the original page shipped with (General
 * algorithm; PD1-PD7 dasa levels), letting the person + time range choice drive the chart. See
 * migration.md for what's deferred and why.
 *
 * These are passed explicitly (not left to EventsChartViewer's own defaults) so this screen can
 * never silently converge on the same chart content as GoodTimeFinder again — see
 * docs/vedAstroArchitecture.md's Known Migration Gaps #18 for the regression this fixes.
 */
const LIFE_PREDICTOR_EVENT_TAGS = 'PD1,PD2,PD3,PD4,PD5,PD6,PD7';
const LIFE_PREDICTOR_ALGORITHMS = 'General';

export default function LifePredictorScreen() {
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const [person, setPerson] = useState<Person | null>(null);
  const [preset, setPreset] = useState<TimeRangePreset>('FullLife');
  const [calculated, setCalculated] = useState<{ person: Person; preset: TimeRangePreset } | null>(null);

  function handleCalculate() {
    if (!person) {
      showErrorToast('Pick a person to predict their life periods.');
      return;
    }
    setCalculated({ person, preset });
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Life Predictor</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Each life is a story. Computation algorithms fused with ancient astrology predicts this
          story. Know good and bad periods of your life years ahead.
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
          <EventsChartViewer
            apiUrlDirect={apiUrlDirect}
            person={calculated.person}
            preset={calculated.preset}
            eventTagsCsv={LIFE_PREDICTOR_EVENT_TAGS}
            algorithmNamesCsv={LIFE_PREDICTOR_ALGORITHMS}
          />
        )}

        <ThemedView style={styles.articleBlock}>
          <ThemedText type="subtitle">Colors</ThemedText>
          <ThemedText style={styles.paragraph}>
            Our eyes and brain can process and understand colors much more easily than text. RED is
            for bad life events, GREEN is for good — quickly readable with zero astrological
            knowledge.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.articleBlock}>
          <ThemedText type="subtitle">Tis Fate Then?</ThemedText>
          <ThemedText style={styles.paragraph}>
            No! This is astrological weather forecast, that is all. You cannot choose when
            thunderstorms come, but you can choose to sit at home dry or get wet outside. Don&apos;t
            take risks during RED periods, and make good use of GREEN ones.
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
    backgroundColor: '#0d6efd',
  },
  calculateButton: {
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  articleBlock: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
});
