import { StyleSheet } from 'react-native';
import { SvgUri } from 'react-native-svg';

import { ThemedView } from './themed-view';
import type { BirthTimeJson } from '@/lib/time';
import { getSkyChartImageUrl } from '@/lib/api/horoscope';

/**
 * Port of ViewComponents/Components/SkyChartViewer.razor — a server-rendered SVG chart image.
 * Uses react-native-svg's SvgUri rather than RN's built-in Image, which only decodes SVG on
 * web (via the browser) - not on native iOS/Android, where it would just render blank.
 */
export function SkyChartViewer({ apiUrlDirect, birthTime }: { apiUrlDirect: string; birthTime: BirthTimeJson }) {
  return (
    <ThemedView style={styles.container} type="backgroundElement">
      <SvgUri uri={getSkyChartImageUrl(apiUrlDirect, birthTime)} width="100%" height="100%" />
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
