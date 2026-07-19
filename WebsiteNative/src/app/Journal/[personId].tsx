import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet } from 'react-native';
import { useLocalSearchParams, useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { useAppStore } from '@/store/useAppStore';
import { getPersonList, getPublicPersonList, type Person } from '@/lib/api/person';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const NATURE_COLOR: Record<string, string> = { Good: '#1a9c4c', Bad: '#d33', Neutral: '#888' };

/** Port of the life-event list half of Journal/Index.razor (ViewComponents/Components/LifeEventViewer.razor). */
export default function JournalListScreen() {
  const router = useRouter();
  const { personId } = useLocalSearchParams<{ personId: string }>();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());
  const visitorId = useAppStore((s) => s.visitorId);

  const [person, setPerson] = useState<Person | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    Promise.all([getPersonList(apiUrlDirect, effectiveOwnerId, visitorId), getPublicPersonList(apiUrlDirect)])
      .then(([own, pub]) => setPerson([...own, ...pub].find((p) => p.id === personId) ?? null))
      .finally(() => setLoading(false));
  }, [apiUrlDirect, effectiveOwnerId, visitorId, personId]);

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
        <ThemedText type="title">Journal | {person.name}</ThemedText>

        <Pressable
          onPress={() => router.push(`/${PageRoute.JournalEditor}/${person.id}/evt${Date.now()}` as never)}
          style={styles.addButton}>
          <ThemedText type="smallBold" themeColor="background">
            Add Event
          </ThemedText>
        </Pressable>

        {person.lifeEventList.length === 0 ? (
          <ThemedText themeColor="textSecondary">No life events recorded yet.</ThemedText>
        ) : (
          <ThemedView style={styles.list}>
            {person.lifeEventList.map((event) => (
              <Pressable
                key={event.id}
                onPress={() => router.push(`/${PageRoute.JournalEditor}/${person.id}/${event.id}` as never)}
                style={styles.row}>
                <ThemedView style={styles.rowContent} type="backgroundElement">
                  <ThemedView style={styles.rowHeader}>
                    <ThemedText type="smallBold">{event.name}</ThemedText>
                    <ThemedText type="smallBold" style={{ color: NATURE_COLOR[event.nature] ?? '#888' }}>
                      {event.nature}
                    </ThemedText>
                  </ThemedView>
                  <ThemedText type="small" themeColor="textSecondary">
                    {event.startTime.StdTime} · {event.weight}
                  </ThemedText>
                  {!!event.description && (
                    <ThemedText type="small" themeColor="textSecondary" numberOfLines={2}>
                      {event.description}
                    </ThemedText>
                  )}
                </ThemedView>
              </Pressable>
            ))}
          </ThemedView>
        )}
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
  addButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
  list: {
    gap: Spacing.two,
  },
  row: {},
  rowContent: {
    borderRadius: 10,
    padding: Spacing.three,
    gap: Spacing.half,
  },
  rowHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
});
