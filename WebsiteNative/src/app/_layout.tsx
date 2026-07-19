import { useEffect } from 'react';
import { DarkTheme, DefaultTheme, Stack, ThemeProvider } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { useColorScheme, View } from 'react-native';
import { ToastProvider } from 'react-native-toast-notifications';

import { AnimatedSplashOverlay } from '@/components/animated-icon';
import { AlertHost } from '@/components/AlertHost';
import { AppHeader } from '@/components/AppHeader';
import { AppFooter } from '@/components/AppFooter';
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
        <View style={{ flex: 1 }}>
          <AppHeader />
          <View style={{ flex: 1 }}>
            <Stack screenOptions={{ headerShown: false }} />
          </View>
          <AppFooter />
        </View>
        <AlertHost />
      </ToastProvider>
    </ThemeProvider>
  );
}
