import { useEffect, useState } from 'react';
import { FlatList, Modal, Pressable, StyleSheet, TextInput } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { useAppStore } from '@/store/useAppStore';
import { getPersonList, getPublicPersonList, type Person } from '@/lib/api/person';
import { PageRoute } from '@/constants/routes';

/**
 * Simplified port of ViewComponents/Components/PersonSelectorBox.razor. Real data (own +
 * public person list, search, selection) is wired up; "Add New Person" now navigates to the
 * real Account/Person/Add screen (see migration.md — this was a "coming soon" stub before
 * Person management was ported).
 */
export function PersonSelector({
  label,
  selectedPerson,
  onSelectPerson,
}: {
  label: string;
  selectedPerson: Person | null;
  onSelectPerson: (person: Person) => void;
}) {
  const theme = useTheme();
  const router = useRouter();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());
  const visitorId = useAppStore((s) => s.visitorId);

  const [modalVisible, setModalVisible] = useState(false);
  const [loading, setLoading] = useState(false);
  const [ownList, setOwnList] = useState<Person[]>([]);
  const [publicList, setPublicList] = useState<Person[]>([]);
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (!modalVisible || loading) return;
    setLoading(true);
    Promise.all([getPersonList(apiUrlDirect, effectiveOwnerId, visitorId), getPublicPersonList(apiUrlDirect)])
      .then(([own, pub]) => {
        setOwnList(own);
        setPublicList(pub);
      })
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [modalVisible]);

  const filter = (list: Person[]) =>
    search ? list.filter((p) => p.name.toLowerCase().includes(search.toLowerCase())) : list;

  function handleSelect(person: Person) {
    onSelectPerson(person);
    setModalVisible(false);
    setSearch('');
  }

  return (
    <ThemedView style={styles.container}>
      <ThemedText type="small" themeColor="textSecondary">
        {label}
      </ThemedText>
      <Pressable
        onPress={() => setModalVisible(true)}
        style={[styles.trigger, { backgroundColor: theme.backgroundElement, borderColor: theme.backgroundSelected }]}>
        <ThemedText>{selectedPerson ? selectedPerson.name : 'Select Person'}</ThemedText>
      </Pressable>

      <Modal visible={modalVisible} animationType="slide" transparent onRequestClose={() => setModalVisible(false)}>
        <Pressable style={styles.backdrop} onPress={() => setModalVisible(false)}>
          <Pressable style={[styles.sheet, { backgroundColor: theme.background }]} onPress={(e) => e.stopPropagation()}>
            <ThemedView style={[styles.searchRow, { borderColor: theme.backgroundSelected }]}>
              <Icon name="search" size={16} color={theme.textSecondary} />
              <TextInput
                value={search}
                onChangeText={setSearch}
                placeholder="Search..."
                placeholderTextColor={theme.textSecondary}
                style={[styles.searchInput, { color: theme.text }]}
              />
            </ThemedView>

            {loading ? (
              <ThemedText style={styles.loadingText}>Loading...</ThemedText>
            ) : (
              <FlatList
                data={[
                  { header: 'Your People' as const },
                  ...filter(ownList).map((p) => ({ person: p })),
                  { header: 'Examples' as const },
                  ...filter(publicList).map((p) => ({ person: p })),
                ]}
                keyExtractor={(item, index) => ('person' in item ? item.person.id : `header-${index}`)}
                renderItem={({ item }) =>
                  'header' in item ? (
                    <ThemedText type="smallBold" themeColor="textSecondary" style={styles.listHeader}>
                      {item.header}
                    </ThemedText>
                  ) : (
                    <Pressable onPress={() => handleSelect(item.person)} style={styles.row}>
                      <ThemedText>{item.person.name}</ThemedText>
                    </Pressable>
                  )
                }
              />
            )}

            <Pressable
              onPress={() => {
                setModalVisible(false);
                router.push(`/${PageRoute.AddPerson}` as never);
              }}
              style={[styles.addButton, { backgroundColor: theme.backgroundSelected }]}>
              <Icon name="plus" size={16} />
              <ThemedText type="smallBold">Add New Person</ThemedText>
            </Pressable>
          </Pressable>
        </Pressable>
      </Modal>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    gap: Spacing.one,
  },
  trigger: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.three,
    minWidth: 220,
  },
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.4)',
    justifyContent: 'flex-end',
  },
  sheet: {
    maxHeight: '70%',
    borderTopLeftRadius: 16,
    borderTopRightRadius: 16,
    padding: Spacing.four,
    gap: Spacing.three,
  },
  searchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
  },
  searchInput: {
    flex: 1,
    paddingVertical: Spacing.two,
  },
  loadingText: {
    textAlign: 'center',
    paddingVertical: Spacing.four,
  },
  listHeader: {
    paddingVertical: Spacing.two,
  },
  row: {
    paddingVertical: Spacing.two,
  },
  addButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: Spacing.one,
    borderRadius: 8,
    paddingVertical: Spacing.three,
  },
});
