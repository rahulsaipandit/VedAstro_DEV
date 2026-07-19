import { storage } from './storage';
import { DEFAULT_AYANAMSA } from '@/lib/api/horoscope';

/**
 * "Advanced Options" calculation preferences (Ayanamsa/House system/Node type) — global, not
 * tied to any one person: per the user's own framing, these are "just a way of computing a
 * person's information", not part of the person's data. Stored locally only (see the Add/Edit
 * Person "settings" drawer), not written to the Person record on the backend.
 *
 * Only `ayanamsa` is currently read anywhere else (it seeds Horoscope's default so the choice
 * actually has an effect) — `houseSystem`/`nodeType` aren't wired into any Calculate/* endpoint
 * in this codebase yet, so changing them here doesn't yet change any chart output.
 */
export type CalculationPreferences = {
  ayanamsa: string;
  houseSystem: string;
  nodeType: string;
};

export const HOUSE_SYSTEM_OPTIONS = ['Placidus', 'Whole Sign', 'Koch', 'Equal'];
export const NODE_TYPE_OPTIONS = ['True Node', 'Mean Node'];

export const DEFAULT_CALCULATION_PREFERENCES: CalculationPreferences = {
  ayanamsa: DEFAULT_AYANAMSA,
  houseSystem: HOUSE_SYSTEM_OPTIONS[0],
  nodeType: NODE_TYPE_OPTIONS[0],
};

const STORAGE_KEY = 'calculation-preferences';

export async function loadCalculationPreferences(): Promise<CalculationPreferences> {
  const raw = await storage.get(STORAGE_KEY);
  if (!raw) return DEFAULT_CALCULATION_PREFERENCES;
  try {
    return { ...DEFAULT_CALCULATION_PREFERENCES, ...JSON.parse(raw) };
  } catch {
    return DEFAULT_CALCULATION_PREFERENCES;
  }
}

export async function saveCalculationPreferences(prefs: CalculationPreferences): Promise<void> {
  await storage.set(STORAGE_KEY, JSON.stringify(prefs));
}
