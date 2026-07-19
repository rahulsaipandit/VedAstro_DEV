import { LegalPageLayout } from '@/components/LegalPageLayout';

/** Port of Website/Pages/CancellationRefund.razor. */
export default function CancellationRefundScreen() {
  return (
    <LegalPageLayout
      title="Cancellation and Refund Policy"
      sections={[
        {
          paragraphs: ["Thank you for shopping at VedAstro. If you are not entirely satisfied with your purchase, we're here to help."],
        },
        {
          heading: 'Cancellations',
          paragraphs: [
            'If you wish to cancel your order, please contact us within 24 hours of placing the order. Cancellation requests received after this period will not be accepted.',
          ],
        },
        {
          heading: 'Returns',
          paragraphs: [
            'You have 30 calendar days to return an item from the date you received it. To be eligible for a return, your item must be unused and in the same condition you received it.',
          ],
        },
        {
          heading: 'Refunds',
          paragraphs: [
            'Once we receive your item, we will inspect it and notify you. If your return is approved, we will initiate a refund to your original method of payment.',
          ],
        },
        {
          heading: 'Contact Us',
          paragraphs: ['If you have any questions about our Cancellation and Refund Policy, please contact us.'],
        },
      ]}
    />
  );
}
