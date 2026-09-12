import { useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { Icon } from './Icon';
import { OptionPickerModal, type PickerGroup, type PickerOption } from './OptionPickerModal';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';

export type DropdownOption = PickerOption;

/**
 * Generic single-select dropdown: a bordered pressable field that opens a modal option list
 * (see OptionPickerModal). Used for gender/country (flat `options`) and Ayanamsa (sectioned
 * `groups`, Easy/Advanced) pickers.
 */
export function Dropdown({
  value,
  options,
  groups,
  onChange,
  placeholder = 'Select…',
  label,
  searchable = false,
  bordered = true,
}: {
  value: string;
  options?: DropdownOption[];
  groups?: PickerGroup[];
  onChange: (value: string) => void;
  placeholder?: string;
  label?: string;
  searchable?: boolean;
  bordered?: boolean;
}) {
  const theme = useTheme();
  const [open, setOpen] = useState(false);

  const flatOptions = groups ? groups.flatMap((g) => g.options) : (options ?? []);
  const selected = flatOptions.find((o) => o.value === value);

  return (
    <View style={styles.wrapper}>
      <Pressable
        onPress={() => setOpen(true)}
        style={[
          styles.field,
          bordered ? [styles.fieldBordered, { borderColor: theme.backgroundSelected }] : styles.fieldBorderless,
        ]}>
        <ThemedText themeColor={selected ? 'text' : 'textSecondary'} style={styles.fieldText}>
          {selected?.label ?? placeholder}
        </ThemedText>
        <Icon name="chevron-down" size={16} color={theme.textSecondary} />
      </Pressable>

      <OptionPickerModal
        visible={open}
        title={label ?? placeholder}
        options={groups ? undefined : options}
        groups={groups}
        selectedValue={value}
        onSelect={onChange}
        onClose={() => setOpen(false)}
        searchable={searchable}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: {
    flex: 1,
    minWidth: 120,
  },
  field: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  fieldBordered: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.three,
    minHeight: 44,
  },
  fieldBorderless: {
    paddingVertical: Spacing.one,
  },
  fieldText: {
    flex: 1,
  },
});
