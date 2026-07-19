import { StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { Spacing } from '@/constants/theme';

/**
 * Port of ViewComponents/Components/UnderConstructionHeader.razor. The original's "leave your
 * email" capture (SweetAlert2 text-input popup -> XML AddMessageApi endpoint) is not carried
 * over — that endpoint is XML-only and unrelated to this migration's JSON-standardization scope,
 * and the capture flow itself is a "nice to have" on an already-stubbed page, not the page's
 * point. This keeps the honest "not built yet" notice, which is the part every page depending on
 * this actually needs.
 */
export function UnderConstructionNotice() {
  return (
    <ThemedView style={styles.box} type="backgroundElement">
      <Icon name="construction" size={20} />
      <ThemedView style={styles.textCol}>
        <ThemedText type="smallBold">Under Construction</ThemedText>
        <ThemedText type="small" themeColor="textSecondary">
          This page is under construction. It might not work!
        </ThemedText>
      </ThemedView>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  box: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
    borderRadius: 12,
    padding: Spacing.three,
  },
  textCol: {
    gap: Spacing.half,
    flexShrink: 1,
  },
});
