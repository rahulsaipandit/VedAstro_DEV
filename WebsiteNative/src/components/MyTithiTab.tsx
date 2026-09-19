import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, TextInput, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { BirthTimeInput, type BirthTimeInputValue } from './BirthTimeInput';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { useMyTithiStore } from '@/store/useMyTithiStore';
import { getVedicBirthDate } from '@/lib/api/vedicBirthday';
import { buildBirthTimeJsonFromWallClock, parseStdTime } from '@/lib/time';
import { getTimezoneOffsetForLocation, type GeoLocation } from '@/lib/api/geo';
import { showErrorToast } from '@/lib/toast';

const DEFAULT_LOCATION: GeoLocation = { name: 'Seattle', longitude: -122.3321, latitude: 47.6062 };

function blankBirthTime(): BirthTimeInputValue {
  return { dd: '', mm: '', yyyy: '', hh: '12', min: '00', location: DEFAULT_LOCATION };
}

/**
 * "My Tithi" tab - see src/store/useMyTithiStore.ts for why this is entirely client-side. Each
 * saved entry's next occurrence is resolved from Calculate.VedicBirthDate, trying this year first
 * and next year if this year's occurrence has already passed.
 */
export function MyTithiTab({ apiUrlDirect }: { apiUrlDirect: string }) {
  const theme = useTheme();
  const entries = useMyTithiStore((s) => s.entries);
  const addEntry = useMyTithiStore((s) => s.addEntry);
  const removeEntry = useMyTithiStore((s) => s.removeEntry);

  const [adding, setAdding] = useState(false);
  const [labelInput, setLabelInput] = useState('');
  const [birthTime, setBirthTime] = useState<BirthTimeInputValue>(blankBirthTime);
  const [saving, setSaving] = useState(false);

  async function handleAdd() {
    if (!labelInput.trim()) {
      showErrorToast('Please enter a name for this tithi');
      return;
    }
    if (!birthTime.dd || !birthTime.mm || !birthTime.yyyy) {
      showErrorToast('Please pick a reference date');
      return;
    }
    setSaving(true);
    try {
      const offset = await getTimezoneOffsetForLocation(
        apiUrlDirect,
        birthTime.location,
        new Date(Date.UTC(Number(birthTime.yyyy), Number(birthTime.mm) - 1, Number(birthTime.dd)))
      );
      const referenceDate = buildBirthTimeJsonFromWallClock(
        birthTime.dd,
        birthTime.mm,
        birthTime.yyyy,
        birthTime.hh || '12',
        birthTime.min || '00',
        offset,
        birthTime.location
      );
      addEntry(labelInput.trim(), referenceDate);
      setLabelInput('');
      setBirthTime(blankBirthTime());
      setAdding(false);
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Failed to save this tithi');
    } finally {
      setSaving(false);
    }
  }

  return (
    <ThemedView style={styles.list}>
      <ThemedText type="small" themeColor="textSecondary" style={styles.subtitle}>
        Track a personal tithi (e.g. a death-anniversary tithi) - its yearly recurrence is found
        the same way your Vedic birthday is. Saved only on this device.
      </ThemedText>

      {entries.map((entry) => (
        <MyTithiRow key={entry.id} apiUrlDirect={apiUrlDirect} entry={entry} onRemove={() => removeEntry(entry.id)} />
      ))}

      {!adding && (
        <Pressable onPress={() => setAdding(true)} style={[styles.addButton, { borderColor: theme.backgroundSelected }]}>
          <Icon name="plus" size={16} color={theme.text} />
          <ThemedText type="smallBold">Add a tithi</ThemedText>
        </Pressable>
      )}

      {adding && (
        <ThemedView style={[styles.addForm, { borderColor: theme.backgroundSelected }]}>
          <TextInput
            value={labelInput}
            onChangeText={setLabelInput}
            placeholder="Name (e.g. Grandfather's Shraddha)"
            placeholderTextColor={theme.textSecondary}
            style={[styles.labelInput, { borderColor: theme.backgroundSelected, color: theme.text }]}
          />
          <BirthTimeInput apiUrlDirect={apiUrlDirect} value={birthTime} onChange={setBirthTime} />
          <View style={styles.addFormButtons}>
            <Pressable onPress={() => setAdding(false)} style={styles.cancelButton}>
              <ThemedText type="smallBold">Cancel</ThemedText>
            </Pressable>
            <Pressable onPress={handleAdd} disabled={saving} style={styles.saveButton}>
              {saving ? <ActivityIndicator size="small" color="#ffffff" /> : <ThemedText type="smallBold" themeColor="background">Save</ThemedText>}
            </Pressable>
          </View>
        </ThemedView>
      )}
    </ThemedView>
  );
}

function MyTithiRow({
  apiUrlDirect,
  entry,
  onRemove,
}: {
  apiUrlDirect: string;
  entry: { id: string; label: string; referenceDate: import('@/lib/time').BirthTimeJson };
  onRemove: () => void;
}) {
  const theme = useTheme();
  const [nextOccurrence, setNextOccurrence] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);

    (async () => {
      const currentYear = new Date().getFullYear();
      let matched = await getVedicBirthDate(apiUrlDirect, entry.referenceDate, currentYear);
      if (parseStdTime(matched.StdTime).getTime() < Date.now()) {
        matched = await getVedicBirthDate(apiUrlDirect, entry.referenceDate, currentYear + 1);
      }
      if (!cancelled) {
        const match = /^\d{2}:\d{2} (\d{2})\/(\d{2})\/(\d{4})/.exec(matched.StdTime);
        setNextOccurrence(match ? `${match[1]}/${match[2]}/${match[3]}` : matched.StdTime);
      }
    })()
      .catch((e) => showErrorToast(e instanceof Error ? e.message : 'Failed to resolve next occurrence'))
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [apiUrlDirect, entry.referenceDate]);

  return (
    <View style={[styles.entryRow, { borderColor: theme.backgroundSelected }]}>
      <Icon name="calendar" size={22} color={theme.textSecondary} />
      <ThemedView style={styles.entryText}>
        <ThemedText type="smallBold">{entry.label}</ThemedText>
        {loading ? <ActivityIndicator size="small" /> : <ThemedText type="small" themeColor="textSecondary">Next: {nextOccurrence}</ThemedText>}
      </ThemedView>
      <Pressable onPress={onRemove} hitSlop={8}>
        <Icon name="close" size={16} color={theme.textSecondary} />
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  list: {
    gap: Spacing.two,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  entryRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.three,
    borderWidth: 1,
    borderRadius: 12,
    padding: Spacing.three,
  },
  entryText: {
    flex: 1,
    gap: 2,
  },
  addButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.one,
    borderWidth: 1,
    borderStyle: 'dashed',
    borderRadius: 12,
    padding: Spacing.three,
  },
  addForm: {
    borderWidth: 1,
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.three,
  },
  labelInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
  },
  addFormButtons: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: Spacing.two,
  },
  cancelButton: {
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
  },
  saveButton: {
    backgroundColor: '#0d6efd',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
});
