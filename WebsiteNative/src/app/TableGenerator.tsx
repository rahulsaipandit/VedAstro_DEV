import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { BirthTimeInput, type BirthTimeInputValue } from '@/components/BirthTimeInput';
import { DEFAULT_GEO_LOCATION } from '@/components/GeoLocationInput';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { getAllCalls, type CallMetadata } from '@/lib/api/listCalls';
import { getTimezoneOffsetForLocation } from '@/lib/api/geo';
import { buildBirthTimeJsonFromWallClock, timeToUrl, type BirthTimeJson } from '@/lib/time';
import { MaxContentWidth, Spacing } from '@/constants/theme';

function formatPayloadValue(value: unknown): string {
  if (value == null) return '—';
  if (Array.isArray(value)) return value.map(formatPayloadValue).join(', ');
  if (typeof value === 'object') return Object.values(value as object).map(String).join(', ');
  return String(value);
}

/**
 * Simplified port of Website/Pages/TableGenerator.razor. The original supports CSV/Excel upload
 * (needs a file-parsing library not installed in WebsiteNative) and a "Public +10k Horoscopes"
 * source (itself an "Under Construction" stub in the original — never built there either) — this
 * uses manual time-row entry instead of file upload, and restricts the column picker to
 * Calculate/* methods taking a single Time parameter (see src/lib/api/listCalls.ts), avoiding the
 * need to also collect a fixed PlanetName/HouseName per column. CSV/Excel/HTML file *download* of
 * the result isn't ported either — the generated table renders on screen only.
 */
export default function TableGeneratorScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());

  const [rows, setRows] = useState<{ label: string; time: BirthTimeJson }[]>([]);
  const [newRow, setNewRow] = useState<BirthTimeInputValue>({
    dd: '01',
    mm: '01',
    yyyy: '2000',
    hh: '00',
    min: '00',
    location: DEFAULT_GEO_LOCATION,
  });
  const [addingRow, setAddingRow] = useState(false);

  const [calls, setCalls] = useState<CallMetadata[] | null>(null);
  const [search, setSearch] = useState('');
  const [selectedColumns, setSelectedColumns] = useState<string[]>([]);

  const [table, setTable] = useState<string[][] | null>(null);
  const [generating, setGenerating] = useState(false);

  useEffect(() => {
    getAllCalls(apiUrlDirect).then((all) =>
      setCalls(all.filter((c) => c.parameters.length === 1 && c.parameters[0].parameterType === 'VedAstro.Library.Time'))
    );
  }, [apiUrlDirect]);

  const filteredCalls = calls?.filter((c) => c.name.toLowerCase().includes(search.toLowerCase())).slice(0, 30) ?? [];

  async function handleAddRow() {
    setAddingRow(true);
    try {
      const offset = await getTimezoneOffsetForLocation(
        apiUrlDirect,
        newRow.location,
        new Date(Date.UTC(Number(newRow.yyyy), Number(newRow.mm) - 1, Number(newRow.dd)))
      );
      const time = buildBirthTimeJsonFromWallClock(newRow.dd, newRow.mm, newRow.yyyy, newRow.hh, newRow.min, offset, newRow.location);
      setRows((prev) => [...prev, { label: `${newRow.dd}/${newRow.mm}/${newRow.yyyy} ${newRow.location.name}`, time }]);
    } finally {
      setAddingRow(false);
    }
  }

  function toggleColumn(name: string) {
    setSelectedColumns((prev) => (prev.includes(name) ? prev.filter((c) => c !== name) : [...prev, name]));
  }

  async function handleGenerate() {
    if (rows.length === 0 || selectedColumns.length === 0) return;
    setGenerating(true);
    try {
      const generated = await Promise.all(
        rows.map(async (row) => {
          const cells = await Promise.all(
            selectedColumns.map(async (columnName) => {
              const response = await fetch(`${apiUrlDirect}/Calculate/${columnName}${timeToUrl(row.time)}`);
              const json = await response.json();
              if (json.Status !== 'Pass') return '—';
              return formatPayloadValue(json.Payload);
            })
          );
          return [row.label, ...cells];
        })
      );
      setTable(generated);
    } finally {
      setGenerating(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">ML Data Generator</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Easily generate astronomical tables for use in ML/AI model training and data science.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Time List</ThemedText>
          <BirthTimeInput apiUrlDirect={apiUrlDirect} value={newRow} onChange={setNewRow} />
          <Pressable onPress={handleAddRow} disabled={addingRow} style={styles.addRowButton}>
            {addingRow ? (
              <ActivityIndicator size="small" color="#ffffff" />
            ) : (
              <ThemedText type="smallBold" themeColor="background">
                Add Row
              </ThemedText>
            )}
          </Pressable>
          {rows.length > 0 && (
            <ThemedView style={styles.rowsList}>
              {rows.map((row, index) => (
                <ThemedView key={index} style={styles.rowItem} type="backgroundElement">
                  <ThemedText type="small">{row.label}</ThemedText>
                  <Pressable onPress={() => setRows((prev) => prev.filter((_, i) => i !== index))}>
                    <ThemedText type="small" style={{ color: '#d33' }}>
                      Remove
                    </ThemedText>
                  </Pressable>
                </ThemedView>
              ))}
            </ThemedView>
          )}
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Data Columns</ThemedText>
          {!calls ? (
            <ActivityIndicator style={styles.loading} />
          ) : (
            <>
              <TextInput
                value={search}
                onChangeText={setSearch}
                placeholder="Search data points..."
                placeholderTextColor={theme.textSecondary}
                style={[styles.searchInput, { color: theme.text, borderColor: theme.backgroundSelected }]}
              />
              <ThemedView style={styles.chipRow}>
                {filteredCalls.map((call) => (
                  <Pressable
                    key={call.name}
                    onPress={() => toggleColumn(call.name)}
                    style={[styles.chip, selectedColumns.includes(call.name) && styles.chipActive]}>
                    <ThemedText type="small" themeColor={selectedColumns.includes(call.name) ? 'background' : 'text'}>
                      {call.name}
                    </ThemedText>
                  </Pressable>
                ))}
              </ThemedView>
            </>
          )}
        </ThemedView>

        <Pressable onPress={handleGenerate} disabled={generating} style={styles.generateButton}>
          {generating ? (
            <ActivityIndicator size="small" color="#ffffff" />
          ) : (
            <ThemedText type="smallBold" themeColor="background">
              Generate
            </ThemedText>
          )}
        </Pressable>

        {table && (
          <ScrollView horizontal>
            <View>
              <ThemedView style={[styles.tableRow, styles.tableHeaderRow, { borderColor: theme.backgroundSelected }]}>
                <ThemedText type="smallBold" style={styles.tableCellKey}>
                  Time
                </ThemedText>
                {selectedColumns.map((name) => (
                  <ThemedText type="smallBold" key={name} style={styles.tableCell}>
                    {name}
                  </ThemedText>
                ))}
              </ThemedView>
              {table.map((row, index) => (
                <ThemedView key={index} style={[styles.tableRow, { borderColor: theme.backgroundSelected }]}>
                  {row.map((cell, cellIndex) => (
                    <ThemedText
                      key={cellIndex}
                      type="small"
                      style={cellIndex === 0 ? styles.tableCellKey : styles.tableCell}>
                      {cell}
                    </ThemedText>
                  ))}
                </ThemedView>
              ))}
            </View>
          </ScrollView>
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
  section: {
    gap: Spacing.two,
  },
  addRowButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
  rowsList: {
    gap: Spacing.one,
  },
  rowItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
  },
  loading: {
    alignSelf: 'flex-start',
  },
  searchInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
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
    backgroundColor: '#1a9c4c',
  },
  generateButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  tableRow: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    paddingVertical: Spacing.two,
  },
  tableHeaderRow: {
    borderBottomWidth: 2,
  },
  tableCellKey: {
    width: 160,
    paddingHorizontal: Spacing.two,
  },
  tableCell: {
    width: 140,
    paddingHorizontal: Spacing.two,
  },
});
