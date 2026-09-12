import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { showErrorToast } from '@/lib/toast';
import { useTheme } from '@/hooks/use-theme';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const QUESTION_OPTIONS = [
  'When is my marriage?',
  'Should I get a divorce?',
  'When is my job promotion?',
  'My good & bad periods in life?',
  'Can I invest in risky business?',
  'Will I win the lottery?',
  'Other',
] as const;

/**
 * Port of Website/Pages/AskAstrologer.razor. The original's "Send" button posted to the XML-only
 * AddMessageApi endpoint (never carried into the JSON API) — form UI kept, submission is honest
 * about that gap instead of faking success, same treatment as Contact/JoinOurFamily.
 */
export default function AskAstrologerScreen() {
  const theme = useTheme();
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [question, setQuestion] = useState<(typeof QUESTION_OPTIONS)[number] | ''>('');
  const [details, setDetails] = useState('');

  function handleSend() {
    if (!email.trim()) {
      showErrorToast('Please enter your email so we know how to reach you.');
      return;
    }
    if (!question) {
      showErrorToast('Please select a question, or choose Other.');
      return;
    }
    showErrorToast('This form isn\'t wired up to send messages yet — please reach out via the Contact page instead.');
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Ask Astrologer</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          In our non-profit effort to help people, we provide FREE Vedic astrology service. Ask
          our astrologers about your finance, marriage, health, job, etc.
        </ThemedText>

        <Pressable onPress={() => router.push(`/${PageRoute.Donate}` as never)}>
          <ThemedText type="linkPrimary">Donate to support this service</ThemedText>
        </Pressable>

        <ThemedView style={styles.field}>
          <ThemedText type="small" themeColor="textSecondary">
            Email
          </ThemedText>
          <TextInput
            value={email}
            onChangeText={setEmail}
            placeholder="john@example.com"
            placeholderTextColor={theme.textSecondary}
            style={[styles.input, { color: theme.text, borderColor: theme.backgroundSelected }]}
          />
        </ThemedView>

        <ThemedView style={styles.field}>
          <ThemedText type="small" themeColor="textSecondary">
            Question
          </ThemedText>
          <ThemedView style={styles.chipRow}>
            {QUESTION_OPTIONS.map((option) => (
              <Pressable
                key={option}
                onPress={() => setQuestion(option)}
                style={[styles.chip, question === option && styles.chipActive]}>
                <ThemedText type="small" themeColor={question === option ? 'background' : 'text'}>
                  {option}
                </ThemedText>
              </Pressable>
            ))}
          </ThemedView>
        </ThemedView>

        <ThemedView style={styles.field}>
          <ThemedText type="small" themeColor="textSecondary">
            Details
          </ThemedText>
          <TextInput
            value={details}
            onChangeText={setDetails}
            placeholder="Enter extra info about your question"
            placeholderTextColor={theme.textSecondary}
            multiline
            numberOfLines={5}
            style={[styles.textArea, { color: theme.text, borderColor: theme.backgroundSelected }]}
          />
        </ThemedView>

        <Pressable onPress={handleSend} style={styles.sendButton}>
          <ThemedText type="smallBold" themeColor="background">
            Send
          </ThemedText>
        </Pressable>

        <ThemedView style={styles.section}>
          <ThemedText type="smallBold">How long to get a reply?</ThemedText>
          <ThemedText type="small" themeColor="textSecondary" style={styles.paragraph}>
            Our astrologers will try to reply as soon as possible. It takes time for an astrologer
            to analyse your chart and give an accurate reading — depending on your question and
            astrologer availability, the time will differ.
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
    gap: Spacing.three,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  field: {
    gap: Spacing.one,
  },
  input: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
  },
  textArea: {
    borderWidth: 1,
    borderRadius: 8,
    padding: Spacing.three,
    minHeight: 120,
    textAlignVertical: 'top',
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
  sendButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#0d6efd',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
  section: {
    gap: Spacing.one,
    marginTop: Spacing.two,
  },
  paragraph: {
    lineHeight: 20,
  },
});
