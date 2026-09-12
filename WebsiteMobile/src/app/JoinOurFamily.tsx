import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { showErrorToast } from '@/lib/toast';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const EXPERIENCE_OPTIONS = [
  'Video Documentation',
  'Documentation in Plain Text',
  'Documentation in HTML, CSS',
  'Adobe Illustrator or Photoshop',
  'ML pipeline & AI model management',
  'Azure, Google Cloud, AWS, etc',
  'Vedic Astrology',
  'JS Code',
  'Python Code',
  'C# Code',
  'WebAssembly',
  'Office Clerk',
  'Others',
] as const;

/**
 * Port of Website/Pages/JoinOurFamily.razor. The original's "Join" button posted to the XML-only
 * AddMessageApi endpoint (never carried into the JSON API, same gap as Contact's message form) —
 * kept the form UI, but submitting is honest about that instead of faking success.
 */
export default function JoinOurFamilyScreen() {
  const [experience, setExperience] = useState<(typeof EXPERIENCE_OPTIONS)[number] | null>(null);

  function handleJoin() {
    if (!experience) {
      showErrorToast('Pick what you\'re interested in first.');
      return;
    }
    showErrorToast('This form isn\'t wired up to send messages yet — please reach out via the Contact page instead.');
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Join Our Family</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Brainstorm ideas, solve problems, learn & share new skills. We are not just a dev team,
          we are a dev family.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Where to begin?</ThemedText>
          <ThemedText style={styles.paragraph}>
            Get started today. Tell us what category of work you&apos;re interested in and
            we&apos;ll help you work on tasks best suited to your needs.
          </ThemedText>
          <ThemedView style={styles.chipRow}>
            {EXPERIENCE_OPTIONS.map((option) => (
              <Pressable
                key={option}
                onPress={() => setExperience(option)}
                style={[styles.chip, experience === option && styles.chipActive]}>
                <ThemedText type="small" themeColor={experience === option ? 'background' : 'text'}>
                  {option}
                </ThemedText>
              </Pressable>
            ))}
          </ThemedView>
          <Pressable onPress={handleJoin} style={styles.joinButton}>
            <ThemedText type="smallBold" themeColor="background">
              Join
            </ThemedText>
          </Pressable>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Publish changes</ThemedText>
          <ThemedText style={styles.paragraph}>
            Fork the repo, make your changes and push. Start with a simple draft, nothing
            complicated. Once you&apos;ve done this let us know — we will double check validity
            and upload to the cloud.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Office & Factory</ThemedText>
          <ThemedText style={styles.paragraph}>
            Our office is on Slack Street and the factory is located in GitHub Lane. We converse,
            discuss and share using Slack; GitHub is used to manage, track and assign tasks, and
            for source control up to deployment.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Who&apos;s Boss?</ThemedText>
          <ThemedText style={styles.paragraph}>
            There is no boss, manager, supervisor or owner for VedAstro. This is a public,
            non-profit, peer-to-peer project. All team members have equal rights — be your own
            manager and work for the common good.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Raise Awareness</ThemedText>
          <ThemedText style={styles.paragraph}>
            In a team without a boss, it is the responsibility of every member to guide others and
            say a word of caution when a teammate is going astray — convince, not order. Using this
            simple method, we&apos;ve done away with the need for managers, supervisors and bosses.
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
  section: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.one,
  },
  chip: {
    borderRadius: 999,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.one,
    backgroundColor: '#00000010',
  },
  chipActive: {
    backgroundColor: '#0d6efd',
  },
  joinButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#0d6efd',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
    marginTop: Spacing.two,
  },
});
