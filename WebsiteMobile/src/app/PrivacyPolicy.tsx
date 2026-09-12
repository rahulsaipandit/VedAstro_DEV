import { LegalPageLayout } from '@/components/LegalPageLayout';

/** Port of Website/Pages/PrivacyPolicy.razor. */
export default function PrivacyPolicyScreen() {
  return (
    <LegalPageLayout
      title="Privacy Policy"
      sections={[
        {
          paragraphs: [
            'This Privacy Policy describes VedAstro\'s policies and procedures on the collection, use and disclosure of your information when you respond to our advertisements and tells you about your privacy rights and how the law protects you. We use your Personal Data to contact and support you, as well as to provide and improve the Service. By using the Service, you agree to the collection and use of information in accordance with this Privacy Policy.',
          ],
        },
        {
          heading: 'Interpretation and Definitions',
          paragraphs: [
            'The words of which the initial letter is capitalized have meanings defined under the following conditions. The following definitions shall have the same meaning regardless of whether they appear in singular or in plural.',
            'Company (referred to as "the Company", "We", "Us" or "Our") refers to VedAstro. Device means any device that can access the Service. Personal Data is any information that relates to an identified or identifiable individual. Service refers to the website or application. Service Provider means any natural or legal person who processes data on behalf of the Company. Usage Data refers to data collected automatically. You means the individual accessing or using the Service.',
          ],
        },
        {
          heading: 'Google OAuth',
          paragraphs: [
            'VedAstro uses Google OAuth for website authentication. When you authenticate via Google OAuth, we access your basic profile information from Google — your name and email address.',
            'We use this data solely for the purpose of authentication and do not share it with any third parties, nor use it for any other purpose.',
          ],
        },
        {
          heading: 'Security of Your Personal Data',
          paragraphs: [
            'The security of your Personal Data is important to us, but remember that no method of transmission over the Internet, or method of electronic storage, is 100% secure. While we strive to use commercially acceptable means to protect your Personal Data, we cannot guarantee its absolute security.',
          ],
        },
        {
          heading: 'Links to Other Websites',
          paragraphs: [
            "Our Service may contain links to other websites that are not operated by us. We strongly advise you to review the Privacy Policy of every site you visit — we have no control over and assume no responsibility for the content, privacy policies or practices of any third-party sites or services.",
          ],
        },
        {
          heading: 'Changes to this Privacy Policy',
          paragraphs: [
            'We may update our Privacy Policy from time to time. We will notify you of any changes by posting the new Privacy Policy on this page. You are advised to review this Privacy Policy periodically for any changes.',
          ],
        },
        {
          heading: 'Consent',
          paragraphs: ['By using our website, you hereby consent to our Privacy Policy and agree to its terms.'],
        },
        {
          heading: 'Contact Us',
          paragraphs: [
            'If you have any questions about this Privacy Policy, you can contact us via email at contact@vedastro.org.',
          ],
        },
      ]}
    />
  );
}
