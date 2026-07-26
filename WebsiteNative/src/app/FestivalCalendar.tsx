import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { GeoLocationInput } from '@/components/GeoLocationInput';
import { MoonPhaseIcon } from '@/components/MoonPhaseIcon';
import { PanchangDetailSheet } from '@/components/PanchangDetailSheet';
import { Icon } from '@/components/Icon';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { showErrorToast } from '@/lib/toast';
import type { BirthTimeJson } from '@/lib/time';
import { getFestivalCalendar, FESTIVAL_NAMES, type FestivalCalendarResult, type FestivalName } from '@/lib/api/festivalCalendar';
import type { GeoLocation } from '@/lib/api/geo';
import { Spacing } from '@/constants/theme';

const FESTIVAL_LABELS: Record<FestivalName, string> = {
  Ramnavami: 'Ramnavami',
  Holi: 'Holi',
  Rakshabandhan: 'Raksha Bandhan',
  KarwaChauth: 'Karwa Chauth',
  Chhath: 'Chhath',
  Diwali: 'Diwali (Deepawali)',
  MahaShivaratri: 'Maha Shivaratri',
  MakarSankranti: 'Makar Sankranti',
};

// Matches the tithi each festival is computed against in Calculate.FestivalDate
// (Library/Logic/Calculate/FestivalCalendar.cs) - undefined for Makar Sankranti, a purely solar
// event with no tithi/moon-phase of its own.
const FESTIVAL_TITHI: Partial<Record<FestivalName, number>> = {
  Ramnavami: 9,
  Holi: 16,
  Rakshabandhan: 15,
  KarwaChauth: 19,
  Chhath: 6,
  Diwali: 30,
  MahaShivaratri: 29,
};

const SEATTLE_LOCATION: GeoLocation = { name: 'Seattle', longitude: -122.3321, latitude: 47.6062 };

function formatDate(time: BirthTimeJson): string {
  const match = /(\d{2})\/(\d{2})\/(\d{4})/.exec(time.StdTime);
  return match ? `${match[1]}/${match[2]}/${match[3]}` : time.StdTime;
}

function dateSortKey(time: BirthTimeJson): number {
  const match = /^(\d{2}):(\d{2}) (\d{2})\/(\d{2})\/(\d{4})/.exec(time.StdTime);
  if (!match) return 0;
  const [, hh, mm, dd, mo, yyyy] = match;
  return Date.UTC(Number(yyyy), Number(mo) - 1, Number(dd), Number(hh), Number(mm));
}

/**
 * Full Hindu festival calendar for a year, computed fresh via Calculate.FestivalCalendar
 * (Library/Logic/Calculate/FestivalCalendar.cs) rather than a static lookup table - each
 * festival's date shifts year to year the same way a person's Vedic birthday does (see
 * VedicBirthday.tsx). Tapping a festival opens its day-detail Panchang (Choghadiya or
 * traditional Panchang, user's choice) via PanchangDetailSheet.
 */
export default function FestivalCalendarScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const currentYear = new Date().getFullYear();

  const [year, setYear] = useState(String(currentYear));
  const [location, setLocation] = useState<GeoLocation>(SEATTLE_LOCATION);
  const [loading, setLoading] = useState(false);
  const [calendar, setCalendar] = useState<FestivalCalendarResult | null>(null);
  const [selected, setSelected] = useState<{ label: string; date: BirthTimeJson } | null>(null);

  async function handleCalculate() {
    const parsedYear = parseInt(year, 10);
    if (!parsedYear || year.length !== 4) {
      showErrorToast('Please enter a valid 4-digit year');
      return;
    }
    setLoading(true);
    setCalendar(null);
    try {
      const result = await getFestivalCalendar(apiUrlDirect, parsedYear, location);
      setCalendar(result);
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Failed to calculate festival calendar');
    } finally {
      setLoading(false);
    }
  }

  const sortedFestivals = calendar
    ? FESTIVAL_NAMES.filter((name) => calendar[name]).sort((a, b) => dateSortKey(calendar[a]!) - dateSortKey(calendar[b]!))
    : [];

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Festival Calendar</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Hindu festival dates are computed fresh each year from tithi (lunar day) and lunar month
          rules - not read from a fixed table - so they fall on a different Gregorian date every
          year. Tap a festival to see its day's Panchang.
        </ThemedText>

        <ThemedView style={styles.fieldGroup}>
          <ThemedText style={styles.fieldLabel}>Year</ThemedText>
          <TextInput
            value={year}
            onChangeText={(t) => setYear(t.replace(/[^0-9]/g, '').slice(0, 4))}
            keyboardType="number-pad"
            maxLength={4}
            placeholderTextColor={theme.textSecondary}
            style={[styles.yearInput, { borderColor: theme.backgroundSelected, color: theme.text }]}
          />
        </ThemedView>

        <GeoLocationInput apiUrlDirect={apiUrlDirect} location={location} onChange={setLocation} label="Location" />

        <Pressable onPress={handleCalculate} disabled={loading} style={styles.calculateButton}>
          {loading ? (
            <ActivityIndicator size="small" color="#ffffff" />
          ) : (
            <ThemedText type="smallBold" themeColor="background">
              Calculate
            </ThemedText>
          )}
        </Pressable>

        {sortedFestivals.length > 0 && (
          <ThemedView style={styles.list}>
            {sortedFestivals.map((name) => {
              const date = calendar![name]!;
              const tithi = FESTIVAL_TITHI[name];
              return (
                <Pressable
                  key={name}
                  onPress={() => setSelected({ label: FESTIVAL_LABELS[name], date })}
                  style={[styles.festivalRow, { borderColor: theme.backgroundSelected }]}>
                  {tithi ? <MoonPhaseIcon tithi={tithi} size={28} /> : <Icon name="sparkles" size={22} color={theme.textSecondary} />}
                  <ThemedView style={styles.festivalText}>
                    <ThemedText type="smallBold">{FESTIVAL_LABELS[name]}</ThemedText>
                    <ThemedText type="small" themeColor="textSecondary">
                      {formatDate(date)}
                    </ThemedText>
                  </ThemedView>
                  <Icon name="chevron-right" size={16} color={theme.textSecondary} />
                </Pressable>
              );
            })}
          </ThemedView>
        )}
      </ThemedView>

      <PanchangDetailSheet
        visible={!!selected}
        onClose={() => setSelected(null)}
        apiUrlDirect={apiUrlDirect}
        date={selected?.date ?? null}
        title={selected?.label ?? ''}
      />
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
  fieldGroup: {
    gap: Spacing.one,
  },
  fieldLabel: {
    fontSize: 13,
    fontWeight: '600',
  },
  yearInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    minWidth: 100,
    alignSelf: 'flex-start',
  },
  calculateButton: {
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  list: {
    gap: Spacing.two,
  },
  festivalRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.three,
    borderWidth: 1,
    borderRadius: 12,
    padding: Spacing.three,
  },
  festivalText: {
    flex: 1,
    gap: 2,
  },
});
