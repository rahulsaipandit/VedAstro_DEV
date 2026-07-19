import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { showErrorToast, showSuccessToast } from '@/lib/toast';
import { confirm } from '@/lib/confirm';
import { getPersonList, getPublicPersonList, updatePerson, type Person } from '@/lib/api/person';
import type { LifeEvent } from '@/lib/api/lifeEvent';
import { buildBirthTimeJsonFromWallClock } from '@/lib/time';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const NATURE_OPTIONS = ['Good', 'Neutral', 'Bad'] as const;
const WEIGHT_OPTIONS = ['Major', 'Normal', 'Minor'] as const;

/** Port of Website/Pages/Journal/Editor.razor. Reuses the person's own birth location for the event's location (the original also defaults GeoLocationInput to it, just editable there — kept fixed here for simplicity). */
export default function JournalEditorScreen() {
  const theme = useTheme();
  const router = useRouter();
  const { personId, eventId } = useLocalSearchParams<{ personId: string; eventId: string }>();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());
  const visitorId = useAppStore((s) => s.visitorId);

  const [person, setPerson] = useState<Person | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [nature, setNature] = useState<LifeEvent['nature'] | ''>('');
  const [weight, setWeight] = useState<LifeEvent['weight']>('Normal');
  const [dd, setDd] = useState('01');
  const [mm, setMm] = useState('01');
  const [yyyy, setYyyy] = useState(String(new Date().getFullYear()));
  const [hh, setHh] = useState('00');
  const [min, setMin] = useState('00');

  useEffect(() => {
    setLoading(true);
    Promise.all([getPersonList(apiUrlDirect, effectiveOwnerId, visitorId), getPublicPersonList(apiUrlDirect)])
      .then(([own, pub]) => {
        const found = [...own, ...pub].find((p) => p.id === personId) ?? null;
        setPerson(found);
        const existing = found?.lifeEventList.find((e) => e.id === eventId);
        if (existing) {
          setName(existing.name);
          setDescription(existing.description);
          setNature(existing.nature);
          setWeight(existing.weight);
          const match = /^(\d{2}):(\d{2}) (\d{2})\/(\d{2})\/(\d{4})/.exec(existing.startTime.StdTime);
          if (match) {
            setHh(match[1]);
            setMin(match[2]);
            setDd(match[3]);
            setMm(match[4]);
            setYyyy(match[5]);
          }
        }
      })
      .finally(() => setLoading(false));
  }, [apiUrlDirect, effectiveOwnerId, visitorId, personId, eventId]);

  async function handleSave() {
    if (!person) return;
    if (!name.trim()) {
      showErrorToast('Please enter a name');
      return;
    }
    if (!nature) {
      showErrorToast('Was the event Good or Bad? Please pick a Nature.');
      return;
    }
    setSaving(true);
    try {
      const offsetMatch = /( [+-]\d{2}:\d{2})$/.exec(person.birthTime.StdTime);
      const offset = offsetMatch ? offsetMatch[1].trim() : '+00:00';
      const startTime = buildBirthTimeJsonFromWallClock(dd, mm, yyyy, hh, min, offset, {
        name: person.birthTime.Location.Name,
        longitude: person.birthTime.Location.Longitude,
        latitude: person.birthTime.Location.Latitude,
      });
      const updatedEvent: LifeEvent = {
        id: eventId,
        personId: person.id,
        name: name.trim(),
        startTime,
        description,
        nature,
        weight,
      };
      const newList = [...person.lifeEventList.filter((e) => e.id !== eventId), updatedEvent];
      await updatePerson(apiUrlDirect, { ...person, lifeEventList: newList });
      showSuccessToast('Event saved!');
      if (router.canGoBack()) router.back();
      else router.replace(`/Journal/${person.id}` as never);
    } catch (e) {
      showErrorToast(e instanceof Error ? e.message : 'Failed to save event');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!person) return;
    const confirmed = await confirm('Delete this event?', 'This can\'t be undone.');
    if (!confirmed) return;
    const newList = person.lifeEventList.filter((e) => e.id !== eventId);
    await updatePerson(apiUrlDirect, { ...person, lifeEventList: newList });
    showSuccessToast('Event deleted.');
    if (router.canGoBack()) router.back();
    else router.replace(`/Journal/${person.id}` as never);
  }

  if (loading) {
    return (
      <ThemedView style={styles.centered}>
        <ActivityIndicator />
      </ThemedView>
    );
  }

  if (!person) {
    return (
      <ThemedView style={styles.centered}>
        <ThemedText themeColor="textSecondary">Person not found.</ThemedText>
      </ThemedView>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Life Event | {person.name}</ThemedText>

        <ThemedView style={styles.row}>
          {[
            { value: hh, set: setHh, placeholder: 'HH', maxLength: 2, width: 48 },
            { value: min, set: setMin, placeholder: 'MM', maxLength: 2, width: 48 },
            { value: dd, set: setDd, placeholder: 'DD', maxLength: 2, width: 48 },
            { value: mm, set: setMm, placeholder: 'MM', maxLength: 2, width: 48 },
            { value: yyyy, set: setYyyy, placeholder: 'YYYY', maxLength: 4, width: 64 },
          ].map((field, index) => (
            <TextInput
              key={index}
              value={field.value}
              onChangeText={field.set}
              placeholder={field.placeholder}
              keyboardType="numeric"
              maxLength={field.maxLength}
              placeholderTextColor={theme.textSecondary}
              style={[
                styles.smallInput,
                { width: field.width, color: theme.text, borderColor: theme.backgroundSelected },
              ]}
            />
          ))}
        </ThemedView>

        <TextInput
          value={name}
          onChangeText={setName}
          placeholder="Marriage"
          placeholderTextColor={theme.textSecondary}
          style={[styles.input, { color: theme.text, borderColor: theme.backgroundSelected }]}
        />

        <TextInput
          value={description}
          onChangeText={setDescription}
          placeholder="All went well"
          placeholderTextColor={theme.textSecondary}
          multiline
          numberOfLines={5}
          style={[styles.textArea, { color: theme.text, borderColor: theme.backgroundSelected }]}
        />

        <ThemedView style={styles.pickerRow}>
          <ThemedText type="small" themeColor="textSecondary">
            Weight
          </ThemedText>
          <ThemedView style={styles.chipRow}>
            {WEIGHT_OPTIONS.map((option) => (
              <Pressable
                key={option}
                onPress={() => setWeight(option)}
                style={[styles.chip, weight === option && styles.chipActive]}>
                <ThemedText type="small" themeColor={weight === option ? 'background' : 'text'}>
                  {option}
                </ThemedText>
              </Pressable>
            ))}
          </ThemedView>
        </ThemedView>

        <ThemedView style={styles.pickerRow}>
          <ThemedText type="small" themeColor="textSecondary">
            Nature
          </ThemedText>
          <ThemedView style={styles.chipRow}>
            {NATURE_OPTIONS.map((option) => (
              <Pressable
                key={option}
                onPress={() => setNature(option)}
                style={[styles.chip, nature === option && styles.chipActive]}>
                <ThemedText type="small" themeColor={nature === option ? 'background' : 'text'}>
                  {option}
                </ThemedText>
              </Pressable>
            ))}
          </ThemedView>
        </ThemedView>

        <ThemedView style={styles.buttonRow}>
          <Pressable onPress={() => router.back()} style={styles.backButton}>
            <ThemedText type="smallBold">Back</ThemedText>
          </Pressable>
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
    gap: Spacing.three,
  },
  row: {
    flexDirection: 'row',
    gap: Spacing.one,
  },
  smallInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingVertical: Spacing.two,
    textAlign: 'center',
  },
  input: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.three,
    fontWeight: '600',
  },
  textArea: {
    borderWidth: 1,
    borderRadius: 8,
    padding: Spacing.three,
    minHeight: 120,
    textAlignVertical: 'top',
  },
  pickerRow: {
    gap: Spacing.one,
  },
  chipRow: {
    flexDirection: 'row',
    gap: Spacing.one,
  },
  chip: {
    borderRadius: 999,
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    backgroundColor: '#00000010',
  },
  chipActive: {
    backgroundColor: '#1a9c4c',
  },
  buttonRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: Spacing.two,
  },
  backButton: {
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.three,
    borderRadius: 8,
    backgroundColor: '#00000010',
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
