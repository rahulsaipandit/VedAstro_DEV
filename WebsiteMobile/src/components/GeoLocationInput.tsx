import { useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { Icon } from './Icon';
import { Dropdown } from './Dropdown';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { COUNTRIES } from '@/constants/countries';
import { reverseGeocodeLocation, searchGeoLocation, type GeoLocation } from '@/lib/api/geo';
import { showErrorToast } from '@/lib/toast';

const COUNTRY_OPTIONS = COUNTRIES.map((name) => ({ label: name, value: name }));
const AccentColor = '#2F6FED';

// Nominatim's display_name ends with the country ("Bhopal, Madhya Pradesh, India") — pull that
// last segment and match it against our own COUNTRIES list so the Country chip stays in sync with
// whatever was actually resolved. Matters beyond just UX: handleSearch appends the currently
// selected country to the next query as a disambiguation hint, so a stale/mismatched selection
// silently corrupts the next search (e.g. "Bhopal, United States" resolves to nonsense).
function matchCountryFromDisplayName(displayName: string): string | null {
  const parts = displayName.split(',').map((p) => p.trim());
  const last = parts[parts.length - 1];
  if (!last) return null;
  return COUNTRIES.find((c) => c.toLowerCase() === last.toLowerCase()) ?? null;
}

/**
 * Port of ViewComponents/Components/GeoLocationInput.razor. Real geocoding, not a stub — backed
 * by Calculate.AddressToGeoLocation for forward search and Calculate.CoordinatesToGeoLocation for
 * reverse geocoding (both free Nominatim/OpenStreetMap lookups, no key needed, see
 * src/lib/api/geo.ts). Redesigned per the "Add New Person" mock: a single search box with an
 * inline "Locate" action (browser Geolocation API — web only; falls back to showing the raw
 * coordinates if reverse geocoding fails/times out), a compact Country chip to narrow the search,
 * and a "Show coordinates" toggle instead of always displaying Long/Lat.
 */
export function GeoLocationInput({
  apiUrlDirect,
  location,
  onChange,
  label = 'Birth location',
  defaultCountry = '',
}: {
  apiUrlDirect: string;
  location: GeoLocation;
  onChange: (location: GeoLocation) => void;
  label?: string;
  defaultCountry?: string;
}) {
  const theme = useTheme();
  const [country, setCountry] = useState(defaultCountry);
  const [nameInput, setNameInput] = useState(location.name);
  const [searching, setSearching] = useState(false);
  const [detecting, setDetecting] = useState(false);
  const [notFound, setNotFound] = useState(false);
  const [showCoords, setShowCoords] = useState(false);

  async function handleSearch() {
    if (!nameInput.trim()) return;
    setSearching(true);
    setNotFound(false);
    try {
      const trimmed = nameInput.trim();
      const query = country ? `${trimmed}, ${country}` : trimmed;
      let found = await searchGeoLocation(apiUrlDirect, query);

      // A stale/mismatched Country selection can push Nominatim into a bogus match instead of
      // failing outright (e.g. "Bhopal, United States" resolves to "United States, United
      // States"). Sanity-check the result actually mentions what was typed; if not, the country
      // filter likely fought the real match, so retry without it.
      if (found && country && !found.name.toLowerCase().includes(trimmed.toLowerCase())) {
        found = await searchGeoLocation(apiUrlDirect, trimmed);
      }

      if (found) {
        onChange(found);
        setNameInput(found.name);
        const matchedCountry = matchCountryFromDisplayName(found.name);
        if (matchedCountry) setCountry(matchedCountry);
      } else {
        setNotFound(true);
      }
    } finally {
      setSearching(false);
    }
  }

  function handleDetectLocation() {
    if (typeof navigator === 'undefined' || !navigator.geolocation) {
      showErrorToast('Location detection is not supported on this device');
      return;
    }
    setDetecting(true);
    navigator.geolocation.getCurrentPosition(
      async (position) => {
        const { latitude, longitude } = position.coords;
        try {
          const resolved = await reverseGeocodeLocation(apiUrlDirect, latitude, longitude);
          const detected: GeoLocation = resolved ?? { name: `${latitude.toFixed(4)}, ${longitude.toFixed(4)}`, latitude, longitude };
          onChange(detected);
          setNameInput(detected.name);
          const matchedCountry = matchCountryFromDisplayName(detected.name);
          if (matchedCountry) setCountry(matchedCountry);
        } finally {
          setDetecting(false);
        }
      },
      () => {
        showErrorToast('Could not detect your location — check location permissions');
        setDetecting(false);
      }
    );
  }

  return (
    <ThemedView style={styles.container}>
      <ThemedText style={styles.fieldLabel}>
        {label}
        <ThemedText style={styles.requiredAsterisk}> *</ThemedText>
      </ThemedText>

      <ThemedView style={[styles.searchRow, { borderColor: theme.backgroundSelected }]}>
        <Pressable onPress={handleSearch} disabled={searching} hitSlop={8} style={styles.searchIconButton}>
          {searching ? (
            <ActivityIndicator size="small" color={theme.textSecondary} />
          ) : (
            <Icon name="search" size={16} color={theme.textSecondary} />
          )}
        </Pressable>
        <TextInput
          value={nameInput}
          onChangeText={setNameInput}
          onSubmitEditing={handleSearch}
          placeholder="city, town or state"
          placeholderTextColor={theme.textSecondary}
          style={[styles.searchInput, { color: theme.text }]}
        />
        <Pressable
          onPress={handleDetectLocation}
          disabled={detecting}
          style={[styles.locateButton, { backgroundColor: theme.backgroundSelected }]}>
          {detecting || searching ? (
            <ActivityIndicator size="small" color={AccentColor} />
          ) : (
            <Icon name="detect-location" size={14} color={AccentColor} />
          )}
          <ThemedText type="small" style={{ color: AccentColor, fontWeight: '600' }}>
            Locate
          </ThemedText>
        </Pressable>
      </ThemedView>

      {notFound && (
        <ThemedText type="small" themeColor="textSecondary">
          &quot;{nameInput}&quot; not found — check spelling or try a nearby place.
        </ThemedText>
      )}

      <ThemedView style={styles.metaRow}>
        <ThemedView style={[styles.countryChip, { borderColor: theme.backgroundSelected }]}>
          <ThemedText type="small" themeColor="textSecondary">
            Country:
          </ThemedText>
          <Dropdown
            value={country}
            options={COUNTRY_OPTIONS}
            onChange={setCountry}
            placeholder="Any"
            label="Country"
            searchable
            bordered={false}
          />
        </ThemedView>

        <Pressable onPress={() => setShowCoords((s) => !s)} hitSlop={8} style={styles.coordsToggle}>
          <ThemedText type="small" style={{ color: AccentColor }}>
            {showCoords ? 'Hide coordinates' : 'Show coordinates'}
          </ThemedText>
          <Icon name="chevron-down" size={14} color={AccentColor} />
        </Pressable>
      </ThemedView>

      {showCoords && (
        <ThemedView style={[styles.coordsBox, { backgroundColor: theme.backgroundElement }]}>
          <ThemedText type="small" themeColor="textSecondary">
            Long: {location.longitude.toFixed(4)}
          </ThemedText>
          <ThemedText type="small" themeColor="textSecondary">
            Lat: {location.latitude.toFixed(4)}
          </ThemedText>
        </ThemedView>
      )}
    </ThemedView>
  );
}

export const DEFAULT_GEO_LOCATION: GeoLocation = { name: 'Singapore', longitude: 103.8198, latitude: 1.3521 };

const styles = StyleSheet.create({
  container: {
    gap: Spacing.two,
  },
  fieldLabel: {
    fontSize: 13,
    fontWeight: '600',
  },
  requiredAsterisk: {
    color: '#D64545',
  },
  searchRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.one,
    borderWidth: 1,
    borderRadius: 8,
    paddingLeft: Spacing.one,
    paddingRight: Spacing.one,
    minHeight: 44,
  },
  searchIconButton: {
    padding: Spacing.two,
    alignItems: 'center',
    justifyContent: 'center',
  },
  searchInput: {
    flex: 1,
    paddingVertical: Spacing.two,
    outlineWidth: 0,
  },
  locateButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.half,
    borderRadius: 8,
    paddingHorizontal: Spacing.two,
    paddingVertical: Spacing.two,
  },
  metaRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    flexWrap: 'wrap',
    gap: Spacing.two,
  },
  countryChip: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.two,
    borderWidth: 1,
    borderRadius: 999,
    paddingLeft: Spacing.three,
    paddingRight: Spacing.one,
    paddingVertical: Spacing.half,
    flex: 1,
    minWidth: 200,
  },
  coordsToggle: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: Spacing.half,
  },
  coordsBox: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.three,
    borderRadius: 10,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
  },
});
