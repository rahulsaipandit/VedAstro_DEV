import { ScrollView, StyleSheet } from 'react-native';
import { Link } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PaymentStubNotice } from '@/components/PaymentStubNotice';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Donate/DonatePayment.razor. The real PayPal form / crypto wallet cards are stubbed — see PaymentStubNotice. */
export default function DonatePaymentScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Payment Options</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Below are a few ways you can make payment for your donation.
        </ThemedText>

        <PaymentStubNotice />

        <Link href={`/${PageRoute.Contact}` as never}>
          <ThemedText type="linkPrimary">Don&apos;t see your preferred payment method? Contact us</ThemedText>
        </Link>
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
