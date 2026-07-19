import { useEffect, useState } from 'react';
import { Modal, Pressable, StyleSheet } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';

type ConfirmRequest = {
  title: string;
  message: string;
  resolve: (value: boolean) => void;
};

/**
 * Backs src/lib/confirm.ts's yes/no confirm() — the one thing in this app that genuinely needs a
 * blocking two-button dialog and has no existing cross-platform primitive to reuse (toast
 * notifications, the established pattern for everything else here, aren't built for blocking
 * decisions). Everywhere else, use showErrorToast/showSuccessToast from src/lib/toast.ts, which
 * already works correctly on every platform via react-native-toast-notifications' <ToastProvider>
 * — don't reach for this for plain messages.
 *
 * This exists only because RN's own `Alert.alert` doesn't: react-native-web's implementation is
 * a literal no-op (`static alert() {}`), so every confirm dialog built on it (delete
 * confirmations, Match's same-person/reversed-gender checks) silently did nothing on web.
 */
let showRequest: ((request: ConfirmRequest) => void) | null = null;

export function requestConfirm(title: string, message: string): Promise<boolean> {
  return new Promise((resolve) => {
    if (showRequest) {
      showRequest({ title, message, resolve });
    } else {
      console.warn('requestConfirm called before AlertHost mounted:', title);
      resolve(false);
    }
  });
}

export function AlertHost() {
  const theme = useTheme();
  const [request, setRequest] = useState<ConfirmRequest | null>(null);

  useEffect(() => {
    showRequest = setRequest;
    return () => {
      showRequest = null;
    };
  }, []);

  function handle(value: boolean) {
    request?.resolve(value);
    setRequest(null);
  }

  return (
    <Modal visible={!!request} transparent animationType="fade" onRequestClose={() => handle(false)}>
      <Pressable style={styles.backdrop} onPress={() => handle(false)}>
        <Pressable style={[styles.dialog, { backgroundColor: theme.background }]} onPress={(e) => e.stopPropagation()}>
          {request && (
            <>
              <ThemedText type="smallBold">{request.title}</ThemedText>
              <ThemedText type="small" themeColor="textSecondary" style={styles.message}>
                {request.message}
              </ThemedText>
              <ThemedView style={styles.buttonRow}>
                <Pressable onPress={() => handle(false)} style={styles.cancelButton}>
                  <ThemedText type="smallBold">Cancel</ThemedText>
                </Pressable>
                <Pressable onPress={() => handle(true)} style={styles.confirmButton}>
                  <ThemedText type="smallBold" themeColor="background">
                    Yes, sure!
                  </ThemedText>
                </Pressable>
              </ThemedView>
            </>
          )}
        </Pressable>
      </Pressable>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.4)',
    alignItems: 'center',
    justifyContent: 'center',
    padding: Spacing.four,
  },
  dialog: {
    width: '100%',
    maxWidth: 360,
    borderRadius: 12,
    padding: Spacing.four,
    gap: Spacing.two,
  },
  message: {
    lineHeight: 20,
  },
  buttonRow: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: Spacing.two,
    marginTop: Spacing.two,
  },
  cancelButton: {
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
    backgroundColor: '#00000010',
  },
  confirmButton: {
    paddingHorizontal: Spacing.four,
    paddingVertical: Spacing.two,
    borderRadius: 8,
    backgroundColor: '#1a9c4c',
  },
});
