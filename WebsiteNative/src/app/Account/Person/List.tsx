import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { getPersonList, type Person } from '@/lib/api/person';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Account/Person/List.razor. Tippy'd note-preview tooltips not ported — RN has no hover concept. */
export default function PersonListScreen() {
  const theme = useTheme();
  const router = useRouter();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());
  const visitorId = useAppStore((s) => s.visitorId);

  const [people, setPeople] = useState<Person[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  useEffect(() => {
    setLoading(true);
    getPersonList(apiUrlDirect, effectiveOwnerId, visitorId)
      .then(setPeople)
      .finally(() => setLoading(false));
  }, [apiUrlDirect, effectiveOwnerId, visitorId]);

  const displayed = search ? people.filter((p) => p.name.toLowerCase().includes(search.toLowerCase())) : people;

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Person List</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          All person profiles in your account, you can also add or edit persons.
        </ThemedText>

        <Pressable onPress={() => router.push(`/${PageRoute.AddPerson}` as never)} style={styles.addButton}>
          <ThemedText type="smallBold" themeColor="background">
            Add Person
          </ThemedText>
        </Pressable>

        {loading ? (
          <ActivityIndicator style={styles.loading} />
        ) : people.length === 0 ? (
          <ThemedView style={styles.emptyState}>
            <ThemedText type="smallBold">No person profiles found.</ThemedText>
            <ThemedText themeColor="textSecondary">
              Add a person first, then it will appear here. Make sure you&apos;re signed into the
              correct account.
            </ThemedText>
          </ThemedView>
        ) : (
          <>
            <TextInput
              value={search}
              onChangeText={setSearch}
              placeholder="Search..."
              placeholderTextColor={theme.textSecondary}
              style={[styles.searchInput, { color: theme.text, borderColor: theme.backgroundSelected }]}
            />
            <ThemedView style={styles.list}>
              {displayed.map((person) => (
                <Pressable
                  key={person.id}
                  onPress={() => router.push(`/${PageRoute.PersonEditor}/${person.id}` as never)}
                  style={styles.row}>
                  <ThemedView style={styles.rowContent} type="backgroundElement">
                    <ThemedText type="smallBold">{person.name}</ThemedText>
                    <ThemedText type="small" themeColor="textSecondary">
                      {person.birthTime.StdTime}
                    </ThemedText>
                    <ThemedText type="small" themeColor="textSecondary" numberOfLines={1}>
                      {person.birthTime.Location.Name}
                    </ThemedText>
                    {!!person.notes && (
                      <ThemedText type="small" themeColor="textSecondary" numberOfLines={1}>
                        {person.notes}
                      </ThemedText>
                    )}
                  </ThemedView>
                </Pressable>
              ))}
            </ThemedView>
          </>
        )}
      </ThemedView>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
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
  subtitle: {
    marginBottom: Spacing.one,
  },
  addButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
  loading: {
    marginVertical: Spacing.four,
  },
  emptyState: {
    gap: Spacing.one,
  },
  searchInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
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
});
