import { LegalPageLayout } from '@/components/LegalPageLayout';

/** Port of Website/Pages/TermsOfService.razor. */
export default function TermsOfServiceScreen() {
  return (
    <LegalPageLayout
      title="Terms Of Service"
      sections={[
        {
          heading: '1. Terms',
          paragraphs: [
            'By accessing this website, accessible from vedastro.org, you are agreeing to be bound by these Terms and Conditions of Use and agree that you are responsible for compliance with any applicable local laws. If you disagree with any of these terms, you are prohibited from accessing this site. The materials contained in this website are protected by copyright and trademark law.',
          ],
        },
        {
          heading: '2. Use License',
          paragraphs: [
            "Permission is granted to temporarily download one copy of the materials on VedAstro's website for personal, non-commercial transitory viewing only. Under this license you may not: modify or copy the materials; use the materials for any commercial purpose or public display; attempt to reverse engineer any software on the website; remove any copyright or proprietary notations; or transfer the materials to another person or mirror them on any other server.",
            'VedAstro may terminate this license upon violation of any of these restrictions, at which point your viewing right is also terminated.',
          ],
        },
        {
          heading: '3. Disclaimer',
          paragraphs: [
            'All materials on VedAstro\'s website are provided "as is". VedAstro makes no warranties, expressed or implied, and hereby disclaims all other warranties, including accuracy or reliability of the materials or any sites linked to this website.',
          ],
        },
        {
          heading: '4. Limitations',
          paragraphs: [
            'VedAstro or its suppliers will not be held accountable for any damages arising from the use or inability to use the materials on this website, even if notified of the possibility of such damage.',
          ],
        },
        {
          heading: '5. Revisions and Errata',
          paragraphs: [
            'The materials on this website may include technical, typographical, or photographic errors. VedAstro does not promise the materials are accurate, complete, or current, and may change them at any time without notice.',
          ],
        },
        {
          heading: '6. Links',
          paragraphs: [
            'VedAstro has not reviewed all sites linked to its website and is not responsible for their contents. Use of any linked website is at the user\'s own risk.',
          ],
        },
        {
          heading: '7. Site Terms of Use Modifications',
          paragraphs: [
            'VedAstro may revise these Terms of Use at any time without prior notice. By using this website, you agree to be bound by the current version of these Terms.',
          ],
        },
        { heading: '8. Your Privacy', paragraphs: ['Please read our Privacy Policy.'] },
        {
          heading: '9. Governing Law',
          paragraphs: ['Any claim related to VedAstro\'s website shall be governed by applicable law without regard to conflict-of-law provisions.'],
        },
      ]}
    />
  );
}
