import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { DynamicParamField, defaultParamValue, resolveParamSegment, type ParamFieldValue } from '@/components/DynamicParamField';
import { useTheme } from '@/hooks/use-theme';
import { useAppStore } from '@/store/useAppStore';
import { getAllCalls, type CallMetadata } from '@/lib/api/listCalls';
import { MaxContentWidth, Spacing } from '@/constants/theme';

/**
 * Port of Website/Pages/Calculator/APIBuilder.razor. The original built a dynamic parameter form
 * from the same `/api/ListAllCalls` reflection metadata this uses — same idea, RN inputs instead
 * of Blazor `<select>`/`<input>` elements. Confirmed live that `DefaultValue` is essentially never
 * populated in this metadata, so every parameter is treated as required rather than trying to
 * guess which are "optional" (an explicit value is always valid anyway).
 */
export default function APIBuilderScreen() {
  const theme = useTheme();
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());

  const [calls, setCalls] = useState<CallMetadata[] | null>(null);
  const [search, setSearch] = useState('');
  const [selected, setSelected] = useState<CallMetadata | null>(null);
  const [paramValues, setParamValues] = useState<ParamFieldValue[]>([]);
  const [generatedUrl, setGeneratedUrl] = useState<string | null>(null);
  const [generating, setGenerating] = useState(false);
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<string | null>(null);

  useEffect(() => {
    getAllCalls(apiUrlDirect).then(setCalls);
  }, [apiUrlDirect]);

  const filtered = calls?.filter((c) => c.name.toLowerCase().includes(search.toLowerCase())).slice(0, 40) ?? [];

  function selectCall(call: CallMetadata) {
    setSelected(call);
    setParamValues(call.parameters.map(defaultParamValue));
    setGeneratedUrl(null);
    setTestResult(null);
  }

  async function handleGenerate() {
    if (!selected) return;
    setGenerating(true);
    try {
      const segments = await Promise.all(
        selected.parameters.map((param, index) => resolveParamSegment(apiUrlDirect, param, paramValues[index]))
      );
      setGeneratedUrl(`${apiUrlDirect}/Calculate/${selected.name}${segments.join('')}`);
      setTestResult(null);
    } finally {
      setGenerating(false);
    }
  }

  async function handleTestCall() {
    if (!generatedUrl) return;
    setTesting(true);
    try {
      const response = await fetch(generatedUrl);
      const json = await response.json();
      setTestResult(JSON.stringify(json, null, 2));
    } catch (e) {
      setTestResult(e instanceof Error ? e.message : 'Call failed');
    } finally {
      setTesting(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Open API</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Advanced astronomical data via a simple HTTP request for free — build a call below to
          see the exact URL, then use it in your own app.
        </ThemedText>

        <ThemedView style={styles.section}>
          <ThemedText type="subtitle">Calculator</ThemedText>
          {!calls ? (
            <ActivityIndicator style={styles.loading} />
          ) : (
            <>
              <TextInput
                value={search}
                onChangeText={setSearch}
                placeholder="Search calculators..."
                placeholderTextColor={theme.textSecondary}
                style={[styles.searchInput, { color: theme.text, borderColor: theme.backgroundSelected }]}
              />
              <ThemedView style={styles.callList}>
                {filtered.map((call) => (
                  <Pressable key={call.name} onPress={() => selectCall(call)} style={styles.callRow}>
                    <ThemedView
                      style={[
                        styles.callRowInner,
                        selected?.name === call.name && { backgroundColor: theme.backgroundSelected },
                      ]}>
                      <ThemedText type="smallBold">{call.name}</ThemedText>
                      {!!call.description && (
                        <ThemedText type="small" themeColor="textSecondary" numberOfLines={1}>
                          {call.description}
                        </ThemedText>
                      )}
                    </ThemedView>
                  </Pressable>
                ))}
              </ThemedView>
            </>
          )}
        </ThemedView>

        {selected && (
          <ThemedView style={styles.section}>
            <ThemedText type="subtitle">Input Parameters</ThemedText>
            {selected.parameters.length === 0 ? (
              <ThemedText type="small" themeColor="textSecondary">
                This calculator takes no parameters.
              </ThemedText>
            ) : (
              selected.parameters.map((param, index) => (
                <DynamicParamField
                  key={param.name}
                  apiUrlDirect={apiUrlDirect}
                  parameter={param}
                  value={paramValues[index]}
                  onChange={(value) =>
                    setParamValues((prev) => prev.map((v, i) => (i === index ? value : v)))
                  }
                />
              ))
            )}

            <Pressable onPress={handleGenerate} disabled={generating} style={styles.generateButton}>
              {generating ? (
                <ActivityIndicator size="small" color="#ffffff" />
              ) : (
                <ThemedText type="smallBold" themeColor="background">
                  Generate
                </ThemedText>
              )}
            </Pressable>
          </ThemedView>
        )}

        {generatedUrl && (
          <ThemedView style={styles.section}>
            <ThemedText type="small" themeColor="textSecondary">
              URL
            </ThemedText>
            <ThemedView style={styles.urlBox} type="backgroundElement">
              <ThemedText type="code" selectable style={styles.urlText}>
                {generatedUrl}
              </ThemedText>
            </ThemedView>
            <Pressable onPress={handleTestCall} disabled={testing} style={styles.testButton}>
              {testing ? (
                <ActivityIndicator size="small" color="#ffffff" />
              ) : (
                <ThemedText type="smallBold" themeColor="background">
                  Test Call
                </ThemedText>
              )}
            </Pressable>
            {testResult && (
              <ThemedView style={styles.urlBox} type="backgroundElement">
                <ThemedText type="code" selectable>
                  {testResult}
                </ThemedText>
              </ThemedView>
            )}
          </ThemedView>
        )}
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
  loading: {
    marginVertical: Spacing.three,
  },
  searchInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
  },
  callList: {
    gap: Spacing.half,
  },
  callRow: {},
  callRowInner: {
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
    gap: Spacing.half,
  },
  generateButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
  urlBox: {
    borderRadius: 10,
    padding: Spacing.three,
  },
  urlText: {
    lineHeight: 20,
  },
  testButton: {
    alignSelf: 'flex-start',
    backgroundColor: '#1a9c4c',
    paddingHorizontal: Spacing.five,
    paddingVertical: Spacing.three,
    borderRadius: 8,
  },
});
