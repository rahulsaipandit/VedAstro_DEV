import { useEffect, useState } from 'react';
import { ActivityIndicator, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { BirthTimeInput, type BirthTimeInputValue } from '@/components/BirthTimeInput';
import { FullDayClock } from '@/components/FullDayClock';
import { useAppStore } from '@/store/useAppStore';
import { showErrorToast } from '@/lib/toast';
import { buildBirthTimeJsonFromWallClock, parseStdTime, type BirthTimeJson } from '@/lib/time';
import { getTimezoneOffsetForLocation, type GeoLocation } from '@/lib/api/geo';
import { getChoghadiyaPeriods, getDailyPanchang, type ChoghadiyaPeriod, type DailyPanchang } from '@/lib/api/festivalCalendar';
import {
  getHoraPeriods,
  getLagnaPeriods,
  getGandMoolPeriods,
  getTithiTransition,
  getNakshatraTransition,
  getYogaTransition,
  getKaranaTransition,
  getVikramSamvatYear,
  getMoonZodiacTransition,
  getSunZodiacSign,
  getPlanetNakshatra,
  getPurnimantaMonth,
  getRitu,
  getAyana,
  getSamvatsaraName,
  getSamvatsaraNameNorth,
  getGujaratiSamvatYear,
  getSakaSamvatYear,
  getKaliSamvatYear,
  getMoonriseTime,
  getMoonsetTime,
  type HoraPeriod,
  type LagnaPeriod,
  type TimeRange,
  type TithiTransition,
  type NakshatraTransition,
  type YogaTransition,
  type KaranaTransition,
  type MoonZodiacTransition,
  type ConstellationInfo,
} from '@/lib/api/muhurat';
import { Spacing } from '@/constants/theme';

const BANGALORE_LOCATION: GeoLocation = { name: 'Bangalore', longitude: 77.5946, latitude: 12.9716 };

function formatTime(time: BirthTimeJson): string {
  const match = /^(\d{2}):(\d{2})/.exec(time.StdTime);
  return match ? `${match[1]}:${match[2]}` : time.StdTime;
}

function formatDate(time: BirthTimeJson): string {
  const match = /(\d{2})\/(\d{2})\/(\d{4})/.exec(time.StdTime);
  return match ? `${match[1]}/${match[2]}/${match[3]}` : time.StdTime;
}

function currentPeriod<T extends { start: BirthTimeJson; end: BirthTimeJson }>(periods: T[], at: Date): T | null {
  return periods.find((p) => parseStdTime(p.start.StdTime) <= at && at < parseStdTime(p.end.StdTime)) ?? null;
}

function isWithinAny(ranges: TimeRange[], at: Date): boolean {
  return ranges.some((r) => parseStdTime(r.start.StdTime) <= at && at < parseStdTime(r.end.StdTime));
}

/** "endTime, +1 day" if it lands on the day after `queriedDate`, else just the time - clearer than the reference app's ">24h" notation for a first pass. */
function formatTransitionEnd(endTime: BirthTimeJson, queriedDate: BirthTimeJson): string {
  const sameDay = formatDate(endTime) === formatDate(queriedDate);
  return sameDay ? formatTime(endTime) : `${formatTime(endTime)} (+1 day)`;
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
 * The detailed "day clock" screen from the reference screenshots: a big radial clock (hour ticks
 * + the 8 classical period names) plus a table where each Panchang limb shows its current value,
 * when it changes, and what it changes to - unlike DailyPanchang's single sunrise-anchored
 * snapshot, or Muhurt.tsx's period-list tabs. See DayClockWidget.tsx for the compact "at a
 * glance" sibling of this same view.
 */
export default function DayClockScreen() {
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());

  const [birthTime, setBirthTime] = useState<BirthTimeInputValue>(blankBirthTime);
  const [loading, setLoading] = useState(false);
  const [date, setDate] = useState<BirthTimeJson | null>(null);

  const [panchang, setPanchang] = useState<DailyPanchang | null>(null);
  const [choghadiya, setChoghadiya] = useState<ChoghadiyaPeriod[] | null>(null);
  const [hora, setHora] = useState<HoraPeriod[] | null>(null);
  const [lagna, setLagna] = useState<LagnaPeriod[] | null>(null);
  const [gandMool, setGandMool] = useState<TimeRange[] | null>(null);
  const [tithiTransition, setTithiTransition] = useState<TithiTransition | null>(null);
  const [nakshatraTransition, setNakshatraTransition] = useState<NakshatraTransition | null>(null);
  const [yogaTransition, setYogaTransition] = useState<YogaTransition | null>(null);
  const [karanaTransition, setKaranaTransition] = useState<KaranaTransition | null>(null);
  const [moonZodiacTransition, setMoonZodiacTransition] = useState<MoonZodiacTransition | null>(null);
  const [sunZodiac, setSunZodiac] = useState<string | null>(null);
  const [suryaNakshatra, setSuryaNakshatra] = useState<ConstellationInfo | null>(null);
  const [purnimantaMonth, setPurnimantaMonth] = useState<string | null>(null);
  const [ritu, setRitu] = useState<string | null>(null);
  const [ayana, setAyana] = useState<string | null>(null);
  const [samvatsara, setSamvatsara] = useState<string | null>(null);
  const [samvatsaraNorth, setSamvatsaraNorth] = useState<string | null>(null);
  const [vikramSamvat, setVikramSamvat] = useState<number | null>(null);
  const [gujaratiSamvat, setGujaratiSamvat] = useState<number | null>(null);
  const [sakaSamvat, setSakaSamvat] = useState<number | null>(null);
  const [kaliSamvat, setKaliSamvat] = useState<number | null>(null);
  const [moonrise, setMoonrise] = useState<BirthTimeJson | null>(null);
  const [moonset, setMoonset] = useState<BirthTimeJson | null>(null);

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
        birthTime.dd, birthTime.mm, birthTime.yyyy, birthTime.hh, birthTime.min, offset, birthTime.location
      );
      if (cancelled) return;
      setDate(time);

      const [
        panchangR, choghadiyaR, horaR, lagnaR, gandMoolR, tithiR, nakshatraR, yogaR, karanaR,
        moonZodiacR, sunZodiacR, suryaNakshatraR, purnimantaR, rituR, ayanaR,
        samvatsaraR, samvatsaraNorthR, vikramR, gujaratiR, sakaR, kaliR, moonriseR, moonsetR,
      ] = await Promise.all([
        getDailyPanchang(apiUrlDirect, time),
        getChoghadiyaPeriods(apiUrlDirect, time),
        getHoraPeriods(apiUrlDirect, time),
        getLagnaPeriods(apiUrlDirect, time),
        getGandMoolPeriods(apiUrlDirect, time, 2),
        getTithiTransition(apiUrlDirect, time),
        getNakshatraTransition(apiUrlDirect, time),
        getYogaTransition(apiUrlDirect, time),
        getKaranaTransition(apiUrlDirect, time),
        getMoonZodiacTransition(apiUrlDirect, time),
        getSunZodiacSign(apiUrlDirect, time),
        getPlanetNakshatra(apiUrlDirect, 'Sun', time),
        getPurnimantaMonth(apiUrlDirect, time),
        getRitu(apiUrlDirect, time),
        getAyana(apiUrlDirect, time),
        getSamvatsaraName(apiUrlDirect, time),
        getSamvatsaraNameNorth(apiUrlDirect, time),
        getVikramSamvatYear(apiUrlDirect, time),
        getGujaratiSamvatYear(apiUrlDirect, time),
        getSakaSamvatYear(apiUrlDirect, time),
        getKaliSamvatYear(apiUrlDirect, time),
        getMoonriseTime(apiUrlDirect, time),
        getMoonsetTime(apiUrlDirect, time),
      ]);

      if (cancelled) return;
      setPanchang(panchangR);
      setChoghadiya(choghadiyaR);
      setHora(horaR);
      setLagna(lagnaR);
      setGandMool(gandMoolR);
      setTithiTransition(tithiR);
      setNakshatraTransition(nakshatraR);
      setYogaTransition(yogaR);
      setKaranaTransition(karanaR);
      setMoonZodiacTransition(moonZodiacR);
      setSunZodiac(sunZodiacR);
      setSuryaNakshatra(suryaNakshatraR);
      setPurnimantaMonth(purnimantaR);
      setRitu(rituR);
      setAyana(ayanaR);
      setSamvatsara(samvatsaraR);
      setSamvatsaraNorth(samvatsaraNorthR);
      setVikramSamvat(vikramR);
      setGujaratiSamvat(gujaratiR);
      setSakaSamvat(sakaR);
      setKaliSamvat(kaliR);
      setMoonrise(moonriseR);
      setMoonset(moonsetR);
    })()
      .catch((e) => showErrorToast(e instanceof Error ? e.message : 'Failed to calculate Day Clock'))
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [apiUrlDirect, birthTime]);

  const markerHour = Number(birthTime.hh || '0') + Number(birthTime.min || '0') / 60;
  const nowInstant = date ? parseStdTime(date.StdTime) : null;
  const nowChoghadiya = choghadiya && nowInstant ? currentPeriod(choghadiya, nowInstant) : null;
  const nowHora = hora && nowInstant ? currentPeriod(hora, nowInstant) : null;
  const nowLagna = lagna && nowInstant ? currentPeriod(lagna, nowInstant) : null;
  const inGandMool = gandMool && nowInstant ? isWithinAny(gandMool, nowInstant) : false;

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Day Clock</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Every Panchang limb&apos;s current value and when it changes, for any date and place.
        </ThemedText>

        <BirthTimeInput apiUrlDirect={apiUrlDirect} value={birthTime} onChange={setBirthTime} />

        {loading && <ActivityIndicator style={styles.loading} />}

        {!loading && panchang && date && (
          <>
            <View style={styles.clockWrap}>
              <FullDayClock sunrise={panchang.sunrise} sunset={panchang.sunset} markerHour={markerHour} />
            </View>

            <ThemedText type="smallBold" style={styles.centered}>{formatDate(date)}</ThemedText>
            <ThemedText type="title" style={[styles.centered, styles.tithiLine]}>
              {panchang.tithiName}, {panchang.paksha} Paksha, {panchang.lunarMonth}
            </ThemedText>
            <ThemedText themeColor="textSecondary" style={styles.centered}>
              Sunrise {formatTime(panchang.sunrise)} · Sunset {formatTime(panchang.sunset)}
            </ThemedText>

            <ThemedView style={styles.list}>
              {tithiTransition && (
                <DetailRow label="Tithi" value={tithiTransition.current.name} end={formatTransitionEnd(tithiTransition.endTime, date)} next={tithiTransition.next.name} />
              )}
              <DetailRow label="Paksha" value={tithiTransition?.current.paksha ?? panchang.paksha} />
              {nakshatraTransition && (
                <DetailRow label="Nakshatra (Chandra)" value={nakshatraTransition.current.name} end={formatTransitionEnd(nakshatraTransition.endTime, date)} next={nakshatraTransition.next.name} />
              )}
              {suryaNakshatra && <DetailRow label="Nakshatra (Surya)" value={suryaNakshatra.name} />}
              {yogaTransition && (
                <DetailRow label="Yoga" value={yogaTransition.current.name} end={formatTransitionEnd(yogaTransition.endTime, date)} next={yogaTransition.next.name} />
              )}
              {karanaTransition && (
                <DetailRow label="Karana" value={karanaTransition.current} end={formatTransitionEnd(karanaTransition.endTime, date)} next={karanaTransition.next} />
              )}
              <DetailRow label="Vaar" value={panchang.vara} />
              <DetailRow label="Month (Amanta)" value={panchang.lunarMonth} />
              {purnimantaMonth && <DetailRow label="Month (Purnimanta)" value={purnimantaMonth} />}
              {moonZodiacTransition && (
                <DetailRow label="Moon Zodiac" value={moonZodiacTransition.current} end={formatTransitionEnd(moonZodiacTransition.endTime, date)} next={moonZodiacTransition.next} />
              )}
              {sunZodiac && <DetailRow label="Sun Zodiac" value={sunZodiac} />}
              {ritu && <DetailRow label="Ritu" value={ritu} />}
              {ayana && <DetailRow label="Ayana" value={ayana} />}
              {samvatsara && <DetailRow label="Samvatsara" value={samvatsara} />}
              {samvatsaraNorth && <DetailRow label="Samvatsara (North)" value={samvatsaraNorth} />}
              {vikramSamvat != null && <DetailRow label="Vikram Samvat" value={`${vikramSamvat}`} />}
              {gujaratiSamvat != null && <DetailRow label="Gujarati Samvat" value={`${gujaratiSamvat}`} />}
              {sakaSamvat != null && <DetailRow label="Saka Samvat" value={`${sakaSamvat}`} />}
              {kaliSamvat != null && <DetailRow label="Kali Samvat" value={`${kaliSamvat}`} />}
              {nowChoghadiya && <DetailRow label="Choghadiya" value={nowChoghadiya.name} end={formatTime(nowChoghadiya.end)} />}
              {nowHora && <DetailRow label="Hora" value={nowHora.lord} end={formatTime(nowHora.end)} />}
              {nowLagna && <DetailRow label="Lagna" value={nowLagna.sign} end={formatTime(nowLagna.end)} />}
              {moonrise && <DetailRow label="Moonrise" value={formatTime(moonrise)} />}
              {moonset && <DetailRow label="Moonset" value={formatTime(moonset)} />}
              {inGandMool && (
                <ThemedText type="smallBold" style={styles.gandMoolText}>
                  Gand Mool
                </ThemedText>
              )}
            </ThemedView>
          </>
        )}
      </ThemedView>
    </ScrollView>
  );
}

function DetailRow({ label, value, end, next }: { label: string; value: string; end?: string; next?: string }) {
  return (
    <View style={styles.detailRow}>
      <ThemedText type="smallBold" style={styles.detailLabel}>{label}</ThemedText>
      <ThemedText style={styles.detailValue}>{value}</ThemedText>
      {end && (
        <ThemedText type="small" themeColor="textSecondary" style={styles.detailEnd}>
          {end}{next ? ` → ${next}` : ''}
        </ThemedText>
      )}
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
    gap: Spacing.three,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  loading: {
    marginVertical: Spacing.four,
  },
  clockWrap: {
    alignItems: 'center',
  },
  centered: {
    textAlign: 'center',
  },
  tithiLine: {
    fontSize: 22,
    lineHeight: 28,
  },
  list: {
    gap: Spacing.two,
    marginTop: Spacing.three,
  },
  detailRow: {
    flexDirection: 'row',
    alignItems: 'baseline',
    flexWrap: 'wrap',
    gap: Spacing.two,
    paddingVertical: Spacing.one,
  },
  detailLabel: {
    minWidth: 90,
  },
  detailValue: {
    flexShrink: 1,
  },
  detailEnd: {
    marginLeft: 'auto',
  },
  gandMoolText: {
    color: '#D64545',
  },
});
