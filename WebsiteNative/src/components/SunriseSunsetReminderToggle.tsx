import { useEffect } from 'react';
import { Platform, StyleSheet, Switch, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { Spacing } from '@/constants/theme';
import type { GeoLocation } from '@/lib/api/geo';
import { refreshSunriseSunsetReminders } from '@/lib/notifications/sunriseSunsetReminders';
import { showErrorToast } from '@/lib/toast';

/**
 * Sunrise/sunset local-notification toggle - see docs/designHinduCalendar.md's "Sunrise/Sunset
 * reminders" note. Placed on Muhurt.tsx (the Panchang home screen) since that's where a
 * location is already in hand. Re-schedules the whole rolling batch whenever a toggle or the
 * location changes.
 */
export function SunriseSunsetReminderToggle({ apiUrlDirect, location }: { apiUrlDirect: string; location: GeoLocation }) {
  const theme = useTheme();
  const remindersEnabled = useAppStore((s) => s.remindersEnabled);
  const setRemindersEnabled = useAppStore((s) => s.setRemindersEnabled);

  useEffect(() => {
    // refreshSunriseSunsetReminders always cancels the previous batch first, so this also
    // correctly clears everything when both toggles are off.
    refreshSunriseSunsetReminders(apiUrlDirect, location, remindersEnabled).catch((e) =>
      showErrorToast(e instanceof Error ? e.message : 'Failed to schedule reminders')
    );
    // location is an object literal recreated on every render upstream - depend on its identity
    // fields instead, or this would re-schedule (and re-request permissions) every render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiUrlDirect, location.name, location.latitude, location.longitude, remindersEnabled.sunrise, remindersEnabled.sunset]);

  function toggle(key: 'sunrise' | 'sunset', value: boolean) {
    setRemindersEnabled({ ...remindersEnabled, [key]: value });
  }

  return (
    <ThemedView style={[styles.card, { borderColor: theme.backgroundSelected }]}>
      <ThemedText type="smallBold">Daily reminders</ThemedText>
      {Platform.OS === 'web' && (
        <ThemedText type="small" themeColor="textSecondary">
          Reminders need the iOS/Android app - not available in a browser.
        </ThemedText>
      )}
      <View style={styles.row}>
        <ThemedText>Remind me at sunrise</ThemedText>
        <Switch value={remindersEnabled.sunrise} onValueChange={(v) => toggle('sunrise', v)} disabled={Platform.OS === 'web'} />
      </View>
      <View style={styles.row}>
        <ThemedText>Remind me at sunset</ThemedText>
        <Switch value={remindersEnabled.sunset} onValueChange={(v) => toggle('sunset', v)} disabled={Platform.OS === 'web'} />
      </View>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  card: {
    borderWidth: 1,
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.two,
  },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
});
