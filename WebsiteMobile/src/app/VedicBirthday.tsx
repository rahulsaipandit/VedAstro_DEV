import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Switch } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { BirthTimeInput, type BirthTimeInputValue } from '@/components/BirthTimeInput';
import { PersonSelector } from '@/components/PersonSelector';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { buildBirthTimeJsonFromWallClock, type BirthTimeJson } from '@/lib/time';
import { getTimezoneOffsetForLocation, type GeoLocation } from '@/lib/api/geo';
import { getVedicBirthDate, getLunarDay, type LunarDayInfo } from '@/lib/api/vedicBirthday';
import { showErrorToast } from '@/lib/toast';
import { Spacing } from '@/constants/theme';
import { useDefaultPerson } from '@/hooks/use-default-person';

const AccentColor = '#2F6FED';

const SEATTLE_LOCATION: GeoLocation = { name: 'Seattle', longitude: -122.3321, latitude: 47.6062 };

function blankBirthTime(): BirthTimeInputValue {
  return { dd: '', mm: '', yyyy: '', hh: '', min: '', location: SEATTLE_LOCATION };
}

type Result = {
  gregorianDate: string;
  tithi: LunarDayInfo;
};

/**
 * A person's "Vedic birthday" is the yearly recurrence of their birth tithi (lunar day) - the same
 * way a Hindu festival like Ramnavami is defined by tithi rather than a fixed Gregorian date, and
 * so lands on a different Gregorian date each year. This calls Calculate.VedicBirthDate for the
 * current calendar year, then Calculate.LunarDay on the matched date to show its tithi/paksha.
 * See docs/vedAstroArchitecture.md's "Vedic Birthday" section for the search algorithm.
 */
export default function VedicBirthdayScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const debugMode = useAppStore((s) => s.debugMode);
  const [useSavedProfile, setUseSavedProfile] = useState(false);
  const { person: selectedPerson, setPerson: setSelectedPerson } = useDefaultPerson();
  const [birthTime, setBirthTime] = useState<BirthTimeInputValue>(blankBirthTime);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<Result | null>(null);

  const currentYear = new Date().getFullYear();

  async function resolveBirthTime(): Promise<BirthTimeJson | null> {
    if (useSavedProfile) {
      if (!selectedPerson) {
        showErrorToast('Please select a saved person');
        return null;
      }
      return selectedPerson.birthTime;
    }

    if (!birthTime.dd || !birthTime.mm || !birthTime.yyyy || !birthTime.hh || !birthTime.min) {
      showErrorToast('Please fill in the full birth time');
      return null;
    }
    const offset = await getTimezoneOffsetForLocation(
      apiUrlDirect,
      birthTime.location,
      new Date(Date.UTC(Number(birthTime.yyyy), Number(birthTime.mm) - 1, Number(birthTime.dd)))
    );
    return buildBirthTimeJsonFromWallClock(
      birthTime.dd,
      birthTime.mm,
      birthTime.yyyy,
      birthTime.hh,
      birthTime.min,
      offset,
      birthTime.location
    );
  }

  async function handleCalculate() {
    setResult(null);
    setLoading(true);
    try {
      const time = await resolveBirthTime();
      if (!time) return;

      const matchedTime = await getVedicBirthDate(apiUrlDirect, time, currentYear);
      const tithi = await getLunarDay(apiUrlDirect, matchedTime);

      const match = /^\d{2}:\d{2} (\d{2})\/(\d{2})\/(\d{4})/.exec(matchedTime.StdTime);
      const gregorianDate = match ? `${match[1]}/${match[2]}/${match[3]}` : matchedTime.StdTime;

      setResult({ gregorianDate, tithi });
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Failed to calculate Vedic birth date');
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Vedic Birthday</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Just as Hindu festivals like Ramnavami fall on a different Gregorian date every year
          because they're defined by tithi (lunar day) rather than a fixed date, your own birth
          tithi recurs on a different Gregorian date each year too. Enter your birth time to find
          your Vedic birthday for {currentYear}.
        </ThemedText>

        <ThemedView style={styles.savedRow}>
          <ThemedText type="smallBold">Use saved profile</ThemedText>
          <Switch
            value={useSavedProfile}
            onValueChange={setUseSavedProfile}
            trackColor={{ true: AccentColor, false: theme.backgroundSelected }}
            thumbColor="#ffffff"
          />
        </ThemedView>

        {useSavedProfile ? (
          <PersonSelector label="Person" selectedPerson={selectedPerson} onSelectPerson={setSelectedPerson} />
        ) : (
          <BirthTimeInput
            apiUrlDirect={apiUrlDirect}
            value={birthTime}
            onChange={setBirthTime}
            defaultCountry={debugMode ? 'India' : 'United States'}
          />
        )}

        <Pressable onPress={handleCalculate} disabled={loading} style={styles.calculateButton}>
          {loading ? (
            <ActivityIndicator size="small" color="#ffffff" />
          ) : (
            <ThemedText type="smallBold" themeColor="background">
              Calculate
            </ThemedText>
          )}
        </Pressable>

        {result && (
          <ThemedView style={styles.resultCard}>
            <ThemedText type="small" themeColor="textSecondary">
              Your Vedic birthday in {currentYear} falls on
            </ThemedText>
            <ThemedText type="title" style={styles.resultDate}>
              {result.gregorianDate}
            </ThemedText>
            <ThemedText themeColor="textSecondary">
              {result.tithi.name} tithi, {result.tithi.paksha} Paksha
            </ThemedText>
          </ThemedView>
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
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    paddingBottom: Spacing.six,
    gap: Spacing.four,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  savedRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  calculateButton: {
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  resultCard: {
    borderRadius: 12,
    padding: Spacing.four,
    backgroundColor: '#00000010',
    gap: Spacing.one,
  },
  resultDate: {
    marginVertical: Spacing.one,
  },
});
