import { useEffect, useState } from 'react';
import { Modal, Pressable, StyleSheet, TextInput, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { OptionPickerModal } from './OptionPickerModal';
import { GeoLocationInput } from './GeoLocationInput';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import type { GeoLocation } from '@/lib/api/geo';

export type BirthTimeInputValue = {
  dd: string;
  mm: string;
  yyyy: string;
  hh: string; // 24-hour, "00".."23" — matches Time.cs's StdTime wire format
  min: string;
  location: GeoLocation;
};

const pad2 = (n: number) => String(n).padStart(2, '0');
const HOUR_24_OPTIONS = Array.from({ length: 24 }, (_, i) => pad2(i)); // 00..23
const MINUTE_OPTIONS = Array.from({ length: 60 }, (_, i) => pad2(i)); // 00..59
const MONTH_OPTIONS = Array.from({ length: 12 }, (_, i) => pad2(i + 1)); // 01..12
const CURRENT_YEAR = new Date().getFullYear();
const YEAR_OPTIONS = Array.from({ length: CURRENT_YEAR - 1900 + 2 }, (_, i) => String(CURRENT_YEAR + 1 - i));

function daysInMonth(mm: string, yyyy: string): number {
  const m = Number(mm) || 1;
  const y = Number(yyyy) || CURRENT_YEAR;
  return new Date(y, m, 0).getDate();
}

function clampToRange(raw: string, min: number, max: number, digits: number): string {
  const digitsOnly = raw.replace(/[^0-9]/g, '');
  if (digitsOnly === '') return '';
  const n = Math.min(max, Math.max(min, Number(digitsOnly)));
  return String(n).padStart(digits, '0');
}

/**
 * Matches the "Add New Person" mock: a single "Hour" field and a single "Date of birth" field,
 * each a compact tappable box (like the mock's native `<input type="time">`/`<input
 * type="date">`, complete with a leading clock/calendar icon) that opens a sheet with the actual
 * pickers — no native date/time picker library is installed for RN, so tapping in opens a plain
 * 24-hour Hour/Minute (or Day/Month/Year) selector that supports both typing a value directly and
 * picking from a list, with range validation. 24-hour, matching the mock's `type="time"` value
 * ("08:30", no AM/PM) rather than a 12-hour + meridian split.
 */
export function BirthTimeInput({
  apiUrlDirect,
  value,
  onChange,
}: {
  apiUrlDirect: string;
  value: BirthTimeInputValue;
  onChange: (value: BirthTimeInputValue) => void;
}) {
  const theme = useTheme();
  const [timeSheetOpen, setTimeSheetOpen] = useState(false);
  const [dateSheetOpen, setDateSheetOpen] = useState(false);

  function set<K extends keyof BirthTimeInputValue>(key: K, v: BirthTimeInputValue[K]) {
    onChange({ ...value, [key]: v });
  }

  function setMonth(v: string) {
    // clamp day to stay valid when switching to a shorter month (e.g. 31 -> Feb)
    const maxDay = daysInMonth(v, value.yyyy);
    const clampedDay = value.dd !== '' && Number(value.dd) > maxDay ? pad2(maxDay) : value.dd;
    onChange({ ...value, mm: v, dd: clampedDay });
  }

  function setYear(v: string) {
    const maxDay = daysInMonth(value.mm, v);
    const clampedDay = value.dd !== '' && Number(value.dd) > maxDay ? pad2(maxDay) : value.dd;
    onChange({ ...value, yyyy: v, dd: clampedDay });
  }

  const dayOptions = Array.from({ length: daysInMonth(value.mm, value.yyyy) }, (_, i) => pad2(i + 1));

  const timeDisplay = value.hh && value.min ? `${value.hh}:${value.min}` : '';
  const dateDisplay = value.dd && value.mm && value.yyyy ? `${value.dd}/${value.mm}/${value.yyyy}` : '';

  return (
    <ThemedView style={styles.container}>
      <ThemedText style={styles.fieldLabel}>
        Birth time<ThemedText style={styles.requiredAsterisk}> *</ThemedText>
      </ThemedText>

      <ThemedView style={styles.fieldRow}>
        <ThemedView style={styles.fieldCol}>
          <ThemedText style={styles.microLabel}>Hour</ThemedText>
          <Pressable
            onPress={() => setTimeSheetOpen(true)}
            style={[styles.compactField, { borderColor: theme.backgroundSelected }]}>
            <ThemedText themeColor={timeDisplay ? 'text' : 'textSecondary'} style={styles.compactFieldText}>
              {timeDisplay || 'HH:MM'}
            </ThemedText>
            <Icon name="clock" size={16} color={theme.textSecondary} />
          </Pressable>
        </ThemedView>
        <ThemedView style={styles.fieldCol}>
          <ThemedText style={styles.microLabel}>Date of birth</ThemedText>
          <Pressable
            onPress={() => setDateSheetOpen(true)}
            style={[styles.compactField, { borderColor: theme.backgroundSelected }]}>
            <ThemedText themeColor={dateDisplay ? 'text' : 'textSecondary'} style={styles.compactFieldText}>
              {dateDisplay || 'DD/MM/YYYY'}
            </ThemedText>
            <Icon name="calendar" size={16} color={theme.textSecondary} />
          </Pressable>
        </ThemedView>
      </ThemedView>

      <GeoLocationInput
        apiUrlDirect={apiUrlDirect}
        location={value.location}
        onChange={(location) => set('location', location)}
      />

      <PickerSheet visible={timeSheetOpen} title="Birth Hour" onClose={() => setTimeSheetOpen(false)}>
        <SelectField
          label="Hour"
          value={value.hh}
          options={HOUR_24_OPTIONS}
          placeholder="HH"
          width={64}
          maxLength={2}
          clamp={(raw) => clampToRange(raw, 0, 23, 2)}
          onChange={(v) => set('hh', v)}
        />
        <ThemedText>:</ThemedText>
        <SelectField
          label="Minute"
          value={value.min}
          options={MINUTE_OPTIONS}
          placeholder="MM"
          width={64}
          maxLength={2}
          clamp={(raw) => clampToRange(raw, 0, 59, 2)}
          onChange={(v) => set('min', v)}
        />
      </PickerSheet>

      <PickerSheet visible={dateSheetOpen} title="Date of Birth" onClose={() => setDateSheetOpen(false)}>
        <SelectField
          label="Day"
          value={value.dd}
          options={dayOptions}
          placeholder="DD"
          width={56}
          maxLength={2}
          clamp={(raw) => clampToRange(raw, 1, daysInMonth(value.mm, value.yyyy), 2)}
          onChange={(v) => set('dd', v)}
        />
        <ThemedText>/</ThemedText>
        <SelectField
          label="Month"
          value={value.mm}
          options={MONTH_OPTIONS}
          placeholder="MM"
          width={56}
          maxLength={2}
          clamp={(raw) => clampToRange(raw, 1, 12, 2)}
          onChange={setMonth}
        />
        <ThemedText>/</ThemedText>
        <SelectField
          label="Year"
          value={value.yyyy}
          options={YEAR_OPTIONS}
          placeholder="YYYY"
          width={76}
          maxLength={4}
          clamp={(raw) => clampToRange(raw, 1900, CURRENT_YEAR + 1, 4)}
          onChange={setYear}
        />
      </PickerSheet>
    </ThemedView>
  );
}

function PickerSheet({
  visible,
  title,
  onClose,
  children,
}: {
  visible: boolean;
  title: string;
  onClose: () => void;
  children: React.ReactNode;
}) {
  const theme = useTheme();
  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <Pressable style={sheetStyles.backdrop} onPress={onClose} />
      <ThemedView style={[sheetStyles.sheet, { backgroundColor: theme.background }]}>
        <View style={[sheetStyles.handle, { backgroundColor: theme.backgroundSelected }]} />
        <View style={sheetStyles.header}>
          <ThemedText type="smallBold">{title}</ThemedText>
          <Pressable onPress={onClose} hitSlop={8} style={[sheetStyles.closeButton, { backgroundColor: theme.backgroundElement }]}>
            <Icon name="close" size={16} color={theme.textSecondary} />
          </Pressable>
        </View>
        <View style={sheetStyles.row}>{children}</View>
      </ThemedView>
    </Modal>
  );
}

function SelectField({
  label,
  value,
  options,
  placeholder,
  width,
  maxLength,
  clamp,
  onChange,
  allowTyping = true,
}: {
  label: string;
  value: string;
  options: string[];
  placeholder: string;
  width: number;
  maxLength: number;
  clamp: (raw: string) => string;
  onChange: (v: string) => void;
  allowTyping?: boolean;
}) {
  const theme = useTheme();
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState(value);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  function commit(raw: string) {
    const cleaned = clamp(raw);
    setDraft(cleaned);
    onChange(cleaned);
  }

  return (
    <ThemedView style={[styles.selectWrapper, { width }]}>
      <ThemedText style={styles.selectMicroLabel} themeColor="textSecondary">
        {label}
      </ThemedText>
      <ThemedView style={[styles.field, { borderColor: theme.backgroundSelected }]}>
        <TextInput
          value={draft}
          onChangeText={allowTyping ? setDraft : undefined}
          editable={allowTyping}
          onBlur={() => commit(draft)}
          onSubmitEditing={() => commit(draft)}
          placeholder={placeholder}
          placeholderTextColor={theme.textSecondary}
          keyboardType="numeric"
          maxLength={maxLength}
          style={[styles.input, { color: theme.text }]}
        />
        <Pressable onPress={() => setOpen(true)} hitSlop={8} style={styles.chevron}>
          <Icon name="chevron-down" size={14} color={theme.textSecondary} />
        </Pressable>
      </ThemedView>

      <OptionPickerModal
        visible={open}
        title={label}
        options={options.map((opt) => ({ label: opt, value: opt }))}
        selectedValue={value}
        onSelect={commit}
        onClose={() => setOpen(false)}
      />
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: Spacing.two,
  },
  fieldLabel: {
    fontSize: 13,
    fontWeight: '600',
  },
  requiredAsterisk: {
    color: '#D64545',
  },
  fieldRow: {
    flexDirection: 'row',
    gap: Spacing.two,
  },
  fieldCol: {
    flex: 1,
    minWidth: 0,
    gap: 4,
  },
  microLabel: {
    fontSize: 11,
    color: '#9AA0AC',
  },
  compactField: {
    height: 46,
    borderWidth: 1,
    borderRadius: 12,
    paddingHorizontal: Spacing.three,
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
  },
  compactFieldText: {
    flex: 1,
  },
  selectWrapper: {
    position: 'relative',
    gap: 4,
  },
  selectMicroLabel: {
    fontSize: 11,
    textAlign: 'center',
  },
  field: {
    borderWidth: 1,
    borderRadius: 8,
    flexDirection: 'row',
    alignItems: 'center',
    minHeight: 44,
  },
  input: {
    flex: 1,
    paddingVertical: Spacing.two,
    paddingLeft: Spacing.one,
    textAlign: 'center',
  },
  chevron: {
    paddingHorizontal: Spacing.one,
  },
});

const sheetStyles = StyleSheet.create({
  backdrop: {
    ...StyleSheet.absoluteFill,
    backgroundColor: '#00000066',
  },
  sheet: {
    position: 'absolute',
    left: 0,
    right: 0,
    bottom: 0,
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    paddingHorizontal: Spacing.four,
    paddingTop: Spacing.two,
    paddingBottom: Spacing.six,
    gap: Spacing.three,
  },
  handle: {
    width: 36,
    height: 4,
    borderRadius: 3,
    alignSelf: 'center',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  closeButton: {
    width: 30,
    height: 30,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  row: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    justifyContent: 'center',
    gap: Spacing.two,
  },
});
