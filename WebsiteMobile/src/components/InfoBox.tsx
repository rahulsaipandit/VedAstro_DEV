import { StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon, type IconName } from './Icon';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';

/**
 * Port of ViewComponents/Components/InfoBox.razor. Icon is optional and now backed by
 * lucide-react-native (see Icon.tsx) instead of the old Iconify spans.
 */
export function InfoBox({ title, description, icon }: { title: string; description: string; icon?: IconName }) {
  const theme = useTheme();
  return (
    <ThemedView style={[styles.box, { backgroundColor: theme.backgroundElement }]}>
      <View style={styles.titleRow}>
        {icon && <Icon name={icon} size={18} />}
        <ThemedText type="smallBold">{title}</ThemedText>
      </View>
      <ThemedText type="small" themeColor="textSecondary">
        {description}
      </ThemedText>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  box: {
    flex: 1,
    minWidth: 200,
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.one,
  },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.one,
  },
});
