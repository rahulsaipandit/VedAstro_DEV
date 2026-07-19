import { StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { Spacing } from '@/constants/theme';

/**
 * Placeholder for every payment-integration surface (Donate, DonatePayment, Sponsor, Payment).
 * The originals embed live client-side PayPal/Stripe/Ko-fi widgets and real wallet addresses —
 * deliberately not carried over here (per explicit instruction to stub payment integration
 * rather than reimplement it), since picking/wiring a real RN-compatible payment flow (WebView
 * checkout vs. a native SDK vs. deep-linking out to a browser) is a product/business decision,
 * not something to improvise silently.
 */
export function PaymentStubNotice() {
  return (
    <ThemedView style={styles.box} type="backgroundElement">
      <Icon name="heart-plus" size={20} />
      <ThemedView style={styles.textCol}>
        <ThemedText type="smallBold">Payment coming soon</ThemedText>
        <ThemedText type="small" themeColor="textSecondary">
          Online payment isn&apos;t wired up in the app yet — please use the web site for now.
        </ThemedText>
      </ThemedView>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  box: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
    borderRadius: 12,
    padding: Spacing.three,
  },
  textCol: {
    gap: Spacing.half,
    flexShrink: 1,
  },
});
