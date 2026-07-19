import { useState } from 'react';
import { Pressable, StyleSheet, TextInput } from 'react-native';

import { ThemedText } from './themed-text';
import { ThemedView } from './themed-view';
import { GeoLocationInput, DEFAULT_GEO_LOCATION } from './GeoLocationInput';
import { BirthTimeInput, type BirthTimeInputValue } from './BirthTimeInput';
import { useTheme } from '@/hooks/use-theme';
import { Spacing } from '@/constants/theme';
import { PLANET_NAMES, HOUSE_NAMES } from '@/lib/api/horoscope';
import { getTimezoneOffsetForLocation, geoLocationToUrl, type GeoLocation } from '@/lib/api/geo';
import { buildBirthTimeJsonFromWallClock, timeToUrl } from '@/lib/time';
import type { CallParameter } from '@/lib/api/listCalls';

const CHIP_OPTIONS: Record<string, readonly string[]> = {
  'VedAstro.Library.PlanetName': PLANET_NAMES,
  'VedAstro.Library.HouseName': HOUSE_NAMES,
  'VedAstro.Library.Gender': ['Male', 'Female'],
  'VedAstro.Library.ZodiacName': [
    'Aries', 'Taurus', 'Gemini', 'Cancer', 'Leo', 'Virgo',
    'Libra', 'Scorpio', 'Sagittarius', 'Capricorn', 'Aquarius', 'Pisces',
  ],
};

export type ParamFieldValue =
  | { kind: 'text'; text: string }
  | { kind: 'time'; birthTime: BirthTimeInputValue }
  | { kind: 'location'; location: GeoLocation };

export function defaultParamValue(parameter: CallParameter): ParamFieldValue {
  if (parameter.parameterType === 'VedAstro.Library.Time') {
    return {
      kind: 'time',
      birthTime: { dd: '01', mm: '01', yyyy: '2000', hh: '00', min: '00', location: DEFAULT_GEO_LOCATION },
    };
  }
  if (parameter.parameterType === 'VedAstro.Library.GeoLocation') {
    return { kind: 'location', location: DEFAULT_GEO_LOCATION };
  }
  const chipOptions = CHIP_OPTIONS[parameter.parameterType];
  return { kind: 'text', text: chipOptions ? chipOptions[0] : '' };
}

/** Resolves a parameter's current UI value into the URL segment the reflection dispatcher expects. */
export async function resolveParamSegment(
  apiUrlDirect: string,
  parameter: CallParameter,
  value: ParamFieldValue
): Promise<string> {
  if (value.kind === 'time') {
    const bt = value.birthTime;
    const offset = await getTimezoneOffsetForLocation(
      apiUrlDirect,
      bt.location,
      new Date(Date.UTC(Number(bt.yyyy), Number(bt.mm) - 1, Number(bt.dd)))
    );
    const birthTimeJson = buildBirthTimeJsonFromWallClock(bt.dd, bt.mm, bt.yyyy, bt.hh, bt.min, offset, bt.location);
    return timeToUrl(birthTimeJson);
  }
  if (value.kind === 'location') {
    return geoLocationToUrl(value.location);
  }
  return `/${parameter.name}/${encodeURIComponent(value.text)}`;
}

/**
 * Renders the right input for a Calculate/* method parameter, based on its .NET type name (from
 * GET /api/ListAllCalls). Falls back to a plain text field for any type without a dedicated
 * picker — APIBuilder/TableGenerator are power-user tools, so typing a raw enum/number/TimeSpan
 * value is an acceptable fallback rather than building a picker for every possible type.
 */
export function DynamicParamField({
  apiUrlDirect,
  parameter,
  value,
  onChange,
}: {
  apiUrlDirect: string;
  parameter: CallParameter;
  value: ParamFieldValue;
  onChange: (value: ParamFieldValue) => void;
}) {
  const theme = useTheme();

  if (value.kind === 'time') {
    return (
      <ThemedView style={styles.field}>
        <ThemedText type="small" themeColor="textSecondary">
          {parameter.name} (Time)
        </ThemedText>
        <BirthTimeInput
          apiUrlDirect={apiUrlDirect}
          value={value.birthTime}
          onChange={(birthTime) => onChange({ kind: 'time', birthTime })}
        />
      </ThemedView>
    );
  }

  if (value.kind === 'location') {
    return (
      <ThemedView style={styles.field}>
        <GeoLocationInput
          apiUrlDirect={apiUrlDirect}
          location={value.location}
          onChange={(location) => onChange({ kind: 'location', location })}
          label={`${parameter.name} (Location)`}
        />
      </ThemedView>
    );
  }

  const chipOptions = CHIP_OPTIONS[parameter.parameterType];
  if (chipOptions) {
    return (
      <ThemedView style={styles.field}>
        <ThemedText type="small" themeColor="textSecondary">
          {parameter.name} ({parameter.parameterType.split('.').pop()})
        </ThemedText>
        <ThemedView style={styles.chipRow}>
          {chipOptions.map((option) => (
            <Pressable
              key={option}
              onPress={() => onChange({ kind: 'text', text: option })}
              style={[styles.chip, value.text === option && styles.chipActive]}>
              <ThemedText type="small" themeColor={value.text === option ? 'background' : 'text'}>
                {option}
              </ThemedText>
            </Pressable>
          ))}
        </ThemedView>
      </ThemedView>
    );
  }

  return (
    <ThemedView style={styles.field}>
      <ThemedText type="small" themeColor="textSecondary">
        {parameter.name} ({parameter.parameterType.split('.').pop()})
      </ThemedText>
      <TextInput
        value={value.text}
        onChangeText={(text) => onChange({ kind: 'text', text })}
        placeholder={parameter.description || parameter.name}
        placeholderTextColor={theme.textSecondary}
        style={[styles.input, { color: theme.text, borderColor: theme.backgroundSelected }]}
      />
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  field: {
    gap: Spacing.one,
  },
  chipRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: Spacing.one,
  },
  chip: {
    borderRadius: 999,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.one,
    backgroundColor: '#00000010',
  },
  chipActive: {
    backgroundColor: '#1a9c4c',
  },
  input: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: Spacing.three,
    paddingVertical: Spacing.two,
  },
});
