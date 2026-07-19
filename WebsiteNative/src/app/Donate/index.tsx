import { ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { InfoBox } from '@/components/InfoBox';
import { PaymentStubNotice } from '@/components/PaymentStubNotice';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/** Port of Website/Pages/Donate/Donate.razor. Ko-fi iframe / Stripe buy-buttons stubbed — see PaymentStubNotice. */
export default function DonateScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Crowdfunding</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Help to improve & speed up development of the project. Contribute to improve the future
          of Vedic astrology.
        </ThemedText>

        <PaymentStubNotice />

        <ThemedView style={styles.infoRow}>
          <InfoBox title="Crypto" description="Zero fees, instant and anonymous donation" icon="user" />
          <InfoBox title="Patreon" description="Monthly contribution via Patreon" icon="heart-plus" />
          <InfoBox title="Gifts" description="Send gifts from our wish list" icon="cards-heart" />
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Why Donate?</ThemedText>
          <ThemedText style={styles.paragraph}>
            Donations help keep this website&apos;s heart beating 24/7 and continually improve the
            project with new features and bug fixes. First priority goes to hosting costs — with
            support from users we hope to diversify the hosting platform for a more stable &
            reliable service.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Transparent Donations</ThemedText>
          <ThemedText style={styles.paragraph}>
            Know exactly where your money is going — we try to keep the fund flow as transparent
            as possible. You can choose to sponsor a specific thing like domain fees or hosting
            costs; leave your choice in the message box.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Programmer Costs</ThemedText>
          <ThemedText style={styles.paragraph}>
            As of December 2022, more than 88,000 lines of code were written for this project, not
            including documentation. The more code there is, the more testing & debugging that
            needs to be done — every dollar donated goes a long way to help pay for this.
          </ThemedText>
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
  section: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
});
