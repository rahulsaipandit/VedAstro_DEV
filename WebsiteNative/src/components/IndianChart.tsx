import { Image, StyleSheet } from 'react-native';

import { ThemedView } from './themed-view';
import type { BirthTimeJson } from '@/lib/time';
import { getIndianChartImageUrl } from '@/lib/api/horoscope';

/**
 * Port of ViewComponents/Components/IndianChart.razor — also a plain <img> against a
 * server-rendered chart image (South/North style baked into the URL path), no interop lift.
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
      <Image
        source={{ uri: getIndianChartImageUrl(apiUrlDirect, birthTime, chartStyle) }}
        style={styles.image}
        resizeMode="contain"
      />
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
  image: {
    width: '100%',
    height: '100%',
  },
});
