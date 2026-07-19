import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PaymentStubNotice } from '@/components/PaymentStubNotice';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Sponsor.razor — the original is a raw PayPal subscription-button JS SDK embed, stubbed here. */
export default function SponsorScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Sponsor</ThemedText>
        <PaymentStubNotice />
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
});
