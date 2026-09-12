import { useEffect, useState } from 'react';
import { ActivityIndicator, StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { getHousePowerList, getPlanetPowerList, type HousePowerRow, type PlanetPowerRow } from '@/lib/api/strength';
import type { Person } from '@/lib/api/person';

function barColor(percentage: number): string {
  if (percentage >= 66) return '#1a9c4c';
  if (percentage >= 33) return '#e0a300';
  return '#d33';
}

function Bar({ label, percentage }: { label: string; percentage: number }) {
  const theme = useTheme();
  return (
    <ThemedView style={styles.barRow}>
      <ThemedText type="small" style={styles.barLabel}>
        {label}
      </ThemedText>
      <ThemedView style={[styles.barTrack, { backgroundColor: theme.backgroundSelected }]}>
        <View
          style={[
            styles.barFill,
            { width: `${Math.max(2, Math.min(100, percentage))}%`, backgroundColor: barColor(percentage) },
          ]}
        />
      </ThemedView>
      <ThemedText type="small" themeColor="textSecondary" style={styles.barValue}>
        {Math.round(percentage)}%
      </ThemedText>
    </ThemedView>
  );
}

/**
 * Simplified port of ViewComponents/Components/StrengthChart.razor + PlanetChart.razor. Both
 * originals drew bars via hand-rolled canvas/SVG JS (JS.DrawPlanetStrengthChart/
 * DrawHouseStrengthChart) computed from client-side Library calls — here plain RN `View` bars are
 * driven by real API values (Calculate.PlanetPowerPercentage/HouseStrength, see
 * src/lib/api/strength.ts), unlocked by this session's Shashtiamsa JSON-serialization fix.
 */
export function StrengthChart({ apiUrlDirect, person }: { apiUrlDirect: string; person: Person }) {
  const [planets, setPlanets] = useState<PlanetPowerRow[] | null>(null);
  const [houses, setHouses] = useState<HousePowerRow[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    setPlanets(null);
    setHouses(null);
    Promise.all([getPlanetPowerList(apiUrlDirect, person.birthTime), getHousePowerList(apiUrlDirect, person.birthTime)]).then(
      ([planetList, houseList]) => {
        if (cancelled) return;
        setPlanets(planetList);
        setHouses(houseList);
      }
    );
    return () => {
      cancelled = true;
    };
  }, [apiUrlDirect, person]);

  if (!planets || !houses) {
    return <ActivityIndicator style={styles.loading} />;
  }

  return (
    <ThemedView style={styles.container}>
      <ThemedView style={styles.section}>
        <ThemedText type="smallBold">Planet Strength</ThemedText>
        {planets.map((row) => (
          <Bar key={row.planet} label={row.planet} percentage={row.percentage} />
        ))}
      </ThemedView>
      <ThemedView style={styles.section}>
        <ThemedText type="smallBold">House Strength</ThemedText>
        {houses.map((row) => (
          <Bar key={row.house} label={row.house.replace('House', 'H')} percentage={row.percentage} />
        ))}
      </ThemedView>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: Spacing.four,
  },
  section: {
    gap: Spacing.two,
  },
  loading: {
    marginVertical: Spacing.four,
  },
  barRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
  },
  barLabel: {
    width: 60,
  },
  barTrack: {
    flex: 1,
    height: 10,
    borderRadius: 5,
    overflow: 'hidden',
  },
  barFill: {
    height: '100%',
    borderRadius: 5,
  },
  barValue: {
    width: 40,
    textAlign: 'right',
  },
});
