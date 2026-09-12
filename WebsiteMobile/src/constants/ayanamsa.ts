/**
 * Mirrors Library/Data/Enum/SimpleAyanamsa.cs ("Easy" group) and Library/Data/Enum/Ayanamsa.cs
 * ("Advanced" group, the raw Swiss Ephemeris list) — both are valid values for a Calculate/*
 * endpoint's `/Ayanamsa/{value}` segment (see Tools.EnumFromUrl: it tries the full Ayanamsa enum
 * first, then falls back to SimpleAyanamsa, so e.g. both "Raman" and "LAHIRI" resolve).
 */
export type AyanamsaOption = { label: string; value: string };

export const EASY_AYANAMSA_OPTIONS: AyanamsaOption[] = [
  { label: 'Lahiri Chitrapaksha', value: 'LahiriChitrapaksha' },
  { label: 'Krishnamurti KP', value: 'KrishnamurtiKP' },
  { label: 'Raman', value: 'Raman' },
  { label: 'Fagan Bradley (Western)', value: 'FaganBradley' },
  { label: 'J2000', value: 'J2000' },
  { label: 'Yukteshwar', value: 'Yukteshwar' },
];

const ADVANCED_AYANAMSA_NAMES = [
  'FAGAN_BRADLEY', 'LAHIRI', 'DELUCE', 'RAMAN', 'USHASHASHI', 'KRISHNAMURTI', 'DJWHAL_KHUL', 'YUKTESHWAR',
  'JN_BHASIN', 'BABYL_KUGLER1', 'BABYL_KUGLER2', 'BABYL_KUGLER3', 'BABYL_HUBER', 'BABYL_ETPSC',
  'ALDEBARAN_15TAU', 'HIPPARCHOS', 'SASSANIAN', 'GALCENT_0SAG', 'J2000', 'J1900', 'B1950', 'SURYASIDDHANTA',
  'SURYASIDDHANTA_MSUN', 'ARYABHATA', 'ARYABHATA_MSUN', 'SS_REVATI', 'SS_CITRA', 'TRUE_CITRA', 'TRUE_REVATI',
  'TRUE_PUSHYA', 'GALCENT_RGBRAND', 'GALEQU_IAU1958', 'GALEQU_TRUE', 'GALEQU_MULA', 'GALALIGN_MARDYKS',
  'TRUE_MULA', 'GALCENT_MULA_WILHELM', 'ARYABHATA_522', 'BABYL_BRITTON', 'TRUE_SHEORAN', 'GALCENT_COCHRANE',
  'GALEQU_FIORENZA', 'VALENS_MOON', 'LAHIRI_1940', 'LAHIRI_VP285', 'KRISHNAMURTI_VP291', 'LAHIRI_ICRC',
];

export const ADVANCED_AYANAMSA_OPTIONS: AyanamsaOption[] = ADVANCED_AYANAMSA_NAMES.map((name) => ({
  label: name,
  value: name,
}));

export const AYANAMSA_GROUPS = [
  { label: 'Easy', options: EASY_AYANAMSA_OPTIONS },
  { label: 'Advanced', options: ADVANCED_AYANAMSA_OPTIONS },
];
