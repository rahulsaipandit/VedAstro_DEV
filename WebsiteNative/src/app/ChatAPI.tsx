import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PersonSelector } from '@/components/PersonSelector';
import { Icon } from '@/components/Icon';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { askHoroscopeChat, askHoroscopeFollowUpChat, sendChatFeedback } from '@/lib/api/chat';
import type { Person } from '@/lib/api/person';
import { MaxContentWidth, Spacing } from '@/constants/theme';

type ChatBubble = {
  role: 'user' | 'ai';
  text: string;
  hash?: string;
  feedback?: 'good' | 'bad';
};

/**
 * Port of Website/Pages/ChatAPI.razor. The original delegated the entire chat widget to
 * vedastro.js's GenerateHoroscopeChat (a separate JS-built UI) — this is a from-scratch RN chat
 * screen over the same real backend (Calculate.HoroscopeChat/HoroscopeFollowUpChat/
 * HoroscopeChatFeedback, see src/lib/api/chat.ts), not a markup port. Requests are synchronous
 * and can take minutes against a real LLM (see CLAUDE.md/ChatEndpointsTests.cs), so there's no
 * client-side timeout — just a patient loading state.
 */
export default function ChatAPIScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());

  const [person, setPerson] = useState<Person | null>(null);
  const [messages, setMessages] = useState<ChatBubble[]>([]);
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [lastHash, setLastHash] = useState<string | null>(null);

  async function handleSend() {
    if (!person) {
      setError('Select a person first — predictions are based on their horoscope.');
      return;
    }
    const question = input.trim();
    if (!question) return;

    setError(null);
    setInput('');
    setMessages((prev) => [...prev, { role: 'user', text: question }]);
    setSending(true);
    try {
      const reply =
        sessionId && lastHash
          ? await askHoroscopeFollowUpChat(apiUrlDirect, person.birthTime, question, lastHash, effectiveOwnerId, sessionId)
          : await askHoroscopeChat(apiUrlDirect, person.birthTime, question, effectiveOwnerId);
      setSessionId(reply.sessionId);
      setLastHash(reply.textHash);
      setMessages((prev) => [...prev, { role: 'ai', text: reply.text, hash: reply.textHash }]);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Chat request failed');
    } finally {
      setSending(false);
    }
  }

  async function handleFeedback(index: number, hash: string, good: boolean) {
    setMessages((prev) => prev.map((m, i) => (i === index ? { ...m, feedback: good ? 'good' : 'bad' } : m)));
    try {
      await sendChatFeedback(apiUrlDirect, hash, good ? 1 : -1);
    } catch {
      // feedback is best-effort UX polish, not worth surfacing an error for
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Chat API</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Talk with an AI powered astrologer to help learn, discuss and improve your knowledge on
          astrology.
        </ThemedText>

        <PersonSelector label="Person" selectedPerson={person} onSelectPerson={setPerson} />

        <ThemedView style={styles.chatBox} type="backgroundElement">
          {messages.length === 0 && (
            <ThemedText type="small" themeColor="textSecondary">
              Ask a question about {person ? person.name + "'s" : 'the selected person\'s'}{' '}
              horoscope — e.g. &quot;Will I be rich?&quot;
            </ThemedText>
          )}
          {messages.map((message, index) => (
            <ThemedView
              key={index}
              style={[styles.bubble, message.role === 'user' ? styles.userBubble : styles.aiBubble]}>
              <ThemedText style={message.role === 'user' ? styles.userText : undefined}>{message.text}</ThemedText>
              {message.role === 'ai' && message.hash && (
                <ThemedView style={styles.feedbackRow}>
                  <Pressable onPress={() => handleFeedback(index, message.hash!, true)}>
                    <Icon
                      name="heart-plus"
                      size={16}
                      color={message.feedback === 'good' ? '#1a9c4c' : theme.textSecondary}
                    />
                  </Pressable>
                  <Pressable onPress={() => handleFeedback(index, message.hash!, false)}>
                    <Icon
                      name="heart-broken"
                      size={16}
                      color={message.feedback === 'bad' ? '#d33' : theme.textSecondary}
                    />
                  </Pressable>
                </ThemedView>
              )}
            </ThemedView>
          ))}
          {sending && (
            <ThemedView style={styles.thinkingRow}>
              <ActivityIndicator size="small" />
              <ThemedText type="small" themeColor="textSecondary">
                Thinking… a real answer can take a few minutes.
              </ThemedText>
            </ThemedView>
          )}
          {error && (
            <ThemedText type="small" style={styles.errorText}>
              {error}
            </ThemedText>
          )}
        </ThemedView>

        <ThemedView style={[styles.inputRow, { borderColor: theme.backgroundSelected }]}>
          <TextInput
            value={input}
            onChangeText={setInput}
            onSubmitEditing={handleSend}
            placeholder="Ask about your horoscope..."
            placeholderTextColor={theme.textSecondary}
            style={[styles.input, { color: theme.text }]}
          />
          <Pressable onPress={handleSend} disabled={sending} style={styles.sendButton}>
            <ThemedText type="smallBold" themeColor="background">
              Send
            </ThemedText>
          </Pressable>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Instant Learning</ThemedText>
          <ThemedText style={styles.paragraph}>
            Every feedback you give is instantly added to the model&apos;s decision paths — this AI
            chatbot effectively learns from mistakes and errors, thanks to guidance from users like
            you. Anonymous learning models made from live learning are uploaded to open-source
            HuggingFace, benefiting the whole world for generations to come.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Democratize AI</ThemedText>
          <ThemedText style={styles.paragraph}>
            Get this intelligent astro AI chatbot into your custom app or website in no time —
            contact us and we can help you set up and customize AI chat for you at zero cost. This
            project is 100% open source because we want everybody to have access to advanced
            astrological tools, not just people with extra income.
          </ThemedText>
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Reverse Engineer</ThemedText>
          <ThemedText style={styles.paragraph}>
            To maximize the use of the Chat API, understand its basic structure — the best way is
            to play with it. Try modifying the API URL in your browser and see what data you get.
            The API is designed to handle this, so don&apos;t worry about breaking it.
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
  chatBox: {
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.two,
    minHeight: 120,
  },
  bubble: {
    borderRadius: 10,
    padding: Spacing.three,
    maxWidth: '85%',
    gap: Spacing.one,
  },
  userBubble: {
    backgroundColor: '#1a9c4c',
    alignSelf: 'flex-end',
  },
  aiBubble: {
    backgroundColor: '#00000010',
    alignSelf: 'flex-start',
  },
  userText: {
    color: '#ffffff',
  },
  feedbackRow: {
    flexDirection: 'row',
    gap: Spacing.two,
  },
  thinkingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
  },
  errorText: {
    color: '#d33',
  },
  inputRow: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    borderRadius: 8,
    paddingLeft: Spacing.three,
  },
  input: {
    flex: 1,
    paddingVertical: Spacing.three,
  },
  sendButton: {
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    margin: Spacing.one,
    borderRadius: 8,
  },
  section: {
    gap: Spacing.two,
  },
  paragraph: {
    lineHeight: 22,
  },
});
