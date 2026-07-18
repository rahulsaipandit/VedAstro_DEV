import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import * as Crypto from 'expo-crypto';
import { apiEndpoints, getApiUrlDirect } from '../constants/urls';

/**
 * Replaces the static AppData class (ViewComponents/Code/Objects/AppData.cs).
 * debugMode persists the same way the old "DebugMode" localStorage key did
 * (see CLAUDE.md / Localhost_Setup.md's "Local API" sidebar toggle) except via
 * AsyncStorage so it works on web, iOS, and Android from one code path.
 *
 * GUEST MODE: sign-in is never mandatory. "101" is the same sentinel guest ID
 * the old Blazor app and the API both use (see Library/Data/UserData.cs's
 * `UserData.Guest`, PersonAPI.cs's "auto swap persons from visitor to logged
 * in account if user id is not 101" comment, MatchAPI.cs's userId "101").
 * Per-device data (person list, etc.) for a not-signed-in user is scoped by
 * `visitorId` instead, exactly like PersonTools.cs's
 * `ownerId = UserId == "101" ? VisitorID : UserId` - `effectiveOwnerId()`
 * below is that same rule in one place. Logging in later migrates that
 * visitor-scoped data to the real account automatically (PersonAPI.GetPersonList's
 * server-side SwapUserId, triggered by always sending both ids together).
 */
export type CurrentUser = {
  id: string;
  name: string;
  isGuest: boolean;
};

export const GUEST_USER_ID = '101';
const GUEST_USER: CurrentUser = { id: GUEST_USER_ID, name: 'Guest', isGuest: true };

type AppState = {
  debugMode: boolean;
  setDebugMode: (value: boolean) => void;

  currentUser: CurrentUser;
  setCurrentUser: (user: CurrentUser) => void;
  signOut: () => void;

  /** Replaces the "PreviousLoginMethod" localStorage key read by Login.razor's memory helper. */
  previousLoginMethod: string | null;
  setPreviousLoginMethod: (method: string) => void;

  /** Per-device anonymous ID, generated once on first use and persisted (replaces "VisitorId"). */
  visitorId: string;
  ensureVisitorId: () => string;

  /** The ID that should actually own data (person list, etc.) right now — see GUEST MODE note above. */
  effectiveOwnerId: () => string;

  darkMode: boolean;
  setDarkMode: (value: boolean) => void;

  /** Mirrors URL.ApiUrlDirect: local API when debugMode is on, deployed API otherwise. */
  apiUrlDirect: () => string;
  apiEndpoints: () => ReturnType<typeof apiEndpoints>;
};

export const useAppStore = create<AppState>()(
  persist(
    (set, get) => ({
      debugMode: false,
      setDebugMode: (value) => set({ debugMode: value }),

      currentUser: GUEST_USER,
      setCurrentUser: (user) => set({ currentUser: user }),
      signOut: () => set({ currentUser: GUEST_USER }),

      previousLoginMethod: null,
      setPreviousLoginMethod: (method) => set({ previousLoginMethod: method }),

      visitorId: '',
      ensureVisitorId: () => {
        const existing = get().visitorId;
        if (existing) return existing;
        const created = Crypto.randomUUID();
        set({ visitorId: created });
        return created;
      },

      // Pure read only — must NOT call ensureVisitorId()/set() here. Components call this from
      // render (via useAppStore selectors), and a set() during render breaks static export/SSR
      // (no `window` in that Node environment). RootLayout's mount effect guarantees visitorId
      // already exists by the time any real user interaction needs it.
      effectiveOwnerId: () => {
        const user = get().currentUser;
        return user.isGuest ? get().visitorId : user.id;
      },

      darkMode: false,
      setDarkMode: (value) => set({ darkMode: value }),

      apiUrlDirect: () => getApiUrlDirect(get().debugMode),
      apiEndpoints: () => apiEndpoints(get().apiUrlDirect()),
    }),
    {
      name: 'vedastro-app-store',
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({
        debugMode: state.debugMode,
        darkMode: state.darkMode,
        currentUser: state.currentUser,
        previousLoginMethod: state.previousLoginMethod,
        visitorId: state.visitorId,
      }),
    }
  )
);
