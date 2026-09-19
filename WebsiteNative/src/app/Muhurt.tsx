import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { BirthTimeInput, type BirthTimeInputValue } from '@/components/BirthTimeInput';
import { RadialDayClock } from '@/components/RadialDayClock';
import { SunriseSunsetReminderToggle } from '@/components/SunriseSunsetReminderToggle';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { showErrorToast } from '@/lib/toast';
import { buildBirthTimeJsonFromWallClock, type BirthTimeJson } from '@/lib/time';
import { getTimezoneOffsetForLocation, type GeoLocation } from '@/lib/api/geo';
import { getChoghadiyaPeriods, getDailyPanchang, type ChoghadiyaPeriod, type ChoghadiyaQuality, type DailyPanchang } from '@/lib/api/festivalCalendar';
import {
  getHoraPeriods,
  getLagnaPeriods,
  getRahuKaal,
  getGulikaKaal,
  getYamagandaKaal,
  getGandMoolPeriods,
  type HoraPeriod,
  type LagnaPeriod,
  type TimeRange,
} from '@/lib/api/muhurat';
import { Spacing } from '@/constants/theme';

const BANGALORE_LOCATION: GeoLocation = { name: 'Bangalore', longitude: 77.5946, latitude: 12.9716 };

const QUALITY_COLOR: Record<ChoghadiyaQuality, string> = {
  Auspicious: '#1f9d55',
  Neutral: '#9AA0AC',
  Inauspicious: '#D64545',
};

const TABS = ['Panchang', 'Choghadiya', 'Hora', 'Lagna', 'Kaal Vela', 'Gand Mool'] as const;
type Tab = (typeof TABS)[number];

function formatTime(time: BirthTimeJson): string {
  const match = /^(\d{2}):(\d{2})/.exec(time.StdTime);
  return match ? `${match[1]}:${match[2]}` : time.StdTime;
}

function formatDateTime(time: BirthTimeJson): string {
  const match = /^(\d{2}:\d{2}) (\d{2}\/\d{2}\/\d{4})/.exec(time.StdTime);
  return match ? `${match[2]} ${match[1]}` : time.StdTime;
}

function blankBirthTime(): BirthTimeInputValue {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return {
    dd: pad(now.getDate()),
    mm: pad(now.getMonth() + 1),
    yyyy: String(now.getFullYear()),
    hh: pad(now.getHours()),
    min: pad(now.getMinutes()),
    location: BANGALORE_LOCATION,
  };
}

/**
 * Panchang/Muhurt home screen: a radial day-clock plus tabbed access to every per-day
 * auspicious-timing view this app computes - Choghadiya (existing), and the newer
 * Hora/Lagna/Kaal-Vela/Gand-Mool calculators added to
 * Library/Logic/Calculate/Panchang.cs alongside it. See docs/designHinduCalendar.md
 * section 4 for why this exists as its own screen rather than folding into FestivalCalendar.tsx.
 */
export default function MuhurtScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());

  const [birthTime, setBirthTime] = useState<BirthTimeInputValue>(blankBirthTime);
  const [tab, setTab] = useState<Tab>('Panchang');
  const [loading, setLoading] = useState(false);
  const [date, setDate] = useState<BirthTimeJson | null>(null);

  const [panchang, setPanchang] = useState<DailyPanchang | null>(null);
  const [choghadiya, setChoghadiya] = useState<ChoghadiyaPeriod[] | null>(null);
  const [hora, setHora] = useState<HoraPeriod[] | null>(null);
  const [lagna, setLagna] = useState<LagnaPeriod[] | null>(null);
  const [rahuKaal, setRahuKaal] = useState<TimeRange | null>(null);
  const [gulikaKaal, setGulikaKaal] = useState<TimeRange | null>(null);
  const [yamagandaKaal, setYamagandaKaal] = useState<TimeRange | null>(null);
  const [gandMool, setGandMool] = useState<TimeRange[] | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);

    (async () => {
      const offset = await getTimezoneOffsetForLocation(
        apiUrlDirect,
        birthTime.location,
        new Date(Date.UTC(Number(birthTime.yyyy), Number(birthTime.mm) - 1, Number(birthTime.dd)))
      );
      const time = buildBirthTimeJsonFromWallClock(
        birthTime.dd,
        birthTime.mm,
        birthTime.yyyy,
        birthTime.hh,
        birthTime.min,
        offset,
        birthTime.location
      );
      if (cancelled) return;
      setDate(time);

      const [panchangResult, choghadiyaResult, horaResult, lagnaResult, rahuResult, gulikaResult, yamagandaResult, gandMoolResult] = await Promise.all([
        getDailyPanchang(apiUrlDirect, time),
        getChoghadiyaPeriods(apiUrlDirect, time),
        getHoraPeriods(apiUrlDirect, time),
        getLagnaPeriods(apiUrlDirect, time),
        getRahuKaal(apiUrlDirect, time),
        getGulikaKaal(apiUrlDirect, time),
        getYamagandaKaal(apiUrlDirect, time),
        getGandMoolPeriods(apiUrlDirect, time, 30),
      ]);

      if (cancelled) return;
      setPanchang(panchangResult);
      setChoghadiya(choghadiyaResult);
      setHora(horaResult);
      setLagna(lagnaResult);
      setRahuKaal(rahuResult);
      setGulikaKaal(gulikaResult);
      setYamagandaKaal(yamagandaResult);
      setGandMool(gandMoolResult);
    })()
      .catch((e) => showErrorToast(e instanceof Error ? e.message : 'Failed to calculate Muhurt data'))
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [apiUrlDirect, birthTime]);

  const markerHour = Number(birthTime.hh || '0') + Number(birthTime.min || '0') / 60;

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Muhurt</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Choghadiya, Hora, Lagna and the day&apos;s inauspicious Kaal windows, for any date and place.
        </ThemedText>

        <BirthTimeInput apiUrlDirect={apiUrlDirect} value={birthTime} onChange={setBirthTime} />

        {loading && <ActivityIndicator style={styles.loading} />}

        {!loading && panchang && (
          <ThemedView style={styles.clockRow}>
            <RadialDayClock sunrise={panchang.sunrise} sunset={panchang.sunset} markerHour={markerHour} />
            <ThemedView style={styles.clockLegend}>
              <PanchangRow label="Tithi" value={`${panchang.tithiName} (${panchang.paksha} Paksha)`} />
              <PanchangRow label="Nakshatra" value={panchang.nakshatra} />
              <PanchangRow label="Yoga" value={panchang.yoga} />
              <PanchangRow label="Sunrise" value={formatTime(panchang.sunrise)} />
              <PanchangRow label="Sunset" value={formatTime(panchang.sunset)} />
            </ThemedView>
          </ThemedView>
        )}

        <View style={styles.tabRow}>
          {TABS.map((t) => (
            <Pressable key={t} onPress={() => setTab(t)} style={[styles.tabChip, tab === t && styles.tabChipActive, { borderColor: theme.backgroundSelected }]}>
              <ThemedText type="small" themeColor={tab === t ? 'background' : 'text'}>
                {t}
              </ThemedText>
            </Pressable>
          ))}
        </View>

        {!loading && tab === 'Panchang' && panchang && (
          <ThemedView style={styles.list}>
            <PanchangRow label="Lunar Month" value={panchang.lunarMonth} />
            <PanchangRow label="Vara (Weekday)" value={panchang.vara} />
            <PanchangRow label="Karana" value={panchang.karana} />
          </ThemedView>
        )}

        {!loading && tab === 'Choghadiya' && choghadiya && (
          <ThemedView style={styles.list}>
            <ThemedText type="small" themeColor="textSecondary">Day</ThemedText>
            {choghadiya.filter((p) => p.isDayPeriod).map((p, i) => (
              <View key={`day-${i}`} style={styles.periodRow}>
                <View style={[styles.qualityDot, { backgroundColor: QUALITY_COLOR[p.quality] }]} />
                <ThemedText style={styles.periodName}>{p.name}</ThemedText>
                <ThemedText type="small" themeColor="textSecondary">{formatTime(p.start)} - {formatTime(p.end)}</ThemedText>
              </View>
            ))}
            <ThemedText type="small" themeColor="textSecondary" style={styles.sectionSpacing}>Night</ThemedText>
            {choghadiya.filter((p) => !p.isDayPeriod).map((p, i) => (
              <View key={`night-${i}`} style={styles.periodRow}>
                <View style={[styles.qualityDot, { backgroundColor: QUALITY_COLOR[p.quality] }]} />
                <ThemedText style={styles.periodName}>{p.name}</ThemedText>
                <ThemedText type="small" themeColor="textSecondary">{formatTime(p.start)} - {formatTime(p.end)}</ThemedText>
              </View>
            ))}
          </ThemedView>
        )}

        {!loading && tab === 'Hora' && hora && (
          <ThemedView style={styles.list}>
            <ThemedText type="small" themeColor="textSecondary">Day</ThemedText>
            {hora.filter((p) => p.isDayPeriod).map((p, i) => (
              <View key={`day-${i}`} style={styles.periodRow}>
                <ThemedText style={styles.periodName}>{p.lord}</ThemedText>
                <ThemedText type="small" themeColor="textSecondary">{formatTime(p.start)} - {formatTime(p.end)}</ThemedText>
              </View>
            ))}
            <ThemedText type="small" themeColor="textSecondary" style={styles.sectionSpacing}>Night</ThemedText>
            {hora.filter((p) => !p.isDayPeriod).map((p, i) => (
              <View key={`night-${i}`} style={styles.periodRow}>
                <ThemedText style={styles.periodName}>{p.lord}</ThemedText>
                <ThemedText type="small" themeColor="textSecondary">{formatTime(p.start)} - {formatTime(p.end)}</ThemedText>
              </View>
            ))}
          </ThemedView>
        )}

        {!loading && tab === 'Lagna' && lagna && (
          <ThemedView style={styles.list}>
            {lagna.map((p, i) => (
              <View key={i} style={styles.periodRow}>
                <ThemedText style={styles.periodName}>{p.sign}</ThemedText>
                <ThemedText type="small" themeColor="textSecondary">{formatTime(p.start)} - {formatTime(p.end)}</ThemedText>
              </View>
            ))}
          </ThemedView>
        )}

        {!loading && tab === 'Kaal Vela' && rahuKaal && gulikaKaal && yamagandaKaal && (
          <ThemedView style={styles.list}>
            <ThemedText themeColor="textSecondary" style={styles.subtitle}>
              These windows are always inauspicious - avoid starting anything important during them.
            </ThemedText>
            <View style={styles.periodRow}>
              <ThemedText style={styles.periodName}>Rahu Kaal</ThemedText>
              <ThemedText type="small" themeColor="textSecondary">{formatTime(rahuKaal.start)} - {formatTime(rahuKaal.end)}</ThemedText>
            </View>
            <View style={styles.periodRow}>
              <ThemedText style={styles.periodName}>Gulika Kaal</ThemedText>
              <ThemedText type="small" themeColor="textSecondary">{formatTime(gulikaKaal.start)} - {formatTime(gulikaKaal.end)}</ThemedText>
            </View>
            <View style={styles.periodRow}>
              <ThemedText style={styles.periodName}>Yamaganda Kaal</ThemedText>
              <ThemedText type="small" themeColor="textSecondary">{formatTime(yamagandaKaal.start)} - {formatTime(yamagandaKaal.end)}</ThemedText>
            </View>
          </ThemedView>
        )}

        {!loading && tab === 'Gand Mool' && gandMool && (
          <ThemedView style={styles.list}>
            <ThemedText themeColor="textSecondary" style={styles.subtitle}>
              Moon transits of the 6 Gand Mool nakshatras in the next 30 days.
            </ThemedText>
            {gandMool.length === 0 && <ThemedText type="small" themeColor="textSecondary">None in this window.</ThemedText>}
            {gandMool.map((p, i) => (
              <View key={i} style={styles.periodRow}>
                <ThemedText type="small">{formatDateTime(p.start)} - {formatDateTime(p.end)}</ThemedText>
              </View>
            ))}
          </ThemedView>
        )}

        {date && <SunriseSunsetReminderToggle apiUrlDirect={apiUrlDirect} location={birthTime.location} />}
      </ThemedView>
    </ScrollView>
  );
}

function PanchangRow({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.panchangRow}>
      <ThemedText type="small" themeColor="textSecondary">{label}</ThemedText>
      <ThemedText type="smallBold">{value}</ThemedText>
    </View>
  );
}

const styles = StyleSheet.create({
  scrollContent: {
    alignItems: 'center',
  },
  page: {
    width: '100%',
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    paddingBottom: Spacing.six,
    gap: Spacing.four,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  loading: {
    marginVertical: Spacing.four,
  },
  clockRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    gap: Spacing.four,
  },
  clockLegend: {
    flex: 1,
    minWidth: 180,
    gap: Spacing.one,
  },
  panchangRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    paddingVertical: Spacing.half,
  },
  tabRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.two,
  },
  tabChip: {
    borderWidth: 1,
    borderRadius: 999,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.one,
  },
  tabChipActive: {
    backgroundColor: '#0d6efd',
    borderColor: '#0d6efd',
  },
  list: {
    gap: Spacing.two,
  },
  sectionSpacing: {
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
});
