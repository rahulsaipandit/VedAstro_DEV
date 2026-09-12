/**
 * Mirrors the 13 EventTag checkboxes rendered by Website/Pages/Calculator/GoodTimeFinder.razor
 * (lines 52-273) and Website_Mobile/js/GoodTimeFinder.js's EventsSelector allowedParentCheckboxes
 * list — the muhurtha/electional-astrology life-area filter, distinct from LifePredictor's
 * Dasa-period checkboxes. Values must be literal Library/Data/Enum/EventTag.cs member names
 * (comma-joined for the EventsChart URL's {eventTagsCsv} segment, see EventTagExtensions.FromString).
 * `comingSoon` tags (Studies, Building) have no EventManager data wired up yet server-side (Blazor
 * shows "Coming Soon!" in their tooltip) — kept selectable for parity, just flagged in the UI.
 */
export type EventTagOption = { value: string; label: string; comingSoon?: boolean };

export const EVENT_TAG_OPTIONS: EventTagOption[] = [
  { value: 'General', label: 'General' },
  { value: 'Personal', label: 'Personal' },
  { value: 'Agriculture', label: 'Agriculture' },
  { value: 'Studies', label: 'Studies', comingSoon: true },
  { value: 'Building', label: 'Building', comingSoon: true },
  { value: 'Travel', label: 'Travel' },
  { value: 'Astronomical', label: 'Astronomical' },
  { value: 'Marriage', label: 'Marriage' },
  { value: 'BuyingSelling', label: 'Buying/Selling' },
  { value: 'HairNailCutting', label: 'Hair/Nail Cutting' },
  { value: 'Medical', label: 'Medical' },
  { value: 'Dasa', label: 'Dasa' },
  { value: 'Gochara', label: 'Gochara' },
];

/** GoodTimeFinder's own default (Website_Mobile/js/GoodTimeFinder.js's defaultSelected) — NOT LifePredictor's PD1-PD7 Dasa-period default. */
export const GOOD_TIME_FINDER_DEFAULT_EVENT_TAGS = ['General', 'Personal'];

/**
 * Mirrors Algorithm.AllMethods (Library/Logic/Algorithms.cs:37-41) — the coloring algorithms used
 * to judge an event good/bad/neutral. CSV values must be exact method names (ChartOptions.FromUrl
 * resolves each token via `typeof(Algorithm).GetMethod(algoName)`).
 */
export const ALGORITHM_OPTIONS = [
  'Neutral',
  'General',
  'GocharaAshtakvargaBindu',
  'StrongestPlanet',
  'WeakestPlanet',
  'StrongestHouse',
  'WeakestHouse',
  'CombinedBad',
  'IshtaKashtaPhalaDegree',
  'PlanetStrengthDegree',
] as const;

/** Website_Mobile/js/GoodTimeFinder.js's AlgorithmsSelector default ("only uses Raman for Muhurtha"). */
export const GOOD_TIME_FINDER_DEFAULT_ALGORITHMS = ['General'];

/** Months for the Custom Year/Month range picker, mirroring MonthYearTimeRangeSelector.razor's <select>. */
export const MONTH_OPTIONS = [
  { label: 'JAN', value: 1 },
  { label: 'FEB', value: 2 },
  { label: 'MAR', value: 3 },
  { label: 'APR', value: 4 },
  { label: 'MAY', value: 5 },
  { label: 'JUN', value: 6 },
  { label: 'JUL', value: 7 },
  { label: 'AUG', value: 8 },
  { label: 'SEP', value: 9 },
  { label: 'OCT', value: 10 },
  { label: 'NOV', value: 11 },
  { label: 'DEC', value: 12 },
];
