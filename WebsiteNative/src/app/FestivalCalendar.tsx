import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { GeoLocationInput } from '@/components/GeoLocationInput';
import { MoonPhaseIcon } from '@/components/MoonPhaseIcon';
import { PanchangDetailSheet } from '@/components/PanchangDetailSheet';
import { MyTithiTab } from '@/components/MyTithiTab';
import { Icon } from '@/components/Icon';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { showErrorToast } from '@/lib/toast';
import { parseStdTime, type BirthTimeJson } from '@/lib/time';
import { getFestivalCalendar, FESTIVAL_NAMES, type FestivalCalendarResult, type FestivalName } from '@/lib/api/festivalCalendar';
import {
  getEkadashiCalendar,
  getPurnimaVratCalendar,
  getAmavasyaVratCalendar,
  getPradoshVratCalendar,
  getSankashtiChaturthiCalendar,
  getPitruPakshaPeriod,
  getJyeshthaGauriDates,
  getChaturmasPeriod,
  getSankrantiCalendar,
  type TimeRange,
} from '@/lib/api/muhurat';
import { geoLocationToUrl, type GeoLocation } from '@/lib/api/geo';
import { Spacing } from '@/constants/theme';

const TABS = ['Festivals', 'My Tithi', 'Vrat', 'Sankranti'] as const;
type Tab = (typeof TABS)[number];

const FESTIVAL_LABELS: Record<FestivalName, string> = {
  Ramnavami: 'Ramnavami',
  Holi: 'Holi',
  Rakshabandhan: 'Raksha Bandhan',
  KarwaChauth: 'Karwa Chauth',
  Chhath: 'Chhath',
  Diwali: 'Diwali (Deepawali)',
  MahaShivaratri: 'Maha Shivaratri',
  MakarSankranti: 'Makar Sankranti',
};

// Matches the tithi each festival is computed against in Calculate.FestivalDate
// (Library/Logic/Calculate/FestivalCalendar.cs) - undefined for Makar Sankranti, a purely solar
// event with no tithi/moon-phase of its own.
const FESTIVAL_TITHI: Partial<Record<FestivalName, number>> = {
  Ramnavami: 9,
  Holi: 16,
  Rakshabandhan: 15,
  KarwaChauth: 19,
  Chhath: 6,
  Diwali: 30,
  MahaShivaratri: 29,
};

const SEATTLE_LOCATION: GeoLocation = { name: 'Seattle', longitude: -122.3321, latitude: 47.6062 };

function formatDate(time: BirthTimeJson): string {
  const match = /(\d{2})\/(\d{2})\/(\d{4})/.exec(time.StdTime);
  return match ? `${match[1]}/${match[2]}/${match[3]}` : time.StdTime;
}

function dateSortKey(time: BirthTimeJson): number {
  const match = /^(\d{2}):(\d{2}) (\d{2})\/(\d{2})\/(\d{4})/.exec(time.StdTime);
  if (!match) return 0;
  const [, hh, mm, dd, mo, yyyy] = match;
  return Date.UTC(Number(yyyy), Number(mo) - 1, Number(dd), Number(hh), Number(mm));
}

const MS_PER_DAY = 24 * 60 * 60 * 1000;

/**
 * "in N days" / "today" / "N days ago" - countdown relative to `referenceNow` (captured once, in
 * handleCalculate, not a picked reference date). Takes the timestamp as a parameter rather than
 * calling Date.now() itself, since this is called during render (from JSX) and Date.now() there
 * would make the component impure - see referenceNow's own comment.
 */
function daysFromNow(time: BirthTimeJson, referenceNow: number): string {
  const diffDays = Math.round((parseStdTime(time.StdTime).getTime() - referenceNow) / MS_PER_DAY);
  if (diffDays === 0) return 'today';
  if (diffDays > 0) return `in ${diffDays} day${diffDays === 1 ? '' : 's'}`;
  return `${-diffDays} day${diffDays === -1 ? '' : 's'} ago`;
}

type VratItem = {
  key: string;
  label: string;
  date: BirthTimeJson;
  endDate?: BirthTimeJson;
  subItems?: { label: string; date: BirthTimeJson }[];
};

/**
 * Screen for combined Vrat & minor-festival browsing - see docs/designHinduCalendar.md section
 * "4c" for the calculators this pulls from. Countdown chip ("in N days") and multi-day ranged
 * events (Ganesh Chaturthi-Anant Chaturdashi, Jyeshtha Gauri's 3 nakshatra days) match the
 * reference screenshot's layout.
 */
export default function FestivalCalendarScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const currentYear = new Date().getFullYear();

  const [year, setYear] = useState(String(currentYear));
  const [location, setLocation] = useState<GeoLocation>(SEATTLE_LOCATION);
  const [loading, setLoading] = useState(false);
  const [tab, setTab] = useState<Tab>('Festivals');
  const [calendar, setCalendar] = useState<FestivalCalendarResult | null>(null);
  const [vratItems, setVratItems] = useState<VratItem[] | null>(null);
  const [chaturmas, setChaturmas] = useState<TimeRange | null>(null);
  const [sankranti, setSankranti] = useState<{ rashi: string; date: BirthTimeJson }[] | null>(null);
  const [selected, setSelected] = useState<{ label: string; date: BirthTimeJson } | null>(null);
  // Captured once per Calculate press (an event handler, not render) rather than called from
  // render/JSX - see daysFromNow's own comment on why Date.now() can't be called there directly.
  const [referenceNow, setReferenceNow] = useState<number | null>(null);

  async function handleCalculate() {
    const parsedYear = parseInt(year, 10);
    if (!parsedYear || year.length !== 4) {
      showErrorToast('Please enter a valid 4-digit year');
      return;
    }
    const now = Date.now();
    setLoading(true);
    setCalendar(null);
    setVratItems(null);
    setChaturmas(null);
    setSankranti(null);
    setReferenceNow(now);
    try {
      const [
        festivals, ekadashis, purnima, amavasya, pradosh, sankashti, pitruPaksha, jyeshthaGauri,
        hartalikaTeej, ganeshChaturthi, anantChaturdashi, chaturmasPeriod, sankrantiCalendar,
      ] = await Promise.all([
        getFestivalCalendar(apiUrlDirect, parsedYear, location),
        getEkadashiCalendar(apiUrlDirect, parsedYear, location),
        getPurnimaVratCalendar(apiUrlDirect, parsedYear, location),
        getAmavasyaVratCalendar(apiUrlDirect, parsedYear, location),
        getPradoshVratCalendar(apiUrlDirect, parsedYear, location),
        getSankashtiChaturthiCalendar(apiUrlDirect, parsedYear, location),
        getPitruPakshaPeriod(apiUrlDirect, parsedYear, location),
        getJyeshthaGauriDates(apiUrlDirect, parsedYear, location),
        getFestivalDateSafe(apiUrlDirect, 'HartalikaTeej', parsedYear, location),
        getFestivalDateSafe(apiUrlDirect, 'GaneshChaturthi', parsedYear, location),
        getFestivalDateSafe(apiUrlDirect, 'AnantChaturdashi', parsedYear, location),
        getChaturmasPeriod(apiUrlDirect, parsedYear, location),
        getSankrantiCalendar(apiUrlDirect, parsedYear, location),
      ]);

      setCalendar(festivals);
      setChaturmas(chaturmasPeriod);
      setSankranti(
        Object.entries(sankrantiCalendar)
          .map(([rashi, date]) => ({ rashi, date }))
          .sort((a, b) => dateSortKey(a.date) - dateSortKey(b.date))
      );

      const items: VratItem[] = [];
      ekadashis.forEach((o, i) => items.push({ key: `ekadashi-${i}`, label: `${o.name} Ekadashi`, date: o.date }));
      purnima.forEach((d, i) => items.push({ key: `purnima-${i}`, label: 'Purnima Vrat', date: d }));
      amavasya.forEach((d, i) => items.push({ key: `amavasya-${i}`, label: 'Amavasya Vrat', date: d }));
      pradosh.forEach((d, i) => items.push({ key: `pradosh-${i}`, label: 'Pradosh Vrat', date: d }));
      sankashti.forEach((d, i) => items.push({ key: `sankashti-${i}`, label: 'Sankashti Chaturthi Vrat', date: d }));
      items.push({ key: 'pitru-paksha', label: 'Pitru Paksha', date: pitruPaksha.start, endDate: pitruPaksha.end });
      items.push({
        key: 'jyeshtha-gauri',
        label: 'Jyeshtha Gauri',
        date: jyeshthaGauri.avahana,
        endDate: jyeshthaGauri.visarjan,
        subItems: [
          { label: 'Jyeshtha Gauri Avahana', date: jyeshthaGauri.avahana },
          { label: 'Jyeshtha Gauri Poojan', date: jyeshthaGauri.poojan },
          { label: 'Jyeshtha Gauri Visarjan', date: jyeshthaGauri.visarjan },
        ],
      });
      if (hartalikaTeej) items.push({ key: 'hartalika-teej', label: 'Hartalika Teej', date: hartalikaTeej });
      if (ganeshChaturthi && anantChaturdashi) {
        items.push({
          key: 'ganesh-chaturthi',
          label: 'Ganesh Chaturthi – Anant Chaturdashi',
          date: ganeshChaturthi,
          endDate: anantChaturdashi,
          subItems: [
            { label: 'Ganesh Chaturthi', date: ganeshChaturthi },
            { label: 'Anant Chaturdashi', date: anantChaturdashi },
          ],
        });
      }

      items.sort((a, b) => dateSortKey(a.date) - dateSortKey(b.date));
      setVratItems(items.filter((item) => parseStdTime(item.date.StdTime).getTime() >= now - MS_PER_DAY));
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Failed to calculate festival calendar');
    } finally {
      setLoading(false);
    }
  }

  const sortedFestivals = calendar
    ? FESTIVAL_NAMES.filter((name) => calendar[name]).sort((a, b) => dateSortKey(calendar[a]!) - dateSortKey(calendar[b]!))
    : [];

  const chaturmasDaysLeft =
    chaturmas && referenceNow != null ? Math.ceil((parseStdTime(chaturmas.end.StdTime).getTime() - referenceNow) / MS_PER_DAY) : null;
  const showChaturmasBanner = chaturmasDaysLeft != null && chaturmasDaysLeft > 0 && (tab === 'Festivals' || tab === 'Vrat');

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Festival Calendar</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Hindu festival dates are computed fresh each year from tithi (lunar day) and lunar month
          rules - not read from a fixed table - so they fall on a different Gregorian date every
          year. Tap a festival to see its day&apos;s Panchang.
        </ThemedText>

        <ThemedView style={styles.tabRow}>
          {TABS.map((t) => (
            <Pressable
              key={t}
              onPress={() => setTab(t)}
              style={[styles.tabChip, tab === t && styles.tabChipActive, { borderColor: theme.backgroundSelected }]}>
              <ThemedText type="small" themeColor={tab === t ? 'background' : 'text'}>
                {t}
              </ThemedText>
            </Pressable>
          ))}
        </ThemedView>

        {tab !== 'My Tithi' && (
          <>
            <ThemedView style={styles.fieldGroup}>
              <ThemedText style={styles.fieldLabel}>Year</ThemedText>
              <TextInput
                value={year}
                onChangeText={(t) => setYear(t.replace(/[^0-9]/g, '').slice(0, 4))}
                keyboardType="number-pad"
                maxLength={4}
                placeholderTextColor={theme.textSecondary}
                style={[styles.yearInput, { borderColor: theme.backgroundSelected, color: theme.text }]}
              />
            </ThemedView>

            <GeoLocationInput apiUrlDirect={apiUrlDirect} location={location} onChange={setLocation} label="Location" />

            <Pressable onPress={handleCalculate} disabled={loading} style={styles.calculateButton}>
              {loading ? (
                <ActivityIndicator size="small" color="#ffffff" />
              ) : (
                <ThemedText type="smallBold" themeColor="background">
                  Calculate
                </ThemedText>
              )}
            </Pressable>
          </>
        )}

        {showChaturmasBanner && (
          <ThemedView style={[styles.chaturmasBanner, { borderColor: theme.backgroundSelected }]}>
            <Icon name="moon" size={18} color="#F5C518" />
            <ThemedText type="smallBold">Chaturmas · {chaturmasDaysLeft}d left</ThemedText>
          </ThemedView>
        )}

        {tab === 'My Tithi' && <MyTithiTab apiUrlDirect={apiUrlDirect} />}

        {tab === 'Festivals' && sortedFestivals.length > 0 && (
          <ThemedView style={styles.list}>
            {sortedFestivals.map((name) => {
              const date = calendar![name]!;
              const tithi = FESTIVAL_TITHI[name];
              return (
                <Pressable
                  key={name}
                  onPress={() => setSelected({ label: FESTIVAL_LABELS[name], date })}
                  style={[styles.festivalRow, { borderColor: theme.backgroundSelected }]}>
                  {tithi ? <MoonPhaseIcon tithi={tithi} size={28} /> : <Icon name="sparkles" size={22} color={theme.textSecondary} />}
                  <ThemedView style={styles.festivalText}>
                    <ThemedText type="smallBold">{FESTIVAL_LABELS[name]}</ThemedText>
                    <ThemedText type="small" themeColor="textSecondary">
                      {formatDate(date)}
                    </ThemedText>
                  </ThemedView>
                  <Icon name="chevron-right" size={16} color={theme.textSecondary} />
                </Pressable>
              );
            })}
          </ThemedView>
        )}

        {tab === 'Vrat' && vratItems && (
          <ThemedView style={styles.list}>
            {vratItems.length === 0 && <ThemedText type="small" themeColor="textSecondary">Nothing upcoming this year.</ThemedText>}
            {vratItems.map((item) => (
              <Pressable
                key={item.key}
                onPress={() => setSelected({ label: item.label, date: item.date })}
                style={[styles.festivalRow, { borderColor: theme.backgroundSelected }]}>
                <Icon name="moon" size={22} color={theme.textSecondary} />
                <ThemedView style={styles.festivalText}>
                  <ThemedText type="smallBold">{item.label}</ThemedText>
                  <ThemedText type="small" themeColor="textSecondary">
                    {formatDate(item.date)}
                    {item.endDate ? ` – ${formatDate(item.endDate)}` : ''}
                  </ThemedText>
                  {item.subItems?.map((sub) => (
                    <View key={sub.label} style={styles.subItemRow}>
                      <ThemedText type="small" themeColor="textSecondary">
                        {sub.label} · {formatDate(sub.date)}
                      </ThemedText>
                    </View>
                  ))}
                </ThemedView>
                <ThemedText type="small" themeColor="textSecondary">
                  {daysFromNow(item.date, referenceNow ?? 0)}
                </ThemedText>
              </Pressable>
            ))}
          </ThemedView>
        )}

        {tab === 'Sankranti' && sankranti && (
          <ThemedView style={styles.list}>
            {sankranti.map((entry) => (
              <Pressable
                key={entry.rashi}
                onPress={() => setSelected({ label: `${entry.rashi} Sankranti`, date: entry.date })}
                style={[styles.festivalRow, { borderColor: theme.backgroundSelected }]}>
                <Icon name="sun" size={22} color={theme.textSecondary} />
                <ThemedView style={styles.festivalText}>
                  <ThemedText type="smallBold">{entry.rashi} Sankranti</ThemedText>
                  <ThemedText type="small" themeColor="textSecondary">
                    {formatDate(entry.date)}
                  </ThemedText>
                </ThemedView>
                <ThemedText type="small" themeColor="textSecondary">
                  {daysFromNow(entry.date, referenceNow ?? 0)}
                </ThemedText>
              </Pressable>
            ))}
          </ThemedView>
        )}
      </ThemedView>

      <PanchangDetailSheet
        visible={!!selected}
        onClose={() => setSelected(null)}
        apiUrlDirect={apiUrlDirect}
        date={selected?.date ?? null}
        title={selected?.label ?? ''}
      />
    </ScrollView>
  );
}

/** FestivalDate throws for unsupported values server-side; this screen calls it for 3 minor festivals not worth their own typed client function. Returns null on any failure rather than aborting the whole Promise.all. */
async function getFestivalDateSafe(apiUrlDirect: string, festivalName: string, year: number, location: GeoLocation): Promise<BirthTimeJson | null> {
  try {
    const url = `${apiUrlDirect}/Calculate/FestivalDate/FestivalName/${festivalName}/Year/${year}${geoLocationToUrl(location)}`;
    const response = await fetch(url);
    const json = await response.json();
    if (json.Status !== 'Pass') return null;
    return json.Payload as BirthTimeJson;
  } catch {
    return null;
  }
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
  fieldGroup: {
    gap: Spacing.one,
  },
  fieldLabel: {
    fontSize: 13,
    fontWeight: '600',
  },
  yearInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    minWidth: 100,
    alignSelf: 'flex-start',
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
  calculateButton: {
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  chaturmasBanner: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
    borderWidth: 1,
    borderRadius: 999,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    alignSelf: 'flex-start',
  },
  list: {
    gap: Spacing.two,
  },
  festivalRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.three,
    borderWidth: 1,
    borderRadius: 12,
    padding: Spacing.three,
  },
  festivalText: {
    flex: 1,
    gap: 2,
  },
  subItemRow: {
    marginTop: 2,
  },
});
