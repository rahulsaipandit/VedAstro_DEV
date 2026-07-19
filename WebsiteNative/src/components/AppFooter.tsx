import { Pressable, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { Spacing } from '@/constants/theme';
import { PageRoute } from '@/constants/routes';

/**
 * Persistent footer, mounted once at the app root next to AppHeader — replaces the old Blazor
 * MainLayout.razor's "deployment stamp" corner (Made on Earth link, version, Local API debug
 * toggle) and its legal footer (Terms/Privacy links). The old debug toggle was a tiny unlabeled
 * floating badge (`DebugModeToggle.tsx`, bottom-right corner) — reported as impossible to find;
 * folded into a real, clearly-labeled footer row instead of a separate floating element.
 */
export function AppFooter() {
  const theme = useTheme();
  const router = useRouter();
  const debugMode = useAppStore((s) => s.debugMode);
  const setDebugMode = useAppStore((s) => s.setDebugMode);

  return (
    <ThemedView style={[styles.footer, { borderColor: theme.backgroundSelected }]} type="background">
      <ThemedView style={styles.linksRow}>
        <Pressable onPress={() => router.push(`/${PageRoute.TermsOfService}` as never)}>
          <ThemedText type="small" themeColor="textSecondary">
            Terms
          </ThemedText>
        </Pressable>
        <Pressable onPress={() => router.push(`/${PageRoute.PrivacyPolicy}` as never)}>
          <ThemedText type="small" themeColor="textSecondary">
            Privacy
          </ThemedText>
        </Pressable>
        <Pressable onPress={() => router.push(`/${PageRoute.MadeOnEarth}` as never)}>
          <ThemedText type="small" themeColor="textSecondary">
            Made on Earth
          </ThemedText>
        </Pressable>
      </ThemedView>

      <Pressable
        onPress={() => setDebugMode(!debugMode)}
        style={[styles.debugToggle, { borderColor: theme.backgroundSelected }]}>
        <Icon name={debugMode ? 'construction' : 'search'} size={14} color={debugMode ? theme.text : theme.textSecondary} />
        <ThemedText type="small" themeColor={debugMode ? 'text' : 'textSecondary'}>
          Local API: {debugMode ? 'ON' : 'OFF'}
        </ThemedText>
      </Pressable>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  footer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    flexWrap: 'wrap',
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    borderTopWidth: 1,
    gap: Spacing.two,
  },
  linksRow: {
    flexDirection: 'row',
    gap: Spacing.three,
  },
  debugToggle: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.one,
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.two,
    paddingVertical: Spacing.half,
  },
});
