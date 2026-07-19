import { useEffect } from 'react';
import { DarkTheme, DefaultTheme, Stack, ThemeProvider } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useColorScheme } from 'react-native';
import { ToastProvider } from 'react-native-toast-notifications';

import { AnimatedSplashOverlay } from '@/components/animated-icon';
import { DebugModeToggle } from '@/components/DebugModeToggle';
import { useAppStore } from '@/store/useAppStore';

SplashScreen.preventAutoHideAsync();

// Stack navigation, not tabs — the old site is ~65 routed pages (see
// src/constants/routes.ts / PageRoute.cs), not a small tab-bar app.
export default function RootLayout() {
  const colorScheme = useColorScheme();

  // Create the per-device visitor ID (guest-mode identity, see useAppStore.ts) once, here,
  // in a mount effect — never during render, since a set() at render time breaks static
  // export/SSR (no `window` in that Node environment).
  useEffect(() => {
    useAppStore.getState().ensureVisitorId();
  }, []);

  return (
    <ThemeProvider value={colorScheme === 'dark' ? DarkTheme : DefaultTheme}>
      <ToastProvider placement="top" duration={3000}>
        <AnimatedSplashOverlay />
        <Stack screenOptions={{ headerShown: false }} />
        <DebugModeToggle />
      </ToastProvider>
    </ThemeProvider>
  );
}
