import { Image, StyleSheet } from 'react-native';

import { ThemedView } from './themed-view';
import type { BirthTimeJson } from '@/lib/time';
import { getSkyChartImageUrl } from '@/lib/api/horoscope';

/**
 * Port of ViewComponents/Components/SkyChartViewer.razor — that component is nothing but an
 * <img> pointing at a server-rendered chart image, so this is a straight <Image> port, no
 * interop needed (see migration.md's Phase 3 notes on chart components with no interop lift).
 */
export function SkyChartViewer({ apiUrlDirect, birthTime }: { apiUrlDirect: string; birthTime: BirthTimeJson }) {
  return (
    <ThemedView style={styles.container} type="backgroundElement">
      <Image
        source={{ uri: getSkyChartImageUrl(apiUrlDirect, birthTime) }}
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
