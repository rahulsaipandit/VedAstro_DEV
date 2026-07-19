import { requestConfirm } from '@/components/AlertHost';

/**
 * Promise-based yes/no confirm, replacing the old SweetAlert2 confirm-dialog pattern
 * (_jsRuntime.ShowAlertResult). Built on requestConfirm (a real Modal), not RN's `Alert.alert` —
 * react-native-web's Alert.alert is a no-op on web, which silently broke every confirm dialog
 * (delete confirmations, Match's same-person/reversed-gender checks) on that platform.
 */
export function confirm(title: string, message: string): Promise<boolean> {
  return requestConfirm(title, message);
}
