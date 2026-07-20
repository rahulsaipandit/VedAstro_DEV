import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PersonSelector } from '@/components/PersonSelector';
import { BirthTimeFinderViewer } from '@/components/BirthTimeFinderViewer';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { showErrorToast } from '@/lib/toast';
import type { BirthTimeFinderOptions } from '@/lib/api/birthTimeFinder';
import type { Person } from '@/lib/api/person';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Port of Website/Pages/Calculator/BirthTimeFinder.razor (previously a stub — see
 * migration.md) — backed by API/FrontDesk/BirthTimeFinderAPI.cs, the RN equivalent of the
 * Console app's "Find Birth Time - Life Predictor - Person" tool (Console/Program.cs). Scans
 * a range of hours on the person's recorded birth day at a given precision, generating a life
 * events chart for each candidate time so the closest match to remembered life events can be
 * picked out visually. Simplified vs. the Console tool: no custom year-range/algorithm picker,
 * server defaults to full life (birth -> +100 years) and the General/IshtaKashtaPhalaDegree/
 * PlanetStrengthDegree algorithm set.
 */
export default function BirthTimeFinderScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const [person, setPerson] = useState<Person | null>(null);
  const [precisionInHours, setPrecisionInHours] = useState('1');
  const [startHour, setStartHour] = useState('00:00');
  const [endHour, setEndHour] = useState('23:59');
  const [calculated, setCalculated] = useState<{ personId: string; options: BirthTimeFinderOptions } | null>(null);

  function handleCalculate() {
    if (!person) {
      showErrorToast('Pick a person to scan possible birth times for.');
      return;
    }
    const precision = parseFloat(precisionInHours);
    if (!precision || precision <= 0) {
      showErrorToast('Scan precision must be a positive number of hours.');
      return;
    }
    if (!/^\d{2}:\d{2}$/.test(startHour) || !/^\d{2}:\d{2}$/.test(endHour)) {
      showErrorToast('Start/end hour must be in HH:mm format, e.g. 06:00.');
      return;
    }

    setCalculated({
      personId: person.id,
      options: { precisionInHours: precision, startHour, endHour },
    });
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Birth Time Finder</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Find a forgotten or uncertain birth time by generating a life-events chart for every
          candidate time in an hour range, then comparing them against remembered life events -
          a "dictionary attack" on time.
        </ThemedText>

        <PersonSelector label="Person" selectedPerson={person} onSelectPerson={setPerson} />

        <ThemedView style={styles.row}>
          <ThemedView style={[styles.inputBlock, { borderColor: theme.backgroundSelected }]}>
            <ThemedText type="small" themeColor="textSecondary">
              Start Hour (HH:mm)
            </ThemedText>
            <TextInput
              value={startHour}
              onChangeText={setStartHour}
              placeholder="00:00"
              placeholderTextColor={theme.textSecondary}
              style={[styles.input, { color: theme.text }]}
            />
          </ThemedView>
          <ThemedView style={[styles.inputBlock, { borderColor: theme.backgroundSelected }]}>
            <ThemedText type="small" themeColor="textSecondary">
              End Hour (HH:mm)
            </ThemedText>
            <TextInput
              value={endHour}
              onChangeText={setEndHour}
              placeholder="23:59"
              placeholderTextColor={theme.textSecondary}
              style={[styles.input, { color: theme.text }]}
            />
          </ThemedView>
          <ThemedView style={[styles.inputBlock, { borderColor: theme.backgroundSelected }]}>
            <ThemedText type="small" themeColor="textSecondary">
              Precision (hours)
            </ThemedText>
            <TextInput
              value={precisionInHours}
              onChangeText={setPrecisionInHours}
              keyboardType="numeric"
              placeholder="1"
              placeholderTextColor={theme.textSecondary}
              style={[styles.input, { color: theme.text }]}
            />
          </ThemedView>
        </ThemedView>

        <Pressable onPress={handleCalculate} style={styles.calculateButton}>
          <ThemedText type="smallBold" themeColor="background">
            Scan Possible Birth Times
          </ThemedText>
        </Pressable>

        {calculated && (
          <BirthTimeFinderViewer apiUrlDirect={apiUrlDirect} personId={calculated.personId} options={calculated.options} />
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
  row: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
  },
  inputBlock: {
    flexGrow: 1,
    minWidth: 120,
    gap: Spacing.one,
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
  },
  input: {
    paddingVertical: Spacing.one,
  },
  calculateButton: {
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
});
