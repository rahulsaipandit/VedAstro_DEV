import AsyncStorage from '@react-native-async-storage/async-storage';

/**
 * Replaces the old Interop.getProperty/setProperty/removeProperty JS-interop wrapper
 * (BlazorJsInterop.cs -> js/Interop.js), which was localStorage-only. AsyncStorage
 * backs onto localStorage on web and native key-value storage on iOS/Android, so the
 * same calls work identically on every target — including the DebugMode local-API toggle.
 */
export const storage = {
  async get(key: string): Promise<string | null> {
    return AsyncStorage.getItem(key);
  },
  async set(key: string, value: string): Promise<void> {
    await AsyncStorage.setItem(key, value);
  },
  async remove(key: string): Promise<void> {
    await AsyncStorage.removeItem(key);
  },
};
