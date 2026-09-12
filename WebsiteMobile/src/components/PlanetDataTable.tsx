import { ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { PLANET_TABLE_COLUMNS, type PlanetTableRow } from '@/lib/api/horoscope';

/**
 * Port of Horoscope.razor's `PlanetDataTable2` (originally rendered by vedastro.js's
 * GenerateAstroTable against reflection-dispatch metadata). Same 6 enabled columns, now backed
 * by typed getPlanetTable() calls instead of the JS table-generation library.
 */
export function PlanetDataTable({ rows }: { rows: PlanetTableRow[] }) {
  const theme = useTheme();
  return (
    <ScrollView horizontal>
      <View>
        <ThemedView style={[styles.row, styles.headerRow, { borderColor: theme.backgroundSelected }]}>
          <ThemedText type="smallBold" style={styles.cellKey}>
            Planet
          </ThemedText>
          {PLANET_TABLE_COLUMNS.map((col) => (
            <ThemedText type="smallBold" key={col.endpoint} style={styles.cell}>
              {col.name}
            </ThemedText>
          ))}
        </ThemedView>
        {rows.map((row) => (
          <ThemedView key={row.planet} style={[styles.row, { borderColor: theme.backgroundSelected }]}>
            <ThemedText type="smallBold" style={styles.cellKey}>
              {row.planet}
            </ThemedText>
            {row.values.map((value, index) => (
              <ThemedText type="small" key={index} style={styles.cell}>
                {value}
              </ThemedText>
            ))}
          </ThemedView>
        ))}
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
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
    width: 130,
    paddingHorizontal: Spacing.two,
  },
});
