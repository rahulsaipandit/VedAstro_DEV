import { ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { HOUSE_TABLE_COLUMNS, type HouseTableRow } from '@/lib/api/horoscope';

/** Port of Horoscope.razor's `HouseDataTable2`. See PlanetDataTable.tsx for the general approach. */
export function HouseDataTable({ rows }: { rows: HouseTableRow[] }) {
  const theme = useTheme();
  return (
    <ScrollView horizontal>
      <View>
        <ThemedView style={[styles.row, styles.headerRow, { borderColor: theme.backgroundSelected }]}>
          <ThemedText type="smallBold" style={styles.cellKey}>
            House
          </ThemedText>
          {HOUSE_TABLE_COLUMNS.map((col) => (
            <ThemedText type="smallBold" key={col.endpoint} style={styles.cell}>
              {col.name}
            </ThemedText>
          ))}
        </ThemedView>
        {rows.map((row) => (
          <ThemedView key={row.house} style={[styles.row, { borderColor: theme.backgroundSelected }]}>
            <ThemedText type="smallBold" style={styles.cellKey}>
              {row.house.replace('House', 'H')}
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
    width: 60,
    paddingHorizontal: Spacing.two,
  },
  cell: {
    width: 130,
    paddingHorizontal: Spacing.two,
  },
});
