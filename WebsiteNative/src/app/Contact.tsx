import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { InfoBox } from '@/components/InfoBox';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Port of Website/Pages/Contact.razor. The original's message form posted to an XML-only
 * "AddMessageApi" endpoint that was never carried into the JSON-standardized ASP.NET Core API
 * (confirmed by grepping API/FrontDesk for any message/contact endpoint, found none) — same
 * category of gap as other XML-only calls flagged elsewhere in migration.md, so only the direct
 * contact channels (which need no backend) are ported.
 */
export default function ContactScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Contact Us</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Report a bug, suggest a feature or just say Hi.
        </ThemedText>

        <ThemedView style={styles.infoRow}>
          <InfoBox title="Email" description="contact@vedastro.org" icon="user" />
          <InfoBox title="WhatsApp" description="Call or chat with us." icon="search" />
          <InfoBox title="Telegram" description="Call or chat with us." icon="plus" />
        </ThemedView>
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
    gap: Spacing.four,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  infoRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
  },
});
