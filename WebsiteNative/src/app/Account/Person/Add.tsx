import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Switch, TextInput } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { InfoBox } from '@/components/InfoBox';
import { Icon } from '@/components/Icon';
import { BirthTimeInput, type BirthTimeInputValue } from '@/components/BirthTimeInput';
import { Dropdown } from '@/components/Dropdown';
import { AdvancedOptionsSheet } from '@/components/AdvancedOptionsSheet';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { addPerson, getPersonList, getPublicPersonList, type Person } from '@/lib/api/person';
import { buildBirthTimeJsonFromWallClock } from '@/lib/time';
import { getTimezoneOffsetForLocation } from '@/lib/api/geo';
import { showErrorToast, showSuccessToast } from '@/lib/toast';
import { DEFAULT_GEO_LOCATION } from '@/components/GeoLocationInput';
import { loadCalculationPreferences, saveCalculationPreferences, type CalculationPreferences } from '@/lib/preferences';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const GENDER_OPTIONS = [
  { label: 'Male', value: 'Male' },
  { label: 'Female', value: 'Female' },
];

const AccentColor = '#2F6FED';

function blankBirthTime(): BirthTimeInputValue {
  return { dd: '', mm: '', yyyy: '', hh: '', min: '', location: DEFAULT_GEO_LOCATION };
}

/**
 * Port of Website/Pages/Account/Person/Add.razor — the first real implementation of
 * PersonSelector's "Add New Person" flow (previously a "coming soon" stub, see migration.md's
 * "Dead/unreachable/deferred code" section). The original's string of "are you sure?" sanity
 * checks (short name, digits in name, time looks like midnight, future birth date) are UX
 * flourishes on top of the real save, not the save itself — not ported, so this keeps only the
 * validation that actually blocks bad data (name required). Layout follows the updated mock:
 * a topbar back arrow, a "Use saved profiles" quick-fill toggle, and a gear-triggered Advanced
 * Options sheet for chart-computation preferences (Ayanamsa/House system/Node type — these are
 * *not* part of the saved person, see src/lib/preferences.ts).
 */
export default function AddPersonScreen() {
  const theme = useTheme();
  const router = useRouter();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());
  const visitorId = useAppStore((s) => s.visitorId);

  const [nameInput, setNameInput] = useState('');
  const [gender, setGender] = useState<'Male' | 'Female' | ''>('');
  const [birthTime, setBirthTime] = useState<BirthTimeInputValue>(blankBirthTime());
  const [saving, setSaving] = useState(false);

  const [useSavedProfile, setUseSavedProfile] = useState(false);
  const [savedProfiles, setSavedProfiles] = useState<Person[]>([]);
  const [savedProfilesLoading, setSavedProfilesLoading] = useState(false);
  const [selectedProfileId, setSelectedProfileId] = useState('');

  const [advancedOpen, setAdvancedOpen] = useState(false);
  const [calcPrefs, setCalcPrefs] = useState<CalculationPreferences | null>(null);

  useEffect(() => {
    loadCalculationPreferences().then(setCalcPrefs);
  }, []);

  useEffect(() => {
    if (!useSavedProfile || savedProfiles.length > 0) return;
    setSavedProfilesLoading(true);
    Promise.all([getPersonList(apiUrlDirect, effectiveOwnerId, visitorId), getPublicPersonList(apiUrlDirect)])
      .then(([own, pub]) => setSavedProfiles([...own, ...pub]))
      .catch(() => showErrorToast('Failed to load saved profiles'))
      .finally(() => setSavedProfilesLoading(false));
  }, [useSavedProfile, savedProfiles.length, apiUrlDirect, effectiveOwnerId, visitorId]);

  function handleChangeCalcPrefs(next: CalculationPreferences) {
    setCalcPrefs(next);
    saveCalculationPreferences(next);
  }

  function handleSelectSavedProfile(personId: string) {
    setSelectedProfileId(personId);
    const found = savedProfiles.find((p) => p.id === personId);
    if (!found) return;
    setGender(found.gender);
    const match = /^(\d{2}):(\d{2}) (\d{2})\/(\d{2})\/(\d{4})/.exec(found.birthTime.StdTime);
    setBirthTime({
      hh: match?.[1] ?? '',
      min: match?.[2] ?? '',
      dd: match?.[3] ?? '',
      mm: match?.[4] ?? '',
      yyyy: match?.[5] ?? '',
      location: {
        name: found.birthTime.Location.Name,
        longitude: found.birthTime.Location.Longitude,
        latitude: found.birthTime.Location.Latitude,
      },
    });
  }

  async function handleSave() {
    if (!nameInput.trim()) {
      showErrorToast('Please enter a name');
      return;
    }
    if (!birthTime.dd || !birthTime.mm || !birthTime.yyyy || !birthTime.hh || !birthTime.min) {
      showErrorToast('Please fill in the full birth time');
      return;
    }
    if (!gender) {
      showErrorToast('Please select a gender');
      return;
    }
    setSaving(true);
    try {
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
      await addPerson(apiUrlDirect, effectiveOwnerId, time, nameInput.trim(), gender as 'Male' | 'Female');
      showSuccessToast(`${nameInput.trim()} added!`);
      // router.back() silently no-ops when this screen was reached by typing the URL directly
      // (no navigation history to go back to) - go somewhere real either way.
      if (router.canGoBack()) router.back();
      else router.replace(`/${PageRoute.PersonList}` as never);
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Failed to save person');
    } finally {
      setSaving(false);
    }
  }

  function handleBack() {
    if (router.canGoBack()) router.back();
    else router.replace(`/${PageRoute.PersonList}` as never);
  }

  return (
    <ThemedView style={styles.screen}>
      <ScrollView contentContainerStyle={styles.scrollContent}>
        <ThemedView style={styles.page}>
        <ThemedView style={styles.topbar}>
          <Pressable onPress={handleBack} hitSlop={8} style={[styles.backIconButton, { backgroundColor: theme.backgroundElement }]}>
            <Icon name="arrow-left" size={18} />
          </Pressable>
          <ThemedText style={styles.topbarTitle}>Add new person</ThemedText>
        </ThemedView>
        <ThemedText type="small" themeColor="textSecondary">
          Used for numerology and astrology predictions.
        </ThemedText>

        <ThemedView style={styles.fieldGroup}>
          <ThemedText style={styles.fieldLabel}>
            Name<ThemedText style={styles.requiredAsterisk}> *</ThemedText>
          </ThemedText>
          <ThemedView style={[styles.inputRow, { borderColor: theme.backgroundSelected }]}>
            <TextInput
              value={nameInput}
              onChangeText={setNameInput}
              placeholder="Enter full name"
              placeholderTextColor={theme.textSecondary}
              style={[styles.input, { color: theme.text }]}
            />
          </ThemedView>
          <ThemedText type="small" themeColor="textSecondary" style={styles.hintText}>
            Use the real name for an accurate prediction.
          </ThemedText>
        </ThemedView>

        <BirthTimeInput apiUrlDirect={apiUrlDirect} value={birthTime} onChange={setBirthTime} />

        <ThemedView style={styles.fieldGroup}>
          <ThemedText style={styles.fieldLabel}>
            Gender<ThemedText style={styles.requiredAsterisk}> *</ThemedText>
          </ThemedText>
          <Dropdown
            value={gender}
            options={GENDER_OPTIONS}
            onChange={(v) => setGender(v as 'Male' | 'Female')}
            placeholder="Select gender"
            label="Gender"
          />
        </ThemedView>

        <ThemedView style={[styles.divider, { backgroundColor: theme.backgroundSelected }]} />

        <ThemedView style={styles.savedRow}>
          <ThemedView style={styles.savedRowLeft}>
            <Pressable
              onPress={() => setAdvancedOpen(true)}
              style={[styles.gearButton, { borderColor: theme.backgroundSelected }]}>
              <Icon name="settings" size={18} color={theme.textSecondary} />
            </Pressable>
            <ThemedView style={styles.savedRowText}>
              <ThemedText type="smallBold">Use saved profiles</ThemedText>
              <ThemedText type="small" themeColor="textSecondary">
                Quickly fill details from an existing profile
              </ThemedText>
            </ThemedView>
          </ThemedView>
          <Switch
            value={useSavedProfile}
            onValueChange={setUseSavedProfile}
            trackColor={{ true: AccentColor, false: theme.backgroundSelected }}
            thumbColor="#ffffff"
          />
        </ThemedView>

        {useSavedProfile && (
          <ThemedView style={styles.fieldGroup}>
            {savedProfilesLoading ? (
              <ActivityIndicator size="small" />
            ) : (
              <Dropdown
                value={selectedProfileId}
                options={savedProfiles.map((p) => ({ label: p.name, value: p.id }))}
                onChange={handleSelectSavedProfile}
                placeholder="Choose a saved profile"
                label="Saved profiles"
              />
            )}
          </ThemedView>
        )}

        <ThemedView style={styles.infoRow}>
          <InfoBox title="Private" description="This person's data stays private and isn't visible to others." icon="user" />
          <InfoBox
            title="Forgotten birth time?"
            description="Use advanced computation to estimate a lost birth time."
            icon="search"
          />
        </ThemedView>
        </ThemedView>
      </ScrollView>

      <ThemedView style={[styles.footerOuter, { backgroundColor: theme.background, borderColor: theme.backgroundSelected }]}>
        <ThemedView style={styles.footer}>
          <Pressable onPress={handleSave} disabled={saving} style={styles.saveButton}>
            {saving ? (
              <ActivityIndicator size="small" color="#ffffff" />
            ) : (
              <ThemedText type="smallBold" themeColor="background">
                Save
              </ThemedText>
            )}
          </Pressable>
        </ThemedView>
      </ThemedView>

      {calcPrefs && (
        <AdvancedOptionsSheet
          visible={advancedOpen}
          onClose={() => setAdvancedOpen(false)}
          prefs={calcPrefs}
          onChange={handleChangeCalcPrefs}
        />
      )}
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  screen: {
    flex: 1,
  },
  scrollContent: {
    alignItems: 'center',
  },
  page: {
    width: '100%',
    maxWidth: MaxContentWidth,
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    paddingBottom: Spacing.six,
    gap: Spacing.four,
  },
  topbar: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.three,
  },
  backIconButton: {
    width: 36,
    height: 36,
    borderRadius: 10,
    alignItems: 'center',
    justifyContent: 'center',
  },
  topbarTitle: {
    fontSize: 22,
    fontWeight: '700',
    lineHeight: 26,
  },
  inputRow: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
  },
  input: {
    paddingVertical: Spacing.three,
    fontWeight: '600',
  },
  fieldGroup: {
    gap: Spacing.one,
  },
  fieldLabel: {
    fontSize: 13,
    fontWeight: '600',
  },
  requiredAsterisk: {
    color: '#D64545',
  },
  hintText: {
    fontSize: 11,
    lineHeight: 14,
  },
  divider: {
    height: 1,
  },
  savedRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: Spacing.three,
  },
  savedRowLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.three,
    flex: 1,
  },
  gearButton: {
    width: 40,
    height: 40,
    borderRadius: 11,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  savedRowText: {
    flex: 1,
    gap: 2,
  },
  infoRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
  },
  footerOuter: {
    alignItems: 'center',
    borderTopWidth: 1,
  },
  footer: {
    width: '100%',
    maxWidth: MaxContentWidth,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.three,
  },
  saveButton: {
    backgroundColor: '#1F9D55',
    paddingVertical: Spacing.three,
    borderRadius: 12,
    alignItems: 'center',
  },
});
