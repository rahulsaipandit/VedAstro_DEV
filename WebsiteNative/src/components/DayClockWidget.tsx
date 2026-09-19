import { useEffect, useState } from 'react';
import { StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { FullDayClock } from './FullDayClock';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import type { BirthTimeJson } from '@/lib/time';
import { parseStdTime } from '@/lib/time';
import { getChoghadiyaPeriods, getDailyPanchang, type ChoghadiyaPeriod, type ChoghadiyaQuality, type DailyPanchang } from '@/lib/api/festivalCalendar';
import { getDurMuhurtaPeriods, getGandMoolPeriods, type TimeRange } from '@/lib/api/muhurat';
import { showErrorToast } from '@/lib/toast';

const QUALITY_COLOR: Record<ChoghadiyaQuality, string> = {
  Auspicious: '#1f9d55',
  Neutral: '#9AA0AC',
  Inauspicious: '#D64545',
};

function formatTime(time: BirthTimeJson): string {
  const match = /^(\d{2}):(\d{2})/.exec(time.StdTime);
  return match ? `${match[1]}:${match[2]}` : time.StdTime;
}

// The location's own local hour, read straight from the StdTime text - not derived from the
// parsed instant's getUTCHours(), which would be shifted by the location's own UTC offset.
function hourOfDay(time: BirthTimeJson): number {
  const match = /^(\d{2}):(\d{2})/.exec(time.StdTime);
  if (!match) return 0;
  return Number(match[1]) + Number(match[2]) / 60;
}

function currentPeriod<T extends { start: BirthTimeJson; end: BirthTimeJson }>(periods: T[], at: Date): T | null {
  return periods.find((p) => parseStdTime(p.start.StdTime) <= at && at < parseStdTime(p.end.StdTime)) ?? null;
}

function isWithinAny(ranges: TimeRange[], at: Date): boolean {
  return ranges.some((r) => parseStdTime(r.start.StdTime) <= at && at < parseStdTime(r.end.StdTime));
}

/**
 * The compact "at a glance" day-clock card from the reference screenshots (the small widget with
 * just the current time, Tithi, current Choghadiya + Dur Muhurta, and Chaturmas/Gand Mool status)
 * - a condensed sibling of DayClock.tsx's full detail screen, meant to read in one glance rather
 * than being tapped through. Fetches its own data for `date`'s calendar day.
 */
export function DayClockWidget({ apiUrlDirect, date }: { apiUrlDirect: string; date: BirthTimeJson }) {
  const theme = useTheme();
  const [panchang, setPanchang] = useState<DailyPanchang | null>(null);
  const [choghadiya, setChoghadiya] = useState<ChoghadiyaPeriod[] | null>(null);
  const [durMuhurta, setDurMuhurta] = useState<TimeRange[] | null>(null);
  const [gandMool, setGandMool] = useState<TimeRange[] | null>(null);

  useEffect(() => {
    let cancelled = false;

    Promise.all([
      getDailyPanchang(apiUrlDirect, date),
      getChoghadiyaPeriods(apiUrlDirect, date),
      getDurMuhurtaPeriods(apiUrlDirect, date),
      getGandMoolPeriods(apiUrlDirect, date, 2),
    ])
      .then(([panchangResult, choghadiyaResult, durMuhurtaResult, gandMoolResult]) => {
        if (cancelled) return;
        setPanchang(panchangResult);
        setChoghadiya(choghadiyaResult);
        setDurMuhurta(durMuhurtaResult);
        setGandMool(gandMoolResult);
      })
      .catch((e) => showErrorToast(e instanceof Error ? e.message : 'Failed to load day clock'));

    return () => {
      cancelled = true;
    };
  }, [apiUrlDirect, date]);

  if (!panchang || !choghadiya || !durMuhurta || !gandMool) return null;

  const at = parseStdTime(date.StdTime);
  const markerHour = hourOfDay(date);
  const nowChoghadiya = currentPeriod(choghadiya, at);
  const nowDurMuhurta = currentPeriod(durMuhurta, at);
  const inGandMool = isWithinAny(gandMool, at);

  return (
    <ThemedView style={[styles.card, { borderColor: theme.backgroundSelected }]}>
      <View style={styles.clockWrap}>
        <FullDayClock sunrise={panchang.sunrise} sunset={panchang.sunset} markerHour={markerHour} size={220} showLabels={false} />
        <ThemedText type="title" style={styles.timeOverlay}>
          {formatTime(date)}
        </ThemedText>
      </View>

      <ThemedText type="smallBold" style={styles.tithiLine}>
        {panchang.tithiName}, {panchang.paksha} Paksha, {panchang.lunarMonth}
      </ThemedText>

      {nowChoghadiya && (
        <ThemedText type="small" style={{ color: QUALITY_COLOR[nowChoghadiya.quality] }}>
          {nowChoghadiya.name} {formatTime(nowChoghadiya.start)} - {formatTime(nowChoghadiya.end)}
        </ThemedText>
      )}

      {nowDurMuhurta && (
        <View style={styles.durMuhurtaRow}>
          <Icon />
          <ThemedText type="small" themeColor="textSecondary">
            Dur Muhurt {formatTime(nowDurMuhurta.start)} - {formatTime(nowDurMuhurta.end)}
          </ThemedText>
        </View>
      )}

      {inGandMool && (
        <ThemedText type="small" style={styles.gandMoolText}>
          Gand Mool
        </ThemedText>
      )}
    </ThemedView>
  );
}

function Icon() {
  return <ThemedText type="small" themeColor="textSecondary">→</ThemedText>;
}

const styles = StyleSheet.create({
  card: {
    borderWidth: 1,
    borderRadius: 16,
    padding: Spacing.four,
    alignItems: 'center',
    gap: Spacing.one,
  },
  clockWrap: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  timeOverlay: {
    position: 'absolute',
    fontSize: 24,
  },
  tithiLine: {
    marginTop: Spacing.two,
    textAlign: 'center',
  },
  durMuhurtaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.half,
  },
  gandMoolText: {
    color: '#D64545',
  },
});
