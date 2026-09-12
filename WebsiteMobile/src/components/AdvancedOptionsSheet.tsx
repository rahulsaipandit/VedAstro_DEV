import { useState } from 'react';
import { Modal, Pressable, StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon, type IconName } from './Icon';
import { OptionPickerModal } from './OptionPickerModal';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { AYANAMSA_GROUPS } from '@/constants/ayanamsa';
import { HOUSE_SYSTEM_OPTIONS, NODE_TYPE_OPTIONS, type CalculationPreferences } from '@/lib/preferences';

const HOUSE_SYSTEM_PICKER_OPTIONS = HOUSE_SYSTEM_OPTIONS.map((v) => ({ label: v, value: v }));
const NODE_TYPE_PICKER_OPTIONS = NODE_TYPE_OPTIONS.map((v) => ({ label: v, value: v }));

type Field = 'ayanamsa' | 'houseSystem' | 'nodeType';

/**
 * Gear-icon-triggered "Advanced Options" panel for the Add/Edit Person forms — settings for
 * *how a chart gets computed* (Ayanamsa/House system/Node type), not part of the person's saved
 * data (see src/lib/preferences.ts). Uses RN's built-in slide-up Modal animation rather than a
 * hand-rolled Animated drawer.
 */
export function AdvancedOptionsSheet({
  visible,
  onClose,
  prefs,
  onChange,
}: {
  visible: boolean;
  onClose: () => void;
  prefs: CalculationPreferences;
  onChange: (next: CalculationPreferences) => void;
}) {
  const theme = useTheme();
  const [openField, setOpenField] = useState<Field | null>(null);

  const ayanamsaLabel =
    AYANAMSA_GROUPS.flatMap((g) => g.options).find((o) => o.value === prefs.ayanamsa)?.label ?? prefs.ayanamsa;

  return (
    <>
      <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
        <Pressable style={styles.backdrop} onPress={onClose} />
        <ThemedView style={[styles.sheet, { backgroundColor: theme.background }]}>
          <View style={[styles.handle, { backgroundColor: theme.backgroundSelected }]} />
          <View style={styles.header}>
            <ThemedText type="small" themeColor="textSecondary" style={styles.eyebrow}>
              Advanced Options
            </ThemedText>
            <Pressable onPress={onClose} hitSlop={8} style={[styles.closeButton, { backgroundColor: theme.backgroundElement }]}>
              <Icon name="close" size={16} color={theme.textSecondary} />
            </Pressable>
          </View>

          <AdvRow
            icon="sparkles"
            label="Ayanamsa"
            valueLabel={ayanamsaLabel}
            onPress={() => setOpenField('ayanamsa')}
          />
          <AdvRow
            icon="house"
            label="House system"
            valueLabel={prefs.houseSystem}
            onPress={() => setOpenField('houseSystem')}
          />
          <AdvRow icon="moon" label="Node type" valueLabel={prefs.nodeType} onPress={() => setOpenField('nodeType')} />

          <ThemedText type="small" themeColor="textSecondary" style={styles.note}>
            These settings only affect chart computation, not saved profile data.
          </ThemedText>
        </ThemedView>
      </Modal>

      <OptionPickerModal
        visible={openField === 'ayanamsa'}
        title="Ayanamsa"
        groups={AYANAMSA_GROUPS}
        selectedValue={prefs.ayanamsa}
        onSelect={(v) => onChange({ ...prefs, ayanamsa: v })}
        onClose={() => setOpenField(null)}
        searchable
      />
      <OptionPickerModal
        visible={openField === 'houseSystem'}
        title="House system"
        options={HOUSE_SYSTEM_PICKER_OPTIONS}
        selectedValue={prefs.houseSystem}
        onSelect={(v) => onChange({ ...prefs, houseSystem: v })}
        onClose={() => setOpenField(null)}
      />
      <OptionPickerModal
        visible={openField === 'nodeType'}
        title="Node type"
        options={NODE_TYPE_PICKER_OPTIONS}
        selectedValue={prefs.nodeType}
        onSelect={(v) => onChange({ ...prefs, nodeType: v })}
        onClose={() => setOpenField(null)}
      />
    </>
  );
}

function AdvRow({
  icon,
  label,
  valueLabel,
  onPress,
}: {
  icon: IconName;
  label: string;
  valueLabel: string;
  onPress: () => void;
}) {
  const theme = useTheme();
  return (
    <ThemedView style={[styles.advRow, { borderColor: theme.backgroundSelected }]}>
      <View style={styles.advLabel}>
        <Icon name={icon} size={16} color="#2F6FED" />
        <ThemedText type="smallBold">{label}</ThemedText>
      </View>
      <Pressable onPress={onPress} style={[styles.advSelect, { borderColor: theme.backgroundSelected }]}>
        <ThemedText type="small" numberOfLines={1} style={styles.advSelectText}>
          {valueLabel}
        </ThemedText>
        <Icon name="chevron-down" size={14} color={theme.textSecondary} />
      </Pressable>
    </ThemedView>
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
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    paddingHorizontal: Spacing.four,
    paddingTop: Spacing.two,
    paddingBottom: Spacing.five,
    gap: Spacing.one,
  },
  handle: {
    width: 36,
    height: 4,
    borderRadius: 3,
    alignSelf: 'center',
    marginBottom: Spacing.three,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: Spacing.two,
  },
  eyebrow: {
    textTransform: 'uppercase',
    fontWeight: '700',
    letterSpacing: 0.4,
  },
  closeButton: {
    width: 30,
    height: 30,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  advRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: Spacing.two,
    paddingVertical: Spacing.three,
    borderBottomWidth: 1,
  },
  advLabel: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
  },
  advSelect: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.one,
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.two,
    paddingVertical: Spacing.two,
    minWidth: 150,
    justifyContent: 'space-between',
  },
  advSelectText: {
    flexShrink: 1,
  },
  note: {
    fontSize: 11,
    marginTop: Spacing.one,
  },
});
