import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';
import AsyncStorage from '@react-native-async-storage/async-storage';
import * as Crypto from 'expo-crypto';

import type { BirthTimeJson } from '@/lib/time';

/**
 * "My Tithi" - a user's own tracked tithi occurrences (e.g. a death-anniversary tithi), the
 * screenshot's "My Tithi" tab. See docs/designHinduCalendar.md's open questions: resolved as a
 * purely device-local feature (this store), not synced to the server or attached to a Person -
 * no backend change at all. Each entry's next occurrence is resolved the same way
 * VedicBirthday.tsx finds a person's Vedic birthday: Calculate.VedicBirthDate anchors on the
 * entry's own referenceDate (whatever tithi+lunar-month it names) and finds that tithi's
 * recurrence in a target year - "My Tithi" is that exact same mechanism applied to an arbitrary
 * saved date instead of a birth date, so it needed no new calculator.
 */
export type MyTithiEntry = {
  id: string;
  label: string;
  referenceDate: BirthTimeJson;
};

type MyTithiState = {
  entries: MyTithiEntry[];
  addEntry: (label: string, referenceDate: BirthTimeJson) => void;
  removeEntry: (id: string) => void;
};

export const useMyTithiStore = create<MyTithiState>()(
  persist(
    (set) => ({
      entries: [],
      addEntry: (label, referenceDate) =>
        set((state) => ({ entries: [...state.entries, { id: Crypto.randomUUID(), label, referenceDate }] })),
      removeEntry: (id) => set((state) => ({ entries: state.entries.filter((e) => e.id !== id) })),
    }),
    {
      name: 'vedastro-my-tithi-store',
      storage: createJSONStorage(() => AsyncStorage),
    }
  )
);
