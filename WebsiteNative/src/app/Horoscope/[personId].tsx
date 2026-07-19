import { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { useLocalSearchParams } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { SkyChartViewer } from '@/components/SkyChartViewer';
import { IndianChart } from '@/components/IndianChart';
import { PlanetDataTable } from '@/components/PlanetDataTable';
import { HouseDataTable } from '@/components/HouseDataTable';
import { AshtakvargaTable } from '@/components/AshtakvargaTable';
import { HoroscopeReferenceList } from '@/components/HoroscopeReferenceList';
import { StrengthChart } from '@/components/StrengthChart';
import { useAppStore } from '@/store/useAppStore';
import { getPersonList, getPublicPersonList, type Person } from '@/lib/api/person';
import {
  DEFAULT_AYANAMSA,
  getBhinnashtakavargaChart,
  getHoroscopePredictions,
  getHouseTable,
  getPlanetTable,
  getSarvashtakavargaChart,
  type AshtakvargaRow,
  type HoroscopePrediction,
  type HouseTableRow,
  type PlanetTableRow,
} from '@/lib/api/horoscope';
import { loadCalculationPreferences } from '@/lib/preferences';
import { Spacing, MaxContentWidth } from '@/constants/theme';

const AYANAMSA_OPTIONS = ['LahiriChitrapaksha', 'Raman', 'KrishnamurtiKP', 'FaganBradley', 'Yukteshwar', 'J2000'];

/** Port of Website/Pages/Calculator/Horoscope.razor's output side (after a person is selected). */
export default function HoroscopeResultScreen() {
  const { personId } = useLocalSearchParams<{ personId: string }>();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());
  const visitorId = useAppStore((s) => s.visitorId);

  const [person, setPerson] = useState<Person | null>(null);
  const [personError, setPersonError] = useState<string | null>(null);
  const [personLoading, setPersonLoading] = useState(true);

  const [ayanamsa, setAyanamsa] = useState(DEFAULT_AYANAMSA);
  const [chartStyle, setChartStyle] = useState<'South' | 'North'>('South');

  // Seeds from the same "Advanced Options" preference set on the Add/Edit Person screens (see
  // src/lib/preferences.ts) so a calculation-method choice made there actually has an effect
  // somewhere, rather than only being saved and never read.
  useEffect(() => {
    loadCalculationPreferences().then((prefs) => setAyanamsa(prefs.ayanamsa));
  }, []);

  const [predictions, setPredictions] = useState<HoroscopePrediction[]>([]);
  const [planetRows, setPlanetRows] = useState<PlanetTableRow[]>([]);
  const [houseRows, setHouseRows] = useState<HouseTableRow[]>([]);
  const [sarvaRows, setSarvaRows] = useState<AshtakvargaRow[]>([]);
  const [bhinnaRows, setBhinnaRows] = useState<AshtakvargaRow[]>([]);
  const [tablesLoading, setTablesLoading] = useState(true);

  // Look up the person by id across the user's own + public/example lists (mirrors
  // PersonSelectorBox.SetPerson(personIdUrl) from the Blazor original) — there's no
  // GetPerson-by-id-alone endpoint reachable without also knowing the owning ownerId.
  useEffect(() => {
    setPersonLoading(true);
    setPersonError(null);
    Promise.all([getPersonList(apiUrlDirect, effectiveOwnerId, visitorId), getPublicPersonList(apiUrlDirect)])
      .then(([own, pub]) => {
        const found = [...own, ...pub].find((p) => p.id === personId);
        if (!found) {
          setPersonError('Person not found.');
        } else {
          setPerson(found);
        }
      })
      .catch(() => setPersonError('Failed to load person.'))
      .finally(() => setPersonLoading(false));
  }, [apiUrlDirect, effectiveOwnerId, visitorId, personId]);

  useEffect(() => {
    if (!person) return;
    setTablesLoading(true);
    Promise.all([
      getHoroscopePredictions(apiUrlDirect, person.birthTime),
      getPlanetTable(apiUrlDirect, person.birthTime, ayanamsa),
      getHouseTable(apiUrlDirect, person.birthTime, ayanamsa),
      getSarvashtakavargaChart(apiUrlDirect, person.birthTime, ayanamsa),
      getBhinnashtakavargaChart(apiUrlDirect, person.birthTime, ayanamsa),
    ])
      .then(([preds, planets, houses, sarva, bhinna]) => {
        setPredictions(preds);
        setPlanetRows(planets);
        setHouseRows(houses);
        setSarvaRows(sarva);
        setBhinnaRows(bhinna);
      })
      .finally(() => setTablesLoading(false));
  }, [apiUrlDirect, person, ayanamsa]);

  const title = useMemo(() => (person ? `Horoscope | ${person.name}` : 'Horoscope'), [person]);

  if (personLoading) {
    return (
      <ThemedView style={styles.centered}>
        <ActivityIndicator />
      </ThemedView>
    );
  }

  if (personError || !person) {
    return (
      <ThemedView style={styles.centered}>
        <ThemedText themeColor="textSecondary">{personError ?? 'Person not found.'}</ThemedText>
      </ThemedView>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">{title}</ThemedText>

        <ThemedView style={styles.optionsRow}>
          <ChipGroup
            label="Ayanamsa"
            options={AYANAMSA_OPTIONS}
            selected={ayanamsa}
            onSelect={setAyanamsa}
          />
          <ChipGroup
            label="Chart Style"
            options={['South', 'North']}
            selected={chartStyle}
            onSelect={(v) => setChartStyle(v as 'South' | 'North')}
          />
        </ThemedView>

        <ThemedView style={styles.chartsRow}>
          <View style={styles.chartHalf}>
            <ThemedText type="smallBold" style={styles.sectionTitle}>
              Sky Chart
            </ThemedText>
            <SkyChartViewer apiUrlDirect={apiUrlDirect} birthTime={person.birthTime} />
          </View>
          <View style={styles.chartHalf}>
            <ThemedText type="smallBold" style={styles.sectionTitle}>
              Birth Chart
            </ThemedText>
            <IndianChart apiUrlDirect={apiUrlDirect} birthTime={person.birthTime} chartStyle={chartStyle} />
          </View>
        </ThemedView>

        {tablesLoading ? (
          <ActivityIndicator style={styles.tablesLoading} />
        ) : (
          <>
            <ThemedView style={styles.section}>
              <ThemedText type="smallBold" style={styles.sectionTitle}>
                Strength
              </ThemedText>
              <StrengthChart apiUrlDirect={apiUrlDirect} person={person} />
            </ThemedView>

            <ThemedView style={styles.section}>
              <ThemedText type="smallBold" style={styles.sectionTitle}>
                Planet Table
              </ThemedText>
              <PlanetDataTable rows={planetRows} />
            </ThemedView>

            <ThemedView style={styles.section}>
              <ThemedText type="smallBold" style={styles.sectionTitle}>
                House Table
              </ThemedText>
              <HouseDataTable rows={houseRows} />
            </ThemedView>

            <ThemedView style={styles.section}>
              <ThemedText type="smallBold" style={styles.sectionTitle}>
                Ashtakvarga
              </ThemedText>
              <AshtakvargaTable sarvaRows={sarvaRows} bhinnaRows={bhinnaRows} />
            </ThemedView>

            <ThemedView style={styles.section}>
              <ThemedText type="smallBold" style={styles.sectionTitle}>
                Predictions
              </ThemedText>
              <HoroscopeReferenceList predictions={predictions} />
            </ThemedView>
          </>
        )}
      </ThemedView>
    </ScrollView>
  );
}

function ChipGroup({
  label,
  options,
  selected,
  onSelect,
}: {
  label: string;
  options: string[];
  selected: string;
  onSelect: (value: string) => void;
}) {
  return (
    <ThemedView style={chipStyles.group}>
      <ThemedText type="small" themeColor="textSecondary">
        {label}
      </ThemedText>
      <ThemedView style={chipStyles.row}>
        {options.map((option) => {
          const active = option === selected;
          return (
            <Pressable
              key={option}
              onPress={() => onSelect(option)}
              style={[chipStyles.chip, active && chipStyles.chipActive]}>
              <ThemedText type="small" themeColor={active ? 'background' : 'text'}>
                {option}
              </ThemedText>
            </Pressable>
          );
        })}
      </ThemedView>
    </ThemedView>
  );
}

const chipStyles = StyleSheet.create({
  group: {
    gap: Spacing.one,
  },
  row: {
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
});

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: Spacing.five,
  },
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
  optionsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.four,
  },
  chartsRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.four,
  },
  chartHalf: {
    flex: 1,
    minWidth: 260,
    gap: Spacing.two,
  },
  section: {
    gap: Spacing.two,
  },
  sectionTitle: {
    marginBottom: Spacing.one,
  },
  tablesLoading: {
    marginVertical: Spacing.five,
  },
});
