import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { BirthTimeInput, type BirthTimeInputValue } from '@/components/BirthTimeInput';
import { Dropdown } from '@/components/Dropdown';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { deletePerson, getPersonList, getPublicPersonList, updatePerson, type Person } from '@/lib/api/person';
import { buildBirthTimeJsonFromWallClock } from '@/lib/time';
import { getTimezoneOffsetForLocation } from '@/lib/api/geo';
import { showErrorToast, showSuccessToast } from '@/lib/toast';
import { confirm } from '@/lib/confirm';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const GENDER_OPTIONS = [
  { label: 'Male', value: 'Male' },
  { label: 'Female', value: 'Female' },
];

function parseBirthTimeInput(person: Person): BirthTimeInputValue {
  const match = /^(\d{2}):(\d{2}) (\d{2})\/(\d{2})\/(\d{4})/.exec(person.birthTime.StdTime);
  return {
    hh: match?.[1] ?? '00',
    min: match?.[2] ?? '00',
    dd: match?.[3] ?? '01',
    mm: match?.[4] ?? '01',
    yyyy: match?.[5] ?? '2000',
    location: {
      name: person.birthTime.Location.Name,
      longitude: person.birthTime.Location.Longitude,
      latitude: person.birthTime.Location.Latitude,
    },
  };
}

/** Port of Website/Pages/Account/Person/Editor.razor. Notes/Advanced tabs collapsed into one scroll, not a tab bar. */
export default function PersonEditorScreen() {
  const theme = useTheme();
  const router = useRouter();
  const { personId } = useLocalSearchParams<{ personId: string }>();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());
  const visitorId = useAppStore((s) => s.visitorId);

  const [person, setPerson] = useState<Person | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [nameInput, setNameInput] = useState('');
  const [notesInput, setNotesInput] = useState('');
  const [gender, setGender] = useState<'Male' | 'Female'>('Male');
  const [birthTime, setBirthTime] = useState<BirthTimeInputValue | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setLoading(true);
    Promise.all([getPersonList(apiUrlDirect, effectiveOwnerId, visitorId), getPublicPersonList(apiUrlDirect)])
      .then(([own, pub]) => {
        const found = [...own, ...pub].find((p) => p.id === personId);
        if (!found) {
          setError('Person not found.');
          return;
        }
        setPerson(found);
        setNameInput(found.name);
        setNotesInput(found.notes);
        setGender(found.gender);
        setBirthTime(parseBirthTimeInput(found));
      })
      .catch(() => setError('Failed to load person.'))
      .finally(() => setLoading(false));
  }, [apiUrlDirect, effectiveOwnerId, visitorId, personId]);

  async function handleSave() {
    if (!person || !birthTime) return;
    setSaving(true);
    try {
      const offset = await getTimezoneOffsetForLocation(
        apiUrlDirect,
        birthTime.location,
        new Date(Date.UTC(Number(birthTime.yyyy), Number(birthTime.mm) - 1, Number(birthTime.dd)))
      );
      const newBirthTime = buildBirthTimeJsonFromWallClock(
        birthTime.dd,
        birthTime.mm,
        birthTime.yyyy,
        birthTime.hh,
        birthTime.min,
        offset,
        birthTime.location
      );
      await updatePerson(apiUrlDirect, {
        ...person,
        name: nameInput,
        notes: notesInput,
        gender,
        birthTime: newBirthTime,
      });
      showSuccessToast('Person profile updated.');
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Failed to save person');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!person) return;
    const confirmed = await confirm('Delete person?', "This can't be undone.");
    if (!confirmed) return;
    await deletePerson(apiUrlDirect, person.ownerId, person.id);
    showSuccessToast('Person deleted.');
    router.replace(`/${PageRoute.PersonList}` as never);
  }

  if (loading) {
    return (
      <ThemedView style={styles.centered}>
        <ActivityIndicator />
      </ThemedView>
    );
  }

  if (error || !person || !birthTime) {
    return (
      <ThemedView style={styles.centered}>
        <ThemedText themeColor="textSecondary">{error ?? 'Person not found.'}</ThemedText>
      </ThemedView>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Person Profile</ThemedText>

        <ThemedView style={[styles.inputRow, { borderColor: theme.backgroundSelected }]}>
          <TextInput
            value={nameInput}
            onChangeText={setNameInput}
            placeholder="Name"
            placeholderTextColor={theme.textSecondary}
            style={[styles.input, { color: theme.text }]}
          />
        </ThemedView>

        <BirthTimeInput apiUrlDirect={apiUrlDirect} value={birthTime} onChange={setBirthTime} />

        <ThemedView style={styles.section}>
          <ThemedText type="small" themeColor="textSecondary">
            Gender
          </ThemedText>
          <Dropdown
            value={gender}
            options={GENDER_OPTIONS}
            onChange={(v) => setGender(v as 'Male' | 'Female')}
            placeholder="Select gender"
            label="Gender"
          />
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="small" themeColor="textSecondary">
            Notes
          </ThemedText>
          <TextInput
            value={notesInput}
            onChangeText={setNotesInput}
            placeholder="Extra details regarding the person"
            placeholderTextColor={theme.textSecondary}
            multiline
            numberOfLines={6}
            style={[styles.notesInput, { color: theme.text, borderColor: theme.backgroundSelected }]}
          />
        </ThemedView>

        <ThemedView style={styles.section} type="backgroundElement">
          <ThemedText type="smallBold">Advanced</ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            Owner: {person.ownerId}
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.buttonRow}>
          <Pressable onPress={handleDelete} style={styles.deleteButton}>
            <ThemedText type="smallBold" themeColor="background">
              Delete
            </ThemedText>
          </Pressable>
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
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    padding: Spacing.five,
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
  inputRow: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
  },
  input: {
    paddingVertical: Spacing.three,
    fontWeight: '600',
  },
  section: {
    gap: Spacing.two,
    borderRadius: 12,
    padding: Spacing.three,
  },
  notesInput: {
    borderWidth: 1,
    borderRadius: 8,
    padding: Spacing.three,
    minHeight: 120,
    textAlignVertical: 'top',
  },
  buttonRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  deleteButton: {
    backgroundColor: '#d33',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  saveButton: {
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
});
