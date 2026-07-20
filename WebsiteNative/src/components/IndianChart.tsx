import { StyleSheet } from 'react-native';
import { SvgUri } from 'react-native-svg';

import { ThemedView } from './themed-view';
import type { BirthTimeJson } from '@/lib/time';
import { getIndianChartImageUrl } from '@/lib/api/horoscope';

/**
 * Port of ViewComponents/Components/IndianChart.razor — a server-rendered SVG chart image
 * (South/North style baked into the URL path). Uses react-native-svg's SvgUri rather than
 * RN's built-in Image, which only decodes SVG on web (via the browser) - not on native
 * iOS/Android, where it would just render blank.
 */
export function IndianChart({
  apiUrlDirect,
  birthTime,
  chartStyle,
}: {
  apiUrlDirect: string;
  birthTime: BirthTimeJson;
  chartStyle: 'South' | 'North';
}) {
  return (
    <ThemedView style={styles.container} type="backgroundElement">
      <SvgUri uri={getIndianChartImageUrl(apiUrlDirect, birthTime, chartStyle)} width="100%" height="100%" />
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  container: {
    borderRadius: 12,
    overflow: 'hidden',
    aspectRatio: 1,
    width: '100%',
    maxWidth: 500,
    alignSelf: 'center',
  },
});
