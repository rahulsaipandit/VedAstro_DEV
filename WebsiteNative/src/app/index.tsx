import { useMemo } from 'react';
import {
  Image,
  ImageBackground,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  useWindowDimensions,
} from 'react-native';
import { Link, useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { useTheme } from '@/hooks/use-theme';
import { PageRoute } from '@/constants/routes';
import { GitHubRepo } from '@/constants/urls';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Ported from Website/Pages/Index.razor. The Bootstrap card grid/hero banner
 * becomes plain RN Flexbox + StyleSheet (no CSS available) — see migration.md's
 * Phase 3 "Why React Native" note.
 */
type QuickLink = {
  route: string;
  image: ReturnType<typeof require>;
  title: string;
  description: string;
};

const QUICK_LINKS: QuickLink[] = [
  {
    route: PageRoute.ChatAPI,
    image: require('@/assets/images/quicklinks/ai-chat-card.jpg'),
    title: 'AI Astrologer',
    description: 'Talk & learn with AI powered astrologer for any queries',
  },
  {
    route: PageRoute.TableGenerator,
    image: require('@/assets/images/quicklinks/table-generator-card.jpg'),
    title: 'ML Data Generator',
    description: 'Easily create large ML & AI data tables',
  },
  {
    route: PageRoute.MatchFinder,
    image: require('@/assets/images/quicklinks/match-finder-card.jpg'),
    title: 'Find Soulmate',
    description: 'Free search astrological database for perfect match',
  },
  {
    route: PageRoute.LifePredictor,
    image: require('@/assets/images/quicklinks/dasa-card.jpg'),
    title: 'Life Predictor',
    description: 'Know good and bad periods of your life years ahead',
  },
  {
    route: PageRoute.Match,
    image: require('@/assets/images/quicklinks/match-card.jpg'),
    title: 'Match',
    description: 'Check astro chemistry for romance & relationship',
  },
  {
    route: PageRoute.Horoscope,
    image: require('@/assets/images/quicklinks/horoscope-card.jpg'),
    title: 'Horoscope',
    description: "Predict a person's character, speech, body & general life",
  },
  {
    route: PageRoute.GoodTimeFinder,
    image: require('@/assets/images/quicklinks/muhurtha-card.jpg'),
    title: 'Muhurtha',
    description: 'Find a good time for buying car, travel, studies, building..',
  },
  {
    route: PageRoute.Journal,
    image: require('@/assets/images/quicklinks/add-life-event-card.jpg'),
    title: 'Journal',
    description: 'Astrological journal to help understand your life events',
  },
  {
    route: PageRoute.APIBuilder,
    image: require('@/assets/images/quicklinks/open-api-card.jpg'),
    title: 'Open API',
    description: 'Access powerful astrological tools via a HTTP request',
  },
  {
    route: PageRoute.BirthTimeFinder,
    image: require('@/assets/images/quicklinks/birth-time-finder-card.jpg'),
    title: 'Birth Time Finder',
    description: 'Find forgotten or lost birth time using astrological',
  },
  {
    route: PageRoute.SunRiseSetTime,
    image: require('@/assets/images/quicklinks/sunrise-card.jpg'),
    title: 'Sunrise Time',
    description: "Time when Sun's disc center meets the horizon",
  },
];

// Old Razor page reshuffled the card order with Random() "for newness effect"
// on every load; kept as a one-time shuffle per mount here.
function shuffle<T>(list: T[]): T[] {
  return [...list].sort(() => Math.random() - 0.5);
}

export default function HomeScreen() {
  const theme = useTheme();
  const router = useRouter();
  const { width } = useWindowDimensions();
  const quickLinks = useMemo(() => shuffle(QUICK_LINKS), []);

  const columns = width >= 900 ? 4 : width >= 650 ? 3 : 2;
  const cardWidthPercent = 100 / columns;

  return (
    <ScrollView style={{ backgroundColor: theme.background }} contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ImageBackground
          source={require('@/assets/images/zodiac-wheel-faded-2.png')}
          style={[styles.hero, { backgroundColor: theme.backgroundElement }]}
          imageStyle={styles.heroImage}>
          <ThemedText type="title" style={styles.heroTitle}>
            VedAstro
          </ThemedText>
          <ThemedText style={styles.heroLead}>
            A non-profit, open source project to make Vedic Astrology easily available to all.
          </ThemedText>
          <ThemedView style={styles.heroButtonRow}>
            <HeroButton label="Quick Guide" onPress={() => router.push(`/${PageRoute.QuickGuide}` as never)} />
            <HeroButton label="Source Code" onPress={() => openExternal(GitHubRepo)} />
            <HeroButton label="Donate" onPress={() => router.push(`/${PageRoute.Donate}` as never)} />
          </ThemedView>
        </ImageBackground>

        <ThemedView style={styles.quickLinksHeader}>
          <ThemedText type="subtitle">Quick Links</ThemedText>
          <Link href={`/${PageRoute.CalculatorList}` as never} asChild>
            <Pressable>
              <ThemedText type="linkPrimary">View All</ThemedText>
            </Pressable>
          </Link>
        </ThemedView>

        <ThemedView style={styles.grid}>
          {quickLinks.map((link) => (
            <Pressable
              key={link.title}
              onPress={() => router.push(`/${link.route}` as never)}
              style={[styles.cardWrapper, { width: `${cardWidthPercent}%` }]}>
              <ThemedView style={[styles.card, { borderColor: theme.backgroundSelected }]}>
                <Image source={link.image} style={styles.cardImage} resizeMode="cover" />
                <ThemedView style={styles.cardBody}>
                  <ThemedText type="smallBold">{link.title}</ThemedText>
                  <ThemedText type="small" themeColor="textSecondary">
                    {link.description}
                  </ThemedText>
                </ThemedView>
              </ThemedView>
            </Pressable>
          ))}
        </ThemedView>
      </ThemedView>
    </ScrollView>
  );
}

function HeroButton({ label, onPress }: { label: string; onPress: () => void }) {
  const theme = useTheme();
  return (
    <Pressable onPress={onPress} style={[styles.heroButton, { backgroundColor: theme.backgroundSelected }]}>
      <ThemedText type="smallBold">{label}</ThemedText>
    </Pressable>
  );
}

function openExternal(url: string) {
  if (Platform.OS === 'web') {
    window.open(url, '_blank');
  } else {
    import('expo-web-browser').then(({ openBrowserAsync }) => openBrowserAsync(url));
  }
}

const styles = StyleSheet.create({
  scrollContent: {
    alignItems: 'center',
  },
  page: {
    width: '100%',
    maxWidth: MaxContentWidth,
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.four,
    paddingBottom: Spacing.six,
  },
  hero: {
    borderRadius: 16,
    padding: Spacing.four,
    marginBottom: Spacing.five,
    overflow: 'hidden',
  },
  heroImage: {
    opacity: 0.15,
    resizeMode: 'contain',
  },
  heroTitle: {
    marginBottom: Spacing.two,
  },
  heroLead: {
    marginBottom: Spacing.four,
  },
  heroButtonRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.two,
  },
  heroButton: {
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
  quickLinksHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: Spacing.three,
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
  },
  cardWrapper: {
    padding: Spacing.two,
  },
  card: {
    borderWidth: 1,
    borderRadius: 12,
    overflow: 'hidden',
  },
  cardImage: {
    width: '100%',
    aspectRatio: 16 / 9,
  },
  cardBody: {
    padding: Spacing.two,
    gap: Spacing.half,
  },
});
