import { useEffect, useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, TextInput, View, useWindowDimensions, type StyleProp, type ViewStyle } from 'react-native';
import { Calendar, type DateData } from 'react-native-calendars';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { Dropdown } from './Dropdown';
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

const AccentColor = '#2F6FED';
const pad2 = (n: number) => String(n).padStart(2, '0');
const CURRENT_YEAR = new Date().getFullYear();
const MIN_YEAR = 1900;
const MAX_YEAR = CURRENT_YEAR + 1;

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
];
const MONTH_OPTIONS = MONTH_NAMES.map((name, i) => ({ label: name, value: pad2(i + 1) }));
const YEAR_OPTIONS = Array.from({ length: MAX_YEAR - MIN_YEAR + 1 }, (_, i) => {
  const y = MAX_YEAR - i;
  return { label: String(y), value: String(y) };
});

function clampToRange(raw: string, min: number, max: number, digits: number): string {
  const digitsOnly = raw.replace(/[^0-9]/g, '');
  if (digitsOnly === '') return '';
  const n = Math.min(max, Math.max(min, Number(digitsOnly)));
  return String(n).padStart(digits, '0');
}

/**
 * Matches the "Add New Person" mock: a single "Hour" field and a single "Date of birth" field,
 * each a compact tappable box (like the mock's native `<input type="time">`/`<input
 * type="date">`, complete with a leading clock/calendar icon). "Hour" opens a sheet with typed
 * Hour/Minute boxes (no native time picker library is installed for RN). "Date of birth" opens a
 * react-native-calendars month view — works identically on web and native, unlike a native
 * platform date picker — with Month/Year quick-jump dropdowns above it so picking a birth date
 * decades back doesn't mean arrow-clicking one month at a time. 24-hour, matching the mock's
 * `type="time"` value ("08:30", no AM/PM) rather than a 12-hour + meridian split.
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
  const [viewYear, setViewYear] = useState(String(CURRENT_YEAR - 30));
  const [viewMonth, setViewMonth] = useState('01');
  const [calendarSeed, setCalendarSeed] = useState(0);

  function set<K extends keyof BirthTimeInputValue>(key: K, v: BirthTimeInputValue[K]) {
    onChange({ ...value, [key]: v });
  }

  function openDateSheet() {
    setViewYear(value.yyyy || String(CURRENT_YEAR - 30));
    setViewMonth(value.mm || '01');
    setCalendarSeed((s) => s + 1);
    setDateSheetOpen(true);
  }

  // Jumping via the Month/Year dropdowns needs the underlying Calendar to remount with a new
  // `current` (it only reads that prop on mount) — bump calendarSeed to force it. Swiping the
  // Calendar's own arrows (onMonthChange below) must NOT do this, or every swipe would remount it.
  function jumpToMonth(mm: string) {
    setViewMonth(mm);
    setCalendarSeed((s) => s + 1);
  }

  function jumpToYear(yyyy: string) {
    setViewYear(yyyy);
    setCalendarSeed((s) => s + 1);
  }

  function handleDayPress(day: DateData) {
    onChange({ ...value, dd: pad2(day.day), mm: pad2(day.month), yyyy: String(day.year) });
    setDateSheetOpen(false);
  }

  const timeDisplay = value.hh && value.min ? `${value.hh}:${value.min}` : '';
  const dateDisplay = value.dd && value.mm && value.yyyy ? `${value.dd}/${value.mm}/${value.yyyy}` : '';
  const markedDates =
    value.dd && value.mm && value.yyyy
      ? { [`${value.yyyy}-${value.mm}-${value.dd}`]: { selected: true, selectedColor: AccentColor, selectedTextColor: '#ffffff' } }
      : {};

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
            onPress={openDateSheet}
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
          placeholder="MM"
          width={64}
          maxLength={2}
          clamp={(raw) => clampToRange(raw, 0, 59, 2)}
          onChange={(v) => set('min', v)}
        />
      </PickerSheet>

      <PickerSheet
        visible={dateSheetOpen}
        title="Date of Birth"
        onClose={() => setDateSheetOpen(false)}
        contentStyle={calendarStyles.sheetContent}
        scrollable>
        <ThemedView style={calendarStyles.navRow}>
          <ThemedView style={calendarStyles.navCol}>
            <Dropdown value={viewMonth} options={MONTH_OPTIONS} onChange={jumpToMonth} placeholder="Month" />
          </ThemedView>
          <ThemedView style={calendarStyles.navCol}>
            <Dropdown value={viewYear} options={YEAR_OPTIONS} onChange={jumpToYear} placeholder="Year" />
          </ThemedView>
        </ThemedView>
        <Calendar
          key={calendarSeed}
          current={`${viewYear}-${viewMonth}-01`}
          onMonthChange={(m) => {
            setViewYear(String(m.year));
            setViewMonth(pad2(m.month));
          }}
          onDayPress={handleDayPress}
          markedDates={markedDates}
          minDate={`${MIN_YEAR}-01-01`}
          maxDate={`${MAX_YEAR}-12-31`}
          enableSwipeMonths
          theme={{
            backgroundColor: theme.background,
            calendarBackground: theme.background,
            textSectionTitleColor: theme.textSecondary,
            selectedDayBackgroundColor: AccentColor,
            selectedDayTextColor: '#ffffff',
            todayTextColor: AccentColor,
            dayTextColor: theme.text,
            textDisabledColor: theme.backgroundSelected,
            arrowColor: AccentColor,
            monthTextColor: theme.text,
            indicatorColor: AccentColor,
          }}
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
  contentStyle,
  scrollable = false,
}: {
  visible: boolean;
  title: string;
  onClose: () => void;
  children: React.ReactNode;
  contentStyle?: StyleProp<ViewStyle>;
  scrollable?: boolean;
}) {
  const theme = useTheme();
  const { height: windowHeight } = useWindowDimensions();
  const content = scrollable ? (
    <ScrollView style={{ maxHeight: windowHeight * 0.65 }} contentContainerStyle={[sheetStyles.row, contentStyle]}>
      {children}
    </ScrollView>
  ) : (
    <View style={[sheetStyles.row, contentStyle]}>{children}</View>
  );

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
        {content}
      </ThemedView>
    </Modal>
  );
}

function SelectField({
  label,
  value,
  placeholder,
  width,
  maxLength,
  clamp,
  onChange,
  allowTyping = true,
}: {
  label: string;
  value: string;
  placeholder: string;
  width: number;
  maxLength: number;
  clamp: (raw: string) => string;
  onChange: (v: string) => void;
  allowTyping?: boolean;
}) {
  const theme = useTheme();
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
      </ThemedView>
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
    overflow: 'hidden',
  },
  input: {
    flex: 1,
    minWidth: 0,
    width: '100%',
    paddingVertical: Spacing.two,
    paddingHorizontal: Spacing.one,
    textAlign: 'center',
  },
});

const calendarStyles = StyleSheet.create({
  sheetContent: {
    flexDirection: 'column',
    alignItems: 'stretch',
    justifyContent: 'flex-start',
    gap: Spacing.three,
  },
  navRow: {
    flexDirection: 'row',
    gap: Spacing.two,
  },
  navCol: {
    flex: 1,
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
