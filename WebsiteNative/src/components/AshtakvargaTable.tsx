import { ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import type { AshtakvargaRow } from '@/lib/api/horoscope';

const ZODIAC_ABBR = ['Ar', 'Ta', 'Ge', 'Ca', 'Le', 'Vi', 'Li', 'Sc', 'Sg', 'Cp', 'Aq', 'Pi'];

function Section({ title, rows }: { title: string; rows: AshtakvargaRow[] }) {
  const theme = useTheme();
  return (
    <ThemedView style={styles.section}>
      <ThemedText type="smallBold">{title}</ThemedText>
      <ScrollView horizontal>
        <View>
          <ThemedView style={[styles.row, styles.headerRow, { borderColor: theme.backgroundSelected }]}>
            <ThemedText type="smallBold" style={styles.cellKey} />
            {ZODIAC_ABBR.map((zodiac) => (
              <ThemedText type="smallBold" key={zodiac} style={styles.cell}>
                {zodiac}
              </ThemedText>
            ))}
            <ThemedText type="smallBold" style={styles.cell}>
              Total
            </ThemedText>
          </ThemedView>
          {rows.map((row) => (
            <ThemedView key={row.key} style={[styles.row, { borderColor: theme.backgroundSelected }]}>
              <ThemedText type="small" style={styles.cellKey}>
                {row.key}
              </ThemedText>
              {row.points.map((point, index) => (
                <ThemedText type="small" key={index} style={styles.cell}>
                  {point}
                </ThemedText>
              ))}
              <ThemedText type="smallBold" style={styles.cell}>
                {row.total}
              </ThemedText>
            </ThemedView>
          ))}
        </View>
      </ScrollView>
    </ThemedView>
  );
}

/**
 * Port of Horoscope.razor's `Ashtakvarga` div (vedastro.js's GenerateAshtakvargaTable), now backed
 * by getSarvashtakavargaChart/getBhinnashtakavargaChart instead of the JS table library.
 */
export function AshtakvargaTable({
  sarvaRows,
  bhinnaRows,
}: {
  sarvaRows: AshtakvargaRow[];
  bhinnaRows: AshtakvargaRow[];
}) {
  return (
    <ThemedView style={styles.container}>
      <Section title="Sarvashtakavarga" rows={sarvaRows} />
      <Section title="Bhinnashtakavarga" rows={bhinnaRows} />
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
  row: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    paddingVertical: Spacing.two,
  },
  headerRow: {
    borderBottomWidth: 2,
  },
  cellKey: {
    width: 90,
    paddingHorizontal: Spacing.two,
  },
  cell: {
    width: 44,
    paddingHorizontal: Spacing.one,
  },
});
