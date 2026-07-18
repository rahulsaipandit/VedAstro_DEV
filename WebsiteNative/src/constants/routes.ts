/**
 * Mirrors ViewComponents/Code/Managers/PageRoute.cs — the Blazor route-constant
 * source of truth. Kept 1:1 with that file so porting a page is a lookup, not a guess.
 * expo-router file paths live under src/app/ and don't have to match these strings;
 * this file is for building nav hrefs/links that read the same as the old app.
 */
export const PageRoute = {
  TaskList: '/Tasklist',
  MessageList: '/MessageList',
  SearchResult: '/SearchResult',
  Debug: '/Debug',
  AskAstrologer: '/AskAstrologer',
  TrainAIAstrologer: '/TrainAIAstrologer',

  // Docs
  QuickGuide: 'Docs/QuickGuide',
  Glossary: 'Docs/Glossary',

  // Journal
  Journal: 'Journal',
  JournalEditor: 'Journal/Editor',

  // Calculators
  CalculatorList: 'Calculator/',
  LifePredictor: 'LifePredictor',
  GoodTimeFinder: 'GoodTimeFinder',
  StarsAboveMe: 'StarsAboveMe',
  TableGenerator: 'TableGenerator',
  TimeListGenerator: 'TimeListGenerator',
  Numerology: 'Numerology',
  SunRiseSetTime: 'SunRiseSetTime',
  BirthTimeFinder: 'BirthTimeFinder',
  LocalMeanTime: 'LocalMeanTime',
  Horoscope: 'Horoscope',
  FamilyChart: 'FamilyChart',
  APIBuilder: 'APIBuilder',

  // Match
  Match: 'Match',
  MatchReport: 'Match/Report',
  SavedMatchReports: 'Match/Saved',
  MatchProfile: 'Match/Profile',
  MatchFinder: 'Match/Finder',

  // Donate
  ThankYou: 'Donate/ThankYou',
  Donate: 'Donate/',
  DonatePayment: 'Donate/Payment',

  // Account
  UserAccount: 'Account/',
  UserAccountGuest: 'Account/Guest',
  Login: 'Account/Login',
  SavedCharts: 'Account/SavedCharts',
  PersonList: 'Account/Person/List',
  AddPerson: 'Account/Person/Add',
  Import: 'Account/Person/Import',
  PersonEditor: 'Account/Person/Editor',

  // Little pages
  NowInDwapara: '/NowInDwapara',
  Remedy: '/Remedy',
  Download: '/Download',
  VisitorList: '/VisitorList',
  FAQ: '/FAQ',
  TaskEditor: '/TaskEditor',
  About: '/About',
  PrivacyPolicy: '/PrivacyPolicy',
  ShippingDelivery: '/ShippingDelivery',
  CancellationRefund: '/CancellationRefund',
  TermsOfService: '/TermsOfService',
  ChatAPI: '/ChatAPI',
  Payment: '/Payment',
  Sponsor: '/Sponsor',
  VSLifeSharePublicSession: '/VSLifeSharePublicSession',
  PrivateServer: '/PrivateServer',
  JoinOurFamily: '/JoinOurFamily',
  BodyTypes: '/BodyTypes',
  Contact: '/Contact',
  MadeOnEarth: '/MadeOnEarth',
  FeatureList: '/FeatureList',
  Home: '/',

  BlogWhyVedic: '/Blog/WhyVedic',
} as const;
