import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { getNameNumber, getNameNumberPrediction } from '@/lib/api/numerology';
import { showErrorToast } from '@/lib/toast';
import { MaxContentWidth, Spacing } from '@/constants/theme';

type ExampleRow = { name: string; number: number; prediction: string };

// Same 3 sample names as Numerology.razor's AccuratePredictionExampleList — computed lazily on
// first mount rather than at module load, since that needs a network call.
const EXAMPLE_NAMES = ['THOMAS ALVA EDISON', 'ADOLF HITLER', 'MICHAEL JACKSON'];

/** Port of Website/Pages/Calculator/Numerology.razor. */
export default function NumerologyScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());

  const [nameInput, setNameInput] = useState('');
  const [calculating, setCalculating] = useState(false);
  const [result, setResult] = useState<{ number: number; prediction: string } | null>(null);

  async function handleCalculate() {
    if (!nameInput.trim()) {
      showErrorToast('Please enter a name!');
      return;
    }
    setCalculating(true);
    try {
      const [number, prediction] = await Promise.all([
        getNameNumber(apiUrlDirect, nameInput.trim()),
        getNameNumberPrediction(apiUrlDirect, nameInput.trim()),
      ]);
      setResult({ number, prediction });
    } finally {
      setCalculating(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Numerology</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          A person&apos;s life can be predicted through his name spellings. From Mantra Shastra,
          uses vibration frequency of alphabets.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Name Number</ThemedText>
          <ThemedView style={[styles.inputRow, { borderColor: theme.backgroundSelected }]}>
            <TextInput
              value={nameInput}
              onChangeText={setNameInput}
              placeholder="Enter name"
              placeholderTextColor={theme.textSecondary}
              style={[styles.input, { color: theme.text }]}
            />
            <Pressable onPress={handleCalculate} style={styles.calculateButton} disabled={calculating}>
              {calculating ? (
                <ActivityIndicator size="small" color="#ffffff" />
              ) : (
                <ThemedText type="smallBold" themeColor="background">
                  Calculate
                </ThemedText>
              )}
            </Pressable>
          </ThemedView>

          {result && (
            <ThemedView style={styles.resultBox} type="backgroundElement">
              <ThemedText type="smallBold">Number: {result.number}</ThemedText>
              <ThemedText style={styles.predictionText}>{result.prediction}</ThemedText>
            </ThemedView>
          )}
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Accurate</ThemedText>
          <ExampleTable apiUrlDirect={apiUrlDirect} />
        </ThemedView>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">What is Numerology?</ThemedText>
          <ThemedText style={styles.paragraph}>
            To put things in a nutshell, every man or woman is represented by a Number (since he
            is born on a particular date, month and year) and also defined by letters pertaining
            to their names.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            According to these sastras the man is a Yantra and his name is a Mantra. Favourable
            results can be obtained only if both agree with each other and any disagreement will
            be harmful to them. This is the basic foundation of this science.
          </ThemedText>
          <ThemedText type="subtitle">Source of Numerology?</ThemedText>
          <ThemedText style={styles.paragraph}>
            Mantra Sastra helps us to understand the latent powers of nature, and we learn to
            command them through sound vibrations. Finding out the forms of those invisible powers
            and then using them is the aim of Tantra Sastra.
          </ThemedText>
          <ThemedText style={styles.paragraph}>
            Only these two mutually related sastras can help us to understand nature and live in
            accordance with it. These two sastras have been kept secret to this day lest it should
            fall in the hands of the unscrupulous. These are still sustained only under a
            Guru-Disciple set-up.
          </ThemedText>
        </ThemedView>
      </ThemedView>
    </ScrollView>
  );
}

function ExampleTable({ apiUrlDirect }: { apiUrlDirect: string }) {
  const [rows, setRows] = useState<ExampleRow[] | null>(null);

  useEffect(() => {
    Promise.all(
      EXAMPLE_NAMES.map(async (name) => {
        const [number, prediction] = await Promise.all([
          getNameNumber(apiUrlDirect, name),
          getNameNumberPrediction(apiUrlDirect, name),
        ]);
        return { name, number, prediction: prediction.slice(0, 194) };
      })
    ).then(setRows);
  }, [apiUrlDirect]);

  if (!rows) return <ActivityIndicator style={styles.loading} />;

  return (
    <ThemedView style={styles.table}>
      {rows.map((row) => (
        <ThemedView key={row.name} style={styles.tableRow} type="backgroundElement">
          <ThemedText type="smallBold">
            {row.name} ({row.number})
          </ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            {row.prediction}
          </ThemedText>
        </ThemedView>
      ))}
    </ThemedView>
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
    gap: Spacing.five,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  section: {
    gap: Spacing.two,
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
    paddingVertical: Spacing.two,
  },
  calculateButton: {
    backgroundColor: '#0d6efd',
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
    margin: Spacing.one,
  },
  resultBox: {
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.one,
  },
  predictionText: {
    lineHeight: 22,
  },
  loading: {
    alignSelf: 'flex-start',
  },
  table: {
    gap: Spacing.two,
  },
  tableRow: {
    borderRadius: 10,
    padding: Spacing.three,
    gap: Spacing.half,
  },
  paragraph: {
    lineHeight: 22,
  },
});
