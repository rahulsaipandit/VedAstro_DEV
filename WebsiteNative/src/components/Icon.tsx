import { Heart, HeartCrack, HeartHandshake, HeartOff, Plus, Search, User, type LucideIcon } from 'lucide-react-native';

import { useTheme } from '@/hooks/use-theme';

/**
 * Replaces the old Iconify (`data-icon="..."`) icon system - there's no RN equivalent, and no
 * icon library was chosen until now (see migration.md's "Icon library" open question). This is
 * a small semantic registry, not a full 1:1 Iconify port: it covers what's actually used by
 * ported components so far (InfoBox, PersonSelector, MatchReport's heart-score summary), not
 * the ~100 distinct Iconify names used across the still-unported Blazor pages.
 */
const registry = {
  'heart-broken': HeartCrack,
  'heart-flash': HeartOff,
  'heart-half-full': Heart,
  'cards-heart': Heart,
  'heart-plus': HeartHandshake,
  plus: Plus,
  search: Search,
  user: User,
} as const satisfies Record<string, LucideIcon>;

export type IconName = keyof typeof registry;

export function Icon({ name, size = 20, color }: { name: IconName; size?: number; color?: string }) {
  const theme = useTheme();
  const LucideComponent = registry[name];
  return <LucideComponent size={size} color={color ?? theme.text} />;
}

/**
 * Maps MatchReport.Summary.HeartIcon (an Iconify name like "mdi:heart-plus", see
 * Library/Data/MatchReport.cs's GetSummary) onto our small registry above. Falls back to a
 * plain heart for anything unrecognized rather than crashing on an unmapped Iconify string.
 */
export function heartIconFromIconify(iconifyName: string): IconName {
  if (iconifyName.includes('heart-broken')) return 'heart-broken';
  if (iconifyName.includes('heart-flash')) return 'heart-flash';
  if (iconifyName.includes('heart-half')) return 'heart-half-full';
  if (iconifyName.includes('heart-plus') || iconifyName.includes('arrow-through-heart')) return 'heart-plus';
  return 'cards-heart';
}
