import { useState } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { PageRoute } from '@/constants/routes';
import { useAppStore } from '@/store/useAppStore';

type NavItem = { label: string; route: string };
type NavGroup = { title: string; items: NavItem[] };

const NAV_GROUPS: NavGroup[] = [
  {
    title: 'Calculators',
    items: [
      { label: 'Match Checker', route: PageRoute.Match },
      { label: 'Match Finder', route: PageRoute.MatchFinder },
      { label: 'Horoscope', route: PageRoute.Horoscope },
      { label: 'Life Predictor', route: PageRoute.LifePredictor },
      { label: 'Good Time Finder', route: PageRoute.GoodTimeFinder },
      { label: 'AI Chat', route: PageRoute.ChatAPI },
      { label: 'Numerology', route: PageRoute.Numerology },
      { label: 'Stars Above Me', route: PageRoute.StarsAboveMe },
      { label: 'Birth Time Finder', route: PageRoute.BirthTimeFinder },
      { label: 'Local Mean Time', route: PageRoute.LocalMeanTime },
      { label: 'Sunrise/Sunset Time', route: PageRoute.SunRiseSetTime },
      { label: 'Open API Builder', route: PageRoute.APIBuilder },
      { label: 'Table Generator', route: PageRoute.TableGenerator },
    ],
  },
  {
    title: 'Account',
    items: [
      { label: 'Login', route: PageRoute.Login },
      { label: 'Person List', route: PageRoute.PersonList },
      { label: 'Add Person', route: PageRoute.AddPerson },
      { label: 'Journal', route: PageRoute.Journal },
    ],
  },
  {
    title: 'More',
    items: [
      { label: 'Quick Guide', route: PageRoute.QuickGuide },
      { label: 'Contact Us', route: PageRoute.Contact },
      { label: 'About', route: PageRoute.About },
      { label: 'Join Us', route: PageRoute.JoinOurFamily },
      { label: 'Train AI', route: PageRoute.TrainAIAstrologer },
      { label: 'Remedy', route: PageRoute.Remedy },
      { label: 'Body Types', route: PageRoute.BodyTypes },
      { label: 'Download', route: PageRoute.Download },
      { label: 'Donate', route: PageRoute.Donate },
    ],
  },
];

/**
 * Persistent top app bar, mounted once at the app root (_layout.tsx) — replaces the old Blazor
 * MainLayout.razor's sidebar (desktop)/offcanvas navbar (mobile), which every single page had.
 * WebsiteNative had no equivalent at all until now: every screen was an isolated island reachable
 * only via in-page buttons or typing a URL directly, with no way to get to Home or browse other
 * calculators from wherever you happened to land. One menu drawer (not a desktop sidebar +
 * separate mobile navbar) since RN doesn't have a natural breakpoint-driven layout swap and this
 * app is mobile-first anyway.
 */
const COLLAPSIBLE_GROUPS = new Set(['Account', 'More']);

export function AppHeader() {
  const theme = useTheme();
  const router = useRouter();
  const currentUser = useAppStore((s) => s.currentUser);
  const [menuOpen, setMenuOpen] = useState(false);
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set());

  function toggleGroup(title: string) {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(title)) {
        next.delete(title);
      } else {
        next.add(title);
      }
      return next;
    });
  }

  function go(route: string) {
    setMenuOpen(false);
    router.push((route.startsWith('/') ? route : `/${route}`) as never);
  }

  return (
    <ThemedView style={[styles.bar, { borderColor: theme.backgroundSelected }]} type="background">
      <Pressable onPress={() => setMenuOpen(true)} style={styles.menuButton} accessibilityLabel="Open menu">
        <Icon name="menu" size={22} />
      </Pressable>

      <Pressable onPress={() => go(PageRoute.Home)} style={styles.brand}>
        <ThemedText type="smallBold" style={styles.brandText}>
          VedAstro
        </ThemedText>
      </Pressable>

      <View style={styles.spacer} />

      {!currentUser.isGuest && (
        <ThemedText type="small" themeColor="textSecondary" style={styles.userLabel} numberOfLines={1}>
          {currentUser.name}
        </ThemedText>
      )}

      <Modal visible={menuOpen} transparent animationType="fade" onRequestClose={() => setMenuOpen(false)}>
        <Pressable style={styles.backdrop} onPress={() => setMenuOpen(false)}>
          <Pressable
            style={[styles.drawer, { backgroundColor: theme.background }]}
            onPress={(e) => e.stopPropagation()}>
            <ScrollView contentContainerStyle={styles.drawerContent}>
              {NAV_GROUPS.map((group) => {
                const collapsible = COLLAPSIBLE_GROUPS.has(group.title);
                const expanded = !collapsible || expandedGroups.has(group.title);
                return (
                  <View key={group.title} style={styles.group}>
                    {collapsible ? (
                      <Pressable
                        onPress={() => toggleGroup(group.title)}
                        style={styles.groupHeader}
                        accessibilityLabel={`${expanded ? 'Collapse' : 'Expand'} ${group.title}`}>
                        <ThemedText type="smallBold" themeColor="textSecondary">
                          {group.title}
                        </ThemedText>
                        <Icon name={expanded ? 'chevron-up' : 'chevron-down'} size={16} />
                      </Pressable>
                    ) : (
                      <ThemedText type="smallBold" themeColor="textSecondary">
                        {group.title}
                      </ThemedText>
                    )}
                    {expanded &&
                      group.items.map((item) => (
                        <Pressable key={item.route} onPress={() => go(item.route)} style={styles.navRow}>
                          <ThemedText type="small">{item.label}</ThemedText>
                        </Pressable>
                      ))}
                  </View>
                );
              })}
            </ScrollView>
          </Pressable>
        </Pressable>
      </Modal>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    height: 52,
    paddingHorizontal: Spacing.three,
    borderBottomWidth: 1,
  },
  brand: {
    paddingVertical: Spacing.one,
    marginLeft: Spacing.two,
  },
  brandText: {
    fontSize: 18,
  },
  spacer: {
    flex: 1,
  },
  userLabel: {
    maxWidth: 120,
    marginRight: Spacing.two,
  },
  menuButton: {
    padding: Spacing.two,
  },
  backdrop: {
    flex: 1,
    flexDirection: 'row',
    justifyContent: 'flex-start',
    backgroundColor: 'rgba(0,0,0,0.35)',
  },
  drawer: {
    width: 280,
    maxWidth: '85%',
    height: '100%',
  },
  drawerContent: {
    paddingTop: 60,
    paddingHorizontal: Spacing.four,
    paddingBottom: Spacing.six,
    gap: Spacing.four,
  },
  group: {
    gap: Spacing.one,
  },
  groupHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  navRow: {
    paddingVertical: Spacing.two,
  },
});
