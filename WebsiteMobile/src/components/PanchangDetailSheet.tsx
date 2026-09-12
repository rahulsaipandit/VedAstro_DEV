import { useEffect, useState } from 'react';
import { ActivityIndicator, Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { showErrorToast } from '@/lib/toast';
import type { BirthTimeJson } from '@/lib/time';
import {
  getChoghadiyaPeriods,
  getDailyPanchang,
  type ChoghadiyaPeriod,
  type ChoghadiyaQuality,
  type DailyPanchang,
} from '@/lib/api/festivalCalendar';

type Mode = 'choghadiya' | 'traditional';

const QUALITY_COLOR: Record<ChoghadiyaQuality, string> = {
  Auspicious: '#1f9d55',
  Neutral: '#9AA0AC',
  Inauspicious: '#D64545',
};

function formatTime(time: BirthTimeJson): string {
  const match = /^(\d{2}):(\d{2})/.exec(time.StdTime);
  return match ? `${match[1]}:${match[2]}` : time.StdTime;
}

/**
 * Day-detail Panchang panel, opened by tapping a festival on FestivalCalendar.tsx. Two
 * user-selectable views of the same day, backed by Calculate.ChoghadiyaPeriods and
 * Calculate.DailyPanchang respectively (Library/Logic/Calculate/Panchang.cs) - see that file's
 * doc comment for how each compares to its reference implementation
 * (github.com/vishalnagda1/choghadiya, github.com/Vedic-Panchanga/Shri-Jagannath-Panchang).
 */
export function PanchangDetailSheet({
  visible,
  onClose,
  apiUrlDirect,
  date,
  title,
}: {
  visible: boolean;
  onClose: () => void;
  apiUrlDirect: string;
  date: BirthTimeJson | null;
  title: string;
}) {
  const theme = useTheme();
  const [mode, setMode] = useState<Mode>('choghadiya');
  const [loading, setLoading] = useState(false);
  const [periods, setPeriods] = useState<ChoghadiyaPeriod[] | null>(null);
  const [panchang, setPanchang] = useState<DailyPanchang | null>(null);

  useEffect(() => {
    if (!visible || !date) return;
    setLoading(true);
    setPeriods(null);
    setPanchang(null);

    const load = mode === 'choghadiya' ? getChoghadiyaPeriods(apiUrlDirect, date).then(setPeriods) : getDailyPanchang(apiUrlDirect, date).then(setPanchang);

    load.catch((e) => showErrorToast(e instanceof Error ? e.message : 'Failed to load Panchang')).finally(() => setLoading(false));
  }, [visible, date, mode, apiUrlDirect]);

  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <Pressable style={styles.backdrop} onPress={onClose} />
      <ThemedView style={[styles.sheet, { backgroundColor: theme.background }]}>
        <View style={[styles.handle, { backgroundColor: theme.backgroundSelected }]} />
        <View style={styles.header}>
          <ThemedText type="smallBold">{title}</ThemedText>
          <Pressable onPress={onClose} hitSlop={8} style={[styles.closeButton, { backgroundColor: theme.backgroundElement }]}>
            <Icon name="close" size={16} color={theme.textSecondary} />
          </Pressable>
        </View>

        <View style={styles.modeRow}>
          {(['choghadiya', 'traditional'] as const).map((m) => (
            <Pressable key={m} onPress={() => setMode(m)} style={[styles.modeChip, mode === m && styles.modeChipActive]}>
              <ThemedText type="small" themeColor={mode === m ? 'background' : 'text'}>
                {m === 'choghadiya' ? 'Choghadiya' : 'Traditional Panchang'}
              </ThemedText>
            </Pressable>
          ))}
        </View>

        <ScrollView contentContainerStyle={styles.content}>
          {loading && <ActivityIndicator size="small" />}

          {!loading && mode === 'choghadiya' && periods && (
            <>
              <ThemedText type="small" themeColor="textSecondary">
                Day
              </ThemedText>
              {periods
                .filter((p) => p.isDayPeriod)
                .map((p, i) => (
                  <PeriodRow key={`day-${i}`} period={p} />
                ))}
              <ThemedText type="small" themeColor="textSecondary" style={styles.nightLabel}>
                Night
              </ThemedText>
              {periods
                .filter((p) => !p.isDayPeriod)
                .map((p, i) => (
                  <PeriodRow key={`night-${i}`} period={p} />
                ))}
            </>
          )}

          {!loading && mode === 'traditional' && panchang && (
            <View style={styles.panchangGrid}>
              <PanchangRow label="Tithi" value={`${panchang.tithiName} (${panchang.paksha} Paksha)`} />
              <PanchangRow label="Lunar Month" value={panchang.lunarMonth} />
              <PanchangRow label="Vara (Weekday)" value={panchang.vara} />
              <PanchangRow label="Nakshatra" value={panchang.nakshatra} />
              <PanchangRow label="Yoga" value={panchang.yoga} />
              <PanchangRow label="Karana" value={panchang.karana} />
              <PanchangRow label="Sunrise" value={formatTime(panchang.sunrise)} />
              <PanchangRow label="Sunset" value={formatTime(panchang.sunset)} />
            </View>
          )}
        </ScrollView>
      </ThemedView>
    </Modal>
  );
}

function PeriodRow({ period }: { period: ChoghadiyaPeriod }) {
  return (
    <View style={styles.periodRow}>
      <View style={[styles.qualityDot, { backgroundColor: QUALITY_COLOR[period.quality] }]} />
      <ThemedText style={styles.periodName}>{period.name}</ThemedText>
      <ThemedText type="small" themeColor="textSecondary">
        {formatTime(period.start)} - {formatTime(period.end)}
      </ThemedText>
    </View>
  );
}

function PanchangRow({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.panchangRow}>
      <ThemedText type="small" themeColor="textSecondary">
        {label}
      </ThemedText>
      <ThemedText type="smallBold">{value}</ThemedText>
    </View>
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
    maxHeight: '80%',
    borderTopLeftRadius: 20,
    borderTopRightRadius: 20,
    paddingHorizontal: Spacing.four,
    paddingTop: Spacing.two,
    paddingBottom: Spacing.six,
    gap: Spacing.three,
  },
  handle: {
    width: 36,
    height: 4,
    borderRadius: 3,
    alignSelf: 'center',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  closeButton: {
    width: 30,
    height: 30,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  modeRow: {
    flexDirection: 'row',
    gap: Spacing.two,
  },
  modeChip: {
    borderRadius: 999,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.one,
    backgroundColor: '#00000010',
  },
  modeChipActive: {
    backgroundColor: '#0d6efd',
  },
  content: {
    gap: Spacing.two,
    paddingBottom: Spacing.four,
  },
  nightLabel: {
    marginTop: Spacing.two,
  },
  periodRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
    paddingVertical: Spacing.one,
  },
  qualityDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
  },
  periodName: {
    flex: 1,
  },
  panchangGrid: {
    gap: Spacing.two,
  },
  panchangRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: Spacing.one,
  },
});
