import { useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet } from 'react-native';
import { useRouter } from 'expo-router';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { PersonSelector } from '@/components/PersonSelector';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { showErrorToast, showSuccessToast } from '@/lib/toast';
import {
  getBtrQuestions,
  scoreBirthTimeCandidates,
  type BtrAnswer,
  type BtrCandidateScore,
  type BtrQuestion,
} from '@/lib/api/birthTimeFinder';
import { addPerson } from '@/lib/api/person';
import type { Person } from '@/lib/api/person';
import { PageRoute } from '@/constants/routes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

const ANSWER_OPTIONS: { value: BtrAnswer; label: string }[] = [
  { value: 'StronglyNo', label: 'Strongly No' },
  { value: 'No', label: 'No' },
  { value: 'Skip', label: 'Skip / Maybe' },
  { value: 'Yes', label: 'Yes' },
  { value: 'StronglyYes', label: 'Strongly Yes' },
];

/**
 * Questionnaire-driven alternative to BirthTimeFinder.tsx's SVG-stack flow: answer life-event
 * questions (BtrQuestionBank, served from API/FrontDesk/BirthTimeScoringAPI.cs) instead of
 * visually comparing charts, and get back candidate birth times ranked by confidence. See
 * docs/BirthTimeFinder.md for the design (question -> btr-literature/karaka rule mapping,
 * scoring). v1 shows results as a ranked list with a confidence badge rather than the mockup's
 * literal radial "hover wheel" - same information, no new visualization primitive needed yet.
 */
export default function BirthTimeQuestionnaireScreen() {
  const theme = useTheme();
  const router = useRouter();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());

  const [person, setPerson] = useState<Person | null>(null);
  const [questions, setQuestions] = useState<BtrQuestion[]>([]);
  const [questionsLoading, setQuestionsLoading] = useState(false);
  const [answers, setAnswers] = useState<Record<number, BtrAnswer>>({});
  const [categoryIndex, setCategoryIndex] = useState(0);
  const [results, setResults] = useState<BtrCandidateScore[] | null>(null);
  const [scoring, setScoring] = useState(false);
  const [savingTime, setSavingTime] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setQuestionsLoading(true);
    getBtrQuestions(apiUrlDirect)
      .then((q) => { if (!cancelled) setQuestions(q); })
      .catch((e) => { if (!cancelled) showErrorToast(e.message ?? 'Failed to load questionnaire'); })
      .finally(() => { if (!cancelled) setQuestionsLoading(false); });
    return () => { cancelled = true; };
  }, [apiUrlDirect]);

  const categories = useMemo(() => {
    const seen: string[] = [];
    for (const q of questions) if (!seen.includes(q.category)) seen.push(q.category);
    return seen;
  }, [questions]);

  const currentCategory = categories[categoryIndex];
  const currentQuestions = useMemo(
    () => questions.filter((q) => q.category === currentCategory),
    [questions, currentCategory]
  );

  function setAnswer(questionId: number, answer: BtrAnswer) {
    setAnswers((current) => ({ ...current, [questionId]: answer }));
  }

  async function handleGetResults() {
    if (!person) {
      showErrorToast('Pick a person to rank possible birth times for.');
      return;
    }

    const answeredList = questions
      .map((q) => ({ questionId: q.id, answer: answers[q.id] ?? 'Skip' as BtrAnswer }))
      .filter((a) => a.answer !== 'Skip');

    if (answeredList.length === 0) {
      showErrorToast('Answer at least one question to rank candidate birth times.');
      return;
    }

    setScoring(true);
    try {
      const ranked = await scoreBirthTimeCandidates(apiUrlDirect, person.id, answeredList, { precisionInHours: 1 });
      setResults(ranked);
    } catch (e: any) {
      showErrorToast(e.message ?? 'Failed to score candidate birth times');
    } finally {
      setScoring(false);
    }
  }

  async function handleSaveAsProfile(candidate: BtrCandidateScore) {
    if (!person) return;
    setSavingTime(candidate.time);
    try {
      const birthTime = { StdTime: candidate.time, Location: person.birthTime.Location };
      await addPerson(apiUrlDirect, effectiveOwnerId, birthTime, `${person.name} (${candidate.time})`, person.gender);
      showSuccessToast('Saved as a new profile.');
    } catch (e: any) {
      showErrorToast(e.message ?? 'Failed to save profile');
    } finally {
      setSavingTime(null);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Birth Time Questionnaire</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Answer life-event questions and get candidate birth times ranked by confidence, instead
          of visually comparing chart stacks. See the{' '}
          <ThemedText
            type="link"
            onPress={() => router.push(`/${PageRoute.BirthTimeFinder}` as never)}>
            chart-comparison version
          </ThemedText>{' '}
          for the original flow.
        </ThemedText>

        <PersonSelector label="Person" selectedPerson={person} onSelectPerson={setPerson} />

        {questionsLoading && <ActivityIndicator />}

        {!questionsLoading && categories.length > 0 && (
          <ThemedView style={styles.questionnaire}>
            <ThemedText type="small" themeColor="textSecondary">
              Category {categoryIndex + 1} of {categories.length}: {currentCategory}
            </ThemedText>

            {currentQuestions.map((q) => (
              <ThemedView key={q.id} style={[styles.questionBlock, { borderColor: theme.backgroundSelected }]}>
                <ThemedText type="small">{q.text}</ThemedText>
                <ThemedView style={styles.chipRow}>
                  {ANSWER_OPTIONS.map((option) => {
                    const active = (answers[q.id] ?? 'Skip') === option.value;
                    return (
                      <Pressable
                        key={option.value}
                        onPress={() => setAnswer(q.id, option.value)}
                        style={[styles.chip, active && styles.chipActive]}>
                        <ThemedText type="small" themeColor={active ? 'background' : 'text'}>
                          {option.label}
                        </ThemedText>
                      </Pressable>
                    );
                  })}
                </ThemedView>
              </ThemedView>
            ))}

            <ThemedView style={styles.navRow}>
              <Pressable
                disabled={categoryIndex === 0}
                onPress={() => setCategoryIndex((i) => Math.max(0, i - 1))}
                style={[styles.navButton, categoryIndex === 0 && styles.navButtonDisabled]}>
                <ThemedText type="small" themeColor="background">Previous</ThemedText>
              </Pressable>
              <Pressable
                disabled={categoryIndex === categories.length - 1}
                onPress={() => setCategoryIndex((i) => Math.min(categories.length - 1, i + 1))}
                style={[styles.navButton, categoryIndex === categories.length - 1 && styles.navButtonDisabled]}>
                <ThemedText type="small" themeColor="background">Next</ThemedText>
              </Pressable>
            </ThemedView>

            <Pressable onPress={handleGetResults} style={styles.calculateButton}>
              {scoring
                ? <ActivityIndicator color="#fff" />
                : <ThemedText type="smallBold" themeColor="background">Rank Candidate Birth Times</ThemedText>}
            </Pressable>
          </ThemedView>
        )}

        {results && (
          <ThemedView style={styles.results}>
            <ThemedText type="smallBold">Ranked Candidate Birth Times</ThemedText>
            {results.map((c) => (
              <ThemedView key={c.time} style={[styles.resultRow, { borderColor: theme.backgroundSelected }]}>
                <ThemedView style={styles.resultInfo}>
                  <ThemedText type="small">{c.time}</ThemedText>
                  <ThemedText type="small" themeColor="textSecondary">
                    {c.confidencePercent}% · {c.bucket}
                  </ThemedText>
                </ThemedView>
                <Pressable
                  disabled={savingTime === c.time}
                  onPress={() => handleSaveAsProfile(c)}
                  style={styles.saveButton}>
                  <ThemedText type="small" themeColor="background">
                    {savingTime === c.time ? 'Saving…' : 'Save as profile'}
                  </ThemedText>
                </Pressable>
              </ThemedView>
            ))}
          </ThemedView>
        )}
      </ThemedView>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scrollContent: { alignItems: 'center' },
  page: {
    width: '100%',
    maxWidth: MaxContentWidth,
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    paddingBottom: Spacing.six,
    gap: Spacing.four,
  },
  subtitle: { marginBottom: Spacing.one },
  questionnaire: { gap: Spacing.three },
  questionBlock: {
    borderWidth: 1,
    borderRadius: 8,
    padding: Spacing.three,
    gap: Spacing.two,
  },
  chipRow: { flexDirection: 'row', flexWrap: 'wrap', gap: Spacing.one },
  chip: {
    borderRadius: 999,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.one,
    backgroundColor: '#00000010',
  },
  chipActive: { backgroundColor: '#0d6efd' },
  navRow: { flexDirection: 'row', gap: Spacing.two },
  navButton: {
    backgroundColor: '#6c757d',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
  navButtonDisabled: { opacity: 0.4 },
  calculateButton: {
    backgroundColor: '#0d6efd',
    alignSelf: 'flex-start',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  results: { gap: Spacing.two },
  resultRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderWidth: 1,
    borderRadius: 8,
    padding: Spacing.three,
  },
  resultInfo: { gap: 2 },
  saveButton: {
    backgroundColor: '#198754',
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    borderRadius: 8,
  },
});
