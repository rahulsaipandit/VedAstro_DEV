import { LegalPageLayout } from '@/components/LegalPageLayout';

/** Port of Website/Pages/ShippingDelivery.razor. */
export default function ShippingDeliveryScreen() {
  return (
    <LegalPageLayout
      title="Shipping and Delivery Policy"
      sections={[
        {
          heading: 'Shipping Process',
          paragraphs: [
            'Orders are processed within 2-3 business days. Shipping times depend on the selected shipping speed and the distance of the destination.',
          ],
        },
        {
          heading: 'International Shipping',
          paragraphs: [
            'We offer international shipping to select countries. You, the buyer, are responsible for any VAT, tariff, duty, taxes, handling fees, or customs clearance charges required by your country for importing consumer goods.',
          ],
        },
        {
          heading: 'Delivery Time',
          paragraphs: [
            'While we do our best to get your order to you as quickly as possible, delivery times can vary depending on the shipping method selected during checkout.',
          ],
        },
        {
          heading: 'Shipping Rates',
          paragraphs: [
            'Shipping rates are calculated based on the weight of the order and its destination. Check the shipping rate for your order at checkout.',
          ],
        },
        {
          heading: 'Order Tracking',
          paragraphs: [
            'Once your order has shipped, we will send you a shipping confirmation email with a link to track the order on the carrier\'s website.',
          ],
        },
      ]}
    />
  );
}
