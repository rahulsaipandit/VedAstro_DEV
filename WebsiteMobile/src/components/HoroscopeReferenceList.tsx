import { StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Spacing } from '@/constants/theme';
import type { HoroscopePrediction } from '@/lib/api/horoscope';

/**
 * Port of ViewComponents/Components/HoroscopeReferenceList.razor. Planet/House/Sign filter
 * dropdowns from the original are not ported yet (they were a partial/`todo` feature there too —
 * `_SearchButtonClicked` was a "coming soon" stub in the Blazor source) — this shows the full
 * unfiltered prediction list, same as that page's actual working behavior.
 */
export function HoroscopeReferenceList({ predictions }: { predictions: HoroscopePrediction[] }) {
  if (predictions.length === 0) {
    return (
      <ThemedText themeColor="textSecondary" style={styles.empty}>
        No predictions found.
      </ThemedText>
    );
  }

  return (
    <ThemedView style={styles.list}>
      {predictions.map((prediction, index) => (
        <ThemedView key={`${prediction.name}-${index}`} style={styles.row} type="backgroundElement">
          <ThemedText type="smallBold">{prediction.formattedName}</ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            {prediction.description}
          </ThemedText>
        </ThemedView>
      ))}
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  empty: {
    paddingVertical: Spacing.three,
  },
  list: {
    gap: Spacing.two,
  },
  row: {
    borderRadius: 10,
    padding: Spacing.three,
    gap: Spacing.half,
  },
});
