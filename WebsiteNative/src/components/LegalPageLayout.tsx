import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { MaxContentWidth, Spacing } from '@/constants/theme';

export type LegalSection = { heading?: string; paragraphs: string[] };

/** Shared layout for the static legal/policy pages (PrivacyPolicy, TermsOfService, ShippingDelivery, CancellationRefund, etc.) — all identical title + heading/paragraph structure in the original Blazor pages. */
export function LegalPageLayout({ title, sections }: { title: string; sections: LegalSection[] }) {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">{title}</ThemedText>
        {sections.map((section, index) => (
          <ThemedView key={index} style={styles.section}>
            {section.heading && <ThemedText type="subtitle">{section.heading}</ThemedText>}
            {section.paragraphs.map((paragraph, pIndex) => (
              <ThemedText key={pIndex} style={styles.paragraph}>
                {paragraph}
              </ThemedText>
            ))}
          </ThemedView>
        ))}
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
  section: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
});
