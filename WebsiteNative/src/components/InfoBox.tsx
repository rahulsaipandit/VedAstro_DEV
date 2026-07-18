import { StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';

/** Simplified port of ViewComponents/Components/InfoBox.razor — icon support dropped for now (no icon lib installed yet). */
export function InfoBox({ title, description }: { title: string; description: string }) {
  const theme = useTheme();
  return (
    <ThemedView style={[styles.box, { backgroundColor: theme.backgroundElement }]}>
      <ThemedText type="smallBold">{title}</ThemedText>
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
});
