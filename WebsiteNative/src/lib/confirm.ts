import { Alert } from 'react-native';

/** Promise-based yes/no confirm, replacing the old SweetAlert2 confirm-dialog pattern (_jsRuntime.ShowAlertResult). */
export function confirm(title: string, message: string): Promise<boolean> {
  return new Promise((resolve) => {
    Alert.alert(title, message, [
      { text: 'Cancel', style: 'cancel', onPress: () => resolve(false) },
      { text: 'Yes, sure!', onPress: () => resolve(true) },
    ]);
  });
}
