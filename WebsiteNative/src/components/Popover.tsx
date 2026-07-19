import { useState } from 'react';
import { Modal, Pressable, StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';

/**
 * Cross-platform replacement for tippy.js tooltips (no RN equivalent - see migration.md's
 * Phase 3 open questions). Deliberately small: tap-to-open/tap-to-close centered popover, not
 * a positioned hover tooltip (RN has no hover), since no ported component needs more than this
 * yet. `EventsChartViewer`'s heavier tippy usage is still unported and may need a fancier
 * anchored variant when that page is tackled.
 */
export function Popover({ trigger, content }: { trigger: React.ReactNode; content: string }) {
  const theme = useTheme();
  const [visible, setVisible] = useState(false);

  return (
    <>
      <Pressable onPress={() => setVisible(true)}>{trigger}</Pressable>

      <Modal visible={visible} transparent animationType="fade" onRequestClose={() => setVisible(false)}>
        <Pressable style={styles.backdrop} onPress={() => setVisible(false)}>
          <ThemedView style={[styles.card, { backgroundColor: theme.backgroundElement }]}>
            <ThemedText type="small">{content}</ThemedText>
          </ThemedView>
        </Pressable>
      </Modal>
    </>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0,0,0,0.25)',
    padding: Spacing.four,
  },
  card: {
    maxWidth: 320,
    borderRadius: 12,
    padding: Spacing.three,
  },
});
