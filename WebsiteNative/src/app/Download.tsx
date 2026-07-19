import { Pressable, ScrollView, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Download.razor. */
export default function DownloadScreen() {
  const router = useRouter();

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Download</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Run powerful Vedic calculation straight on your desktop. Faster and more powerful than
          the web version.
        </ThemedText>
        <ThemedText type="smallBold">macOS · Windows · Android</ThemedText>
        <Pressable onPress={() => router.push(`/${PageRoute.Donate}` as never)}>
          <ThemedText type="linkPrimary">Fund this feature for faster development.</ThemedText>
        </Pressable>
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
});
