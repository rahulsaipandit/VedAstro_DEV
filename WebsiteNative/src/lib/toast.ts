import { Toast } from 'react-native-toast-notifications';

/**
 * Thin wrapper around react-native-toast-notifications' global imperative ref - the
 * cross-platform replacement for SweetAlert2's `Swal.fire` success/error toasts (see
 * migration.md's Phase 3 open questions). `<ToastProvider>` wraps the app root in
 * _layout.tsx, which is what makes this global ref available; this module is just the
 * call-site API, mirroring the shape of the old `_jsRuntime.ShowAlert(type, message)` helper.
 */
export function showSuccessToast(message: string) {
  Toast.show(message, { type: 'success' });
}

export function showErrorToast(message: string) {
  Toast.show(message, { type: 'danger' });
}
