import { getApps, initializeApp } from 'firebase/app';
import { getAuth, initializeAuth } from 'firebase/auth';
import { Platform } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';

import { firebaseConfig } from './config';

const app = getApps().length ? getApps()[0]! : initializeApp(firebaseConfig);

/**
 * Web uses the SDK's own browser persistence; native needs an explicit AsyncStorage-backed
 * persistence layer (there's no `window.localStorage` to fall back to) - standard
 * Expo + Firebase Auth pattern. getReactNativePersistence must come from `@firebase/auth`
 * directly (not the `firebase` wrapper package's "firebase/auth" subpath) - the wrapper's
 * export map doesn't forward the "react-native" condition, so it always resolves to the
 * web build and lacks this export even though Metro can resolve @firebase/auth's RN entry
 * fine via package.json's "react-native" field/exports condition.
 */
export const auth = Platform.OS === 'web' ? getAuth(app) : initializeReactNativeAuth(app);

function initializeReactNativeAuth(firebaseApp: ReturnType<typeof initializeApp>) {
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  const { getReactNativePersistence } = require('@firebase/auth');
  return initializeAuth(firebaseApp, { persistence: getReactNativePersistence(AsyncStorage) });
}
