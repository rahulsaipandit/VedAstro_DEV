import { Pressable, StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { Spacing } from '@/constants/theme';

/**
 * Port of the old Blazor sidebar's "Local API: ON/OFF" stamp
 * (Website/Shared/MainLayout.razor, see Localhost_Setup.md) - toggles useAppStore's debugMode,
 * which switches every API call between the deployed API and http://localhost:7071/api (see
 * src/constants/urls.ts's getApiUrlDirect). Rendered globally in _layout.tsx (same
 * always-visible-from-any-screen placement as the original), not gated behind __DEV__ since
 * the old site's toggle wasn't either - it's a harmless local-only switch, not a secret.
 */
export function DebugModeToggle() {
  const theme = useTheme();
  const debugMode = useAppStore((s) => s.debugMode);
  const setDebugMode = useAppStore((s) => s.setDebugMode);

  return (
    <Pressable
      onPress={() => setDebugMode(!debugMode)}
      style={[styles.container, { backgroundColor: theme.backgroundElement, borderColor: theme.backgroundSelected }]}>
      <ThemedText type="small" themeColor={debugMode ? 'text' : 'textSecondary'}>
        Local API: {debugMode ? 'ON' : 'OFF'}
      </ThemedText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    right: Spacing.two,
    bottom: Spacing.two,
    zIndex: 1000,
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.two,
    paddingVertical: Spacing.one,
  },
});
