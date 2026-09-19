/**
 * Local, on-device sunrise/sunset reminders - see docs/designHinduCalendar.md's
 * "Sunrise/Sunset reminders" note. Sunrise/sunset drift day to day, so this can't be a single
 * fixed-time OS alarm - instead it schedules a rolling batch of the next DAYS_AHEAD days' worth
 * of notifications and re-schedules the whole batch whenever the toggle or location changes
 * (simpler and more robust than a recurring background task). No backend change needed: this
 * calls the same Calculate.SunriseTime/SunsetTime already exposed via src/lib/api/timeTools.ts.
 *
 * Web has no reliable local-scheduled-notification support (no background alarm), so this is a
 * no-op there - see SunriseSunsetReminderToggle.tsx's platform check.
 */
import * as Notifications from 'expo-notifications';
import { Platform } from 'react-native';

import type { GeoLocation } from '@/lib/api/geo';
import { getSunriseTime, getSunsetTime } from '@/lib/api/timeTools';
import { buildBirthTimeJson, parseStdTime } from '@/lib/time';

const DAYS_AHEAD = 14;

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: false,
    shouldSetBadge: false,
    shouldShowBanner: true,
    shouldShowList: true,
  }),
});

export type ReminderPrefs = { sunrise: boolean; sunset: boolean };

async function scheduleIfInFuture(stdTime: string, title: string, body: string): Promise<void> {
  const at = parseStdTime(stdTime);
  if (at.getTime() <= Date.now()) return; // already passed today - next refresh picks up tomorrow's

  await Notifications.scheduleNotificationAsync({
    content: { title, body },
    trigger: { type: Notifications.SchedulableTriggerInputTypes.DATE, date: at },
  });
}

/**
 * Cancels any previously scheduled batch and, if enabled, schedules the next DAYS_AHEAD days of
 * sunrise/and-or-sunset notifications for `location`. Safe to call whenever the toggle or
 * location changes - always starts from a clean slate since this app schedules nothing else
 * locally.
 */
export async function refreshSunriseSunsetReminders(apiUrlDirect: string, location: GeoLocation, prefs: ReminderPrefs): Promise<void> {
  if (Platform.OS === 'web') return;

  await Notifications.cancelAllScheduledNotificationsAsync();
  if (!prefs.sunrise && !prefs.sunset) return;

  const permission = await Notifications.requestPermissionsAsync();
  if (permission.status !== 'granted') return;

  const today = new Date();

  for (let d = 0; d < DAYS_AHEAD; d++) {
    const day = new Date(Date.UTC(today.getUTCFullYear(), today.getUTCMonth(), today.getUTCDate() + d));
    const time = buildBirthTimeJson(day, 0, location);

    if (prefs.sunrise) {
      const sunrise = await getSunriseTime(apiUrlDirect, time);
      await scheduleIfInFuture(sunrise, 'Sunrise', `Sunrise in ${location.name} is now`);
    }
    if (prefs.sunset) {
      const sunset = await getSunsetTime(apiUrlDirect, time);
      await scheduleIfInFuture(sunset, 'Sunset', `Sunset in ${location.name} is now`);
    }
  }
}

export async function cancelSunriseSunsetReminders(): Promise<void> {
  if (Platform.OS === 'web') return;
  await Notifications.cancelAllScheduledNotificationsAsync();
}
