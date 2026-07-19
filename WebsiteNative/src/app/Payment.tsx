import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PaymentStubNotice } from '@/components/PaymentStubNotice';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Payment.razor — the original immediately redirects to the Ko-fi membership page; stubbed here rather than auto-redirecting to an external payment site. */
export default function PaymentScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Payment</ThemedText>
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
