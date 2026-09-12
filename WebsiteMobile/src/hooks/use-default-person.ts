import { useEffect, useState } from 'react';

import { useAppStore } from '@/store/useAppStore';
import { getPersonList, getPublicPersonList, type Person } from '@/lib/api/person';

/**
 * Resolves the last person picked via any PersonSelector (see useAppStore's
 * `lastUsedPersonId`) into a real Person, so calculator screens can default to them instead of
 * making the user re-pick every visit. Falls back silently to null if the remembered id no
 * longer matches anyone in the current owner's list (e.g. the person was deleted, or the owner
 * changed after logout) - never assumes a remembered id is still valid.
 */
export function useDefaultPerson() {
  const apiUrlDirect = useAppStore((s) => s.apiUrlDirect());
  const effectiveOwnerId = useAppStore((s) => s.effectiveOwnerId());
  const visitorId = useAppStore((s) => s.visitorId);
  const lastUsedPersonId = useAppStore((s) => s.lastUsedPersonId);
  const setLastUsedPersonId = useAppStore((s) => s.setLastUsedPersonId);

  const [person, setPersonState] = useState<Person | null>(null);

  useEffect(() => {
    if (!lastUsedPersonId) return;
    let cancelled = false;
    Promise.all([getPersonList(apiUrlDirect, effectiveOwnerId, visitorId), getPublicPersonList(apiUrlDirect)]).then(
      ([own, pub]) => {
        if (cancelled) return;
        const match = [...own, ...pub].find((p) => p.id === lastUsedPersonId) ?? null;
        setPersonState(match);
      }
    );
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function setPerson(next: Person) {
    setPersonState(next);
    setLastUsedPersonId(next.id);
  }

  return { person, setPerson };
}
