import { useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, TextInput, useWindowDimensions } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';

export type PickerOption = { label: string; value: string };
export type PickerGroup = { label: string; options: PickerOption[] };

/**
 * Shared bottom-sheet-style option list used by Dropdown and BirthTimeInput's SelectField.
 * Renders via RN's Modal (its own top-level layer) rather than an absolutely-positioned View
 * nested in the normal layout — the earlier per-field approach got visually clipped/covered by
 * whatever sibling field came next in the form (e.g. Country's list hidden behind the Search
 * row), since a plain `position: absolute` child only stacks above siblings within the same
 * local stacking context, not across a scrolling form of many bordered rows. A modal sidesteps
 * that entirely and doubles as a bigger, thumb-friendly tap target on mobile.
 *
 * Pass either `options` (flat list) or `groups` (sectioned, e.g. Ayanamsa's Easy/Advanced split)
 * — not both.
 */
export function OptionPickerModal({
  visible,
  title,
  options,
  groups,
  selectedValue,
  onSelect,
  onClose,
  searchable = false,
}: {
  visible: boolean;
  title: string;
  options?: PickerOption[];
  groups?: PickerGroup[];
  selectedValue: string;
  onSelect: (value: string) => void;
  onClose: () => void;
  searchable?: boolean;
}) {
  const theme = useTheme();
  const { height: windowHeight } = useWindowDimensions();
  const [query, setQuery] = useState('');

  const q = query.toLowerCase();
  const visibleGroups: PickerGroup[] = groups
    ? groups
        .map((g) => ({ ...g, options: searchable ? g.options.filter((o) => o.label.toLowerCase().includes(q)) : g.options }))
        .filter((g) => g.options.length > 0)
    : [{ label: '', options: searchable ? (options ?? []).filter((o) => o.label.toLowerCase().includes(q)) : options ?? [] }];

  const totalVisible = visibleGroups.reduce((n, g) => n + g.options.length, 0);

  function handleClose() {
    setQuery('');
    onClose();
  }

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={handleClose}>
      <Pressable style={styles.backdrop} onPress={handleClose} />
      <ThemedView style={[styles.sheet, { backgroundColor: theme.background, maxHeight: windowHeight * 0.6 }]}>
        <ThemedText type="smallBold" style={styles.title}>
          {title}
        </ThemedText>
        {searchable && (
          <TextInput
            value={query}
            onChangeText={setQuery}
            placeholder="Search…"
            placeholderTextColor={theme.textSecondary}
            autoFocus
            style={[styles.searchInput, { color: theme.text, borderColor: theme.backgroundSelected }]}
          />
        )}
        <ScrollView keyboardShouldPersistTaps="handled">
          {visibleGroups.map((group) => (
            <ThemedView key={group.label || '_'}>
              {group.label !== '' && (
                <ThemedText type="small" themeColor="textSecondary" style={styles.groupLabel}>
                  {group.label}
                </ThemedText>
              )}
              {group.options.map((opt) => (
                <Pressable
                  key={opt.value}
                  onPress={() => {
                    onSelect(opt.value);
                    handleClose();
                  }}
                  style={[styles.item, opt.value === selectedValue && { backgroundColor: theme.backgroundSelected }]}>
                  <ThemedText>{opt.label}</ThemedText>
                </Pressable>
              ))}
            </ThemedView>
          ))}
          {totalVisible === 0 && (
            <ThemedText type="small" themeColor="textSecondary" style={styles.item}>
              No matches
            </ThemedText>
          )}
        </ScrollView>
      </ThemedView>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    ...StyleSheet.absoluteFill,
    backgroundColor: '#00000066',
  },
  sheet: {
    position: 'absolute',
    left: 0,
    right: 0,
    bottom: 0,
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    paddingTop: Spacing.three,
    paddingBottom: Spacing.four,
  },
  title: {
    paddingHorizontal: Spacing.four,
    paddingBottom: Spacing.two,
  },
  searchInput: {
    marginHorizontal: Spacing.four,
    marginBottom: Spacing.two,
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    outlineWidth: 0,
  },
  groupLabel: {
    textTransform: 'uppercase',
    fontWeight: '700',
    paddingHorizontal: Spacing.four,
    paddingTop: Spacing.three,
    paddingBottom: Spacing.one,
  },
  item: {
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.three,
    minHeight: 44,
    justifyContent: 'center',
  },
});
