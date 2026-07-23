import { useEffect, useState } from 'react';
import { Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PersonSelector } from '@/components/PersonSelector';
import { EventsChartViewer } from '@/components/EventsChartViewer';
import { Dropdown } from '@/components/Dropdown';
import { Icon } from '@/components/Icon';
import { showErrorToast } from '@/lib/toast';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import {
  TIME_RANGE_PRESETS,
  computeDaysPerPixelForCustomRange,
  computeDaysPerPixelForPreset,
  type CustomRange,
  type TimeRangePreset,
} from '@/lib/api/eventsChart';
import {
  ALGORITHM_OPTIONS,
  EVENT_TAG_OPTIONS,
  GOOD_TIME_FINDER_DEFAULT_ALGORITHMS,
  GOOD_TIME_FINDER_DEFAULT_EVENT_TAGS,
  MONTH_OPTIONS,
} from '@/constants/eventsChartOptions';
import { AYANAMSA_GROUPS } from '@/constants/ayanamsa';
import type { Person } from '@/lib/api/person';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const PRESET_LABELS: Record<TimeRangePreset, string> = {
  FullLife: 'Full Life',
  '1year': '+1 Year',
  '3year': '+3 Years',
  '5year': '+5 Years',
  '10year': '+10 Years',
};

const MONTH_DROPDOWN_OPTIONS = MONTH_OPTIONS.map((m) => ({ label: m.label, value: String(m.value) }));

type RangeMode = 'preset' | 'custom';

type Calculated = {
  person: Person;
  preset: TimeRangePreset;
  customRange?: CustomRange;
  eventTagsCsv: string;
  algorithmNamesCsv: string;
  ayanamsaName: string;
  daysPerPixelOverride: number;
};

/**
 * Full-parity port of Website/Pages/Calculator/GoodTimeFinder.razor and
 * Website_Mobile/GoodTimeFinder.html+js/GoodTimeFinder.js — unlike the earlier "simplified port"
 * (see docs/vedAstroArchitecture.md's Known Migration Gaps #18), this restores the EventTag
 * checkbox tree (with GoodTimeFinder's own General/Personal default, not LifePredictor's PD1-PD7),
 * the Ayanamsa/Algorithm/Precision Advanced panel, and a Custom Year/Month range option — matching
 * MonthYearTimeRangeSelector.razor's granularity (month/year, not full calendar dates).
 */
export default function GoodTimeFinderScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const [person, setPerson] = useState<Person | null>(null);
  const [rangeMode, setRangeMode] = useState<RangeMode>('preset');
  const [preset, setPreset] = useState<TimeRangePreset>('1year');
  const currentYear = new Date().getFullYear();
  const [startYear, setStartYear] = useState(String(currentYear));
  const [startMonth, setStartMonth] = useState('1');
  const [endYear, setEndYear] = useState(String(currentYear));
  const [endMonth, setEndMonth] = useState('12');

  const [selectedTags, setSelectedTags] = useState<string[]>(GOOD_TIME_FINDER_DEFAULT_EVENT_TAGS);
  const [advancedOpen, setAdvancedOpen] = useState(false);
  const [selectedAlgorithms, setSelectedAlgorithms] = useState<string[]>(GOOD_TIME_FINDER_DEFAULT_ALGORITHMS);
  const [ayanamsaName, setAyanamsaName] = useState('Raman');
  const [precision, setPrecision] = useState('');

  const [calculated, setCalculated] = useState<Calculated | null>(null);

  const customRange: CustomRange = {
    startYear: parseInt(startYear, 10) || currentYear,
    startMonth: parseInt(startMonth, 10) || 1,
    endYear: parseInt(endYear, 10) || currentYear,
    endMonth: parseInt(endMonth, 10) || 12,
  };

  // Auto-fills Precision whenever the time range changes, same as SelectedTimeRangePreset's setter /
  // OnUpdateMonthYearSelection in GoodTimeFinder.razor — still freely editable afterwards.
  useEffect(() => {
    if (!person) return;
    const auto =
      rangeMode === 'custom' ? computeDaysPerPixelForCustomRange(customRange) : computeDaysPerPixelForPreset(person, preset);
    setPrecision(String(auto));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [person, preset, rangeMode, customRange.startYear, customRange.startMonth, customRange.endYear, customRange.endMonth]);

  function toggleTag(tag: string) {
    setSelectedTags((current) => (current.includes(tag) ? current.filter((t) => t !== tag) : [...current, tag]));
  }

  function toggleAlgorithm(algo: string) {
    setSelectedAlgorithms((current) => (current.includes(algo) ? current.filter((a) => a !== algo) : [...current, algo]));
  }

  function handleCalculate() {
    if (!person) {
      showErrorToast('Please select person, sir!');
      return;
    }
    if (selectedTags.length === 0) {
      showErrorToast('Select at least 1 Event Type. Without it what to calculate?');
      return;
    }
    if (selectedAlgorithms.length === 0) {
      showErrorToast('Select at least 1 Algorithm. If you don’t want coloring, check Neutral.');
      return;
    }
    if (rangeMode === 'custom' && (startYear.length !== 4 || endYear.length !== 4)) {
      showErrorToast('Start and End year must both be 4 digits.');
      return;
    }

    const daysPerPixelOverride = parseFloat(precision) || computeDaysPerPixelForPreset(person, preset);

    setCalculated({
      person,
      preset,
      customRange: rangeMode === 'custom' ? customRange : undefined,
      eventTagsCsv: selectedTags.join(','),
      algorithmNamesCsv: selectedAlgorithms.join(','),
      ayanamsaName,
      daysPerPixelOverride,
    });
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
                onPress={() => {
                  setRangeMode('preset');
                  setPreset(option);
                }}
                style={[styles.chip, rangeMode === 'preset' && preset === option && styles.chipActive]}>
                <ThemedText type="small" themeColor={rangeMode === 'preset' && preset === option ? 'background' : 'text'}>
                  {PRESET_LABELS[option]}
                </ThemedText>
              </Pressable>
            ))}
            <Pressable onPress={() => setRangeMode('custom')} style={[styles.chip, rangeMode === 'custom' && styles.chipActive]}>
              <ThemedText type="small" themeColor={rangeMode === 'custom' ? 'background' : 'text'}>
                Custom
              </ThemedText>
            </Pressable>
          </ThemedView>

          {rangeMode === 'custom' && (
            <ThemedView style={styles.customRangeRow}>
              <ThemedView style={styles.customRangeField}>
                <ThemedText type="small" themeColor="textSecondary">
                  Start
                </ThemedText>
                <ThemedView style={styles.customRangeInline}>
                  <TextInput
                    value={startYear}
                    onChangeText={(t) => setStartYear(t.replace(/[^0-9]/g, '').slice(0, 4))}
                    keyboardType="number-pad"
                    maxLength={4}
                    placeholder="Year"
                    placeholderTextColor={theme.textSecondary}
                    style={[styles.yearInput, { borderColor: theme.backgroundSelected, color: theme.text }]}
                  />
                  <Dropdown value={startMonth} options={MONTH_DROPDOWN_OPTIONS} onChange={setStartMonth} placeholder="Month" />
                </ThemedView>
              </ThemedView>
              <ThemedView style={styles.customRangeField}>
                <ThemedText type="small" themeColor="textSecondary">
                  End
                </ThemedText>
                <ThemedView style={styles.customRangeInline}>
                  <TextInput
                    value={endYear}
                    onChangeText={(t) => setEndYear(t.replace(/[^0-9]/g, '').slice(0, 4))}
                    keyboardType="number-pad"
                    maxLength={4}
                    placeholder="Year"
                    placeholderTextColor={theme.textSecondary}
                    style={[styles.yearInput, { borderColor: theme.backgroundSelected, color: theme.text }]}
                  />
                  <Dropdown value={endMonth} options={MONTH_DROPDOWN_OPTIONS} onChange={setEndMonth} placeholder="Month" />
                </ThemedView>
              </ThemedView>
            </ThemedView>
          )}
        </ThemedView>

        <ThemedView style={styles.presetRow}>
          <ThemedText type="small" themeColor="textSecondary">
            Event Type
          </ThemedText>
          <ThemedView style={styles.chipRow}>
            {EVENT_TAG_OPTIONS.map((tag) => (
              <Pressable
                key={tag.value}
                onPress={() => toggleTag(tag.value)}
                style={[styles.chip, selectedTags.includes(tag.value) && styles.chipActive]}>
                <ThemedText type="small" themeColor={selectedTags.includes(tag.value) ? 'background' : 'text'}>
                  {tag.label}
                  {tag.comingSoon ? ' (soon)' : ''}
                </ThemedText>
              </Pressable>
            ))}
          </ThemedView>
        </ThemedView>

        <Pressable onPress={() => setAdvancedOpen((v) => !v)} style={styles.advancedToggle}>
          <Icon name={advancedOpen ? 'chevron-up' : 'settings'} size={16} color={theme.textSecondary} />
          <ThemedText type="small" themeColor="textSecondary">
            Advanced (optional)
          </ThemedText>
        </Pressable>

        {advancedOpen && (
          <ThemedView style={[styles.advancedPanel, { borderColor: theme.backgroundSelected }]}>
            <ThemedView style={styles.presetRow}>
              <ThemedText type="small" themeColor="textSecondary">
                Ayanamsa
              </ThemedText>
              <Dropdown value={ayanamsaName} groups={AYANAMSA_GROUPS} onChange={setAyanamsaName} placeholder="Ayanamsa" />
            </ThemedView>

            <ThemedView style={styles.presetRow}>
              <ThemedText type="small" themeColor="textSecondary">
                Precision (days per pixel)
              </ThemedText>
              <TextInput
                value={precision}
                onChangeText={setPrecision}
                keyboardType="decimal-pad"
                style={[styles.precisionInput, { borderColor: theme.backgroundSelected, color: theme.text }]}
              />
            </ThemedView>

            <ThemedView style={styles.presetRow}>
              <ThemedText type="small" themeColor="textSecondary">
                Algorithms
              </ThemedText>
              <ThemedView style={styles.chipRow}>
                {ALGORITHM_OPTIONS.map((algo) => (
                  <Pressable
                    key={algo}
                    onPress={() => toggleAlgorithm(algo)}
                    style={[styles.chip, selectedAlgorithms.includes(algo) && styles.chipActive]}>
                    <ThemedText type="small" themeColor={selectedAlgorithms.includes(algo) ? 'background' : 'text'}>
                      {algo}
                    </ThemedText>
                  </Pressable>
                ))}
              </ThemedView>
            </ThemedView>
          </ThemedView>
        )}

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
            customRange={calculated.customRange}
            eventTagsCsv={calculated.eventTagsCsv}
            algorithmNamesCsv={calculated.algorithmNamesCsv}
            ayanamsaName={calculated.ayanamsaName}
            daysPerPixelOverride={calculated.daysPerPixelOverride}
          />
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
    backgroundColor: '#0d6efd',
  },
  customRangeRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
    marginTop: Spacing.one,
  },
  customRangeField: {
    gap: Spacing.one,
  },
  customRangeInline: {
    flexDirection: 'row',
    gap: Spacing.two,
    alignItems: 'center',
  },
  yearInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    minWidth: 80,
  },
  precisionInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    minWidth: 100,
    alignSelf: 'flex-start',
  },
  advancedToggle: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.one,
    alignSelf: 'flex-start',
  },
  advancedPanel: {
    borderWidth: 1,
    borderRadius: 8,
    padding: Spacing.three,
    gap: Spacing.three,
  },
  calculateButton: {
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
});
