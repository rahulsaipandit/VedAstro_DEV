using System;
using System.Collections.Generic;
using System.Linq;
using static VedAstro.Library.PlanetName;

namespace VedAstro.Library
{
    /// <summary>
    /// Generates a full Hindu festival calendar for a Gregorian year, computed fresh from
    /// Swiss-Ephemeris-backed tithi/lunar-month/solar-longitude searches (the same search style
    /// as <see cref="Calculate.VedicBirthDate"/> and <see cref="Calculate.TajikaDateForYear"/>) -
    /// not a static per-year lookup table. See docs/vedAstroArchitecture.md's "Festival Calendar
    /// Generator" section for the design and its known limitations.
    /// </summary>
    public partial class Calculate
    {
        /// <summary>
        /// Gets a given festival's date for a Gregorian year.
        ///
        /// Month names here are all in the Amanta reckoning <see cref="Calculate.LunarMonth"/>
        /// itself uses (new-moon-to-new-moon), NOT the Purnimanta reckoning (full-moon-to-full-moon)
        /// most mainstream/North Indian sources name festivals with. The two conventions agree on
        /// the Shukla-paksha half of every month (tithi 1-15) but disagree by exactly one month
        /// name on the Krishna-paksha half (tithi 16-30): a Krishna-paksha tithi popularly known as
        /// "<c>X</c> Krishna ..." is Amanta month <c>X-1</c>. Confirmed against real dates: Diwali
        /// (popularly "Kartik Amavasya") is Amanta Aaswayuja's Amavasya (12/11/2023); Maha
        /// Shivaratri and Karwa Chauth are Krishna-paksha tithis and shifted the same way. Ramnavami,
        /// Rakshabandhan, Holi (its Pratipada day still falls within Amanta Phaalguna) and Chhath
        /// are all Shukla-paksha or land in the shared half, so need no shift.
        /// </summary>
        public static Time FestivalDate(FestivalName festival, int year, GeoLocation location)
        {
            return festival switch
            {
                FestivalName.MakarSankranti => MakarSankrantiDate(year, location),
                FestivalName.Ramnavami => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Chaitra, 9, year, location),
                FestivalName.Holi => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Phaalguna, 16, year, location),
                FestivalName.Rakshabandhan => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Sraavana, 15, year, location),
                FestivalName.KarwaChauth => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Aaswayuja, 19, year, location),
                FestivalName.Chhath => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Kaarteeka, 6, year, location),
                FestivalName.Diwali => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Aaswayuja, 30, year, location),
                FestivalName.MahaShivaratri => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Maagha, 29, year, location),
                FestivalName.HartalikaTeej => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Bhaadrapada, 3, year, location),
                FestivalName.GaneshChaturthi => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Bhaadrapada, 4, year, location),
                FestivalName.AnantChaturdashi => FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Bhaadrapada, 14, year, location),
                _ => throw new Exception($"Festival {festival} not supported!")
            };
        }

        /// <summary>
        /// Gets every festival's date for a Gregorian year, keyed by <see cref="FestivalName"/>.
        /// </summary>
        public static Dictionary<FestivalName, Time> FestivalCalendar(int year, GeoLocation location)
        {
            var calendar = new Dictionary<FestivalName, Time>();

            foreach (FestivalName festival in Enum.GetValues(typeof(FestivalName)))
            {
                calendar[festival] = FestivalDate(festival, year, location);
            }

            return calendar;
        }

        /// <summary>
        /// Finds the exact moment a given tithi occurs within the Nija (regular, non-Adhika)
        /// occurrence of a named lunar month, in a given Gregorian year. Adhika (leap) months are
        /// deliberately skipped when matching the requested month name, since festivals are
        /// classically observed only in a month's Nija occurrence - its Adhika repeat is skipped.
        /// Scans ~15 synodic months starting well before the target year, since an Adhika month
        /// shifts every later month's Gregorian date within the year by up to ~29.5 days.
        /// </summary>
        private static Time FindTithiInNijaMonth(LunarMonth nijaMonth, int tithiNumber, int year, GeoLocation location)
        {
            var monthStart = PreviousNewMoon(new Time($"00:00 01/12/{year - 1} +00:00", location));

            for (var i = 0; i < 15; i++)
            {
                //sample a point safely inside this synodic month (never right at either edge) to name it
                var sampleTime = monthStart.AddHours(24 * 10);

                if (LunarMonth(sampleTime) == nijaMonth)
                {
                    var tithiInstant = FindTithiInstant(monthStart, tithiNumber);
                    if (tithiInstant.GetStdDateTimeOffset().Year == year) { return tithiInstant; }
                }

                monthStart = NextNewMoon(monthStart.AddHours(24));
            }

            throw new Exception($"Could not find {nijaMonth} tithi {tithiNumber} in {year} - error!");
        }

        /// <summary>
        /// Finds a moment squarely inside a given tithi (1-30) within the synodic month that starts
        /// at <paramref name="monthStart"/> (a new moon) - a Newton-style search on Moon-Sun
        /// elongation, the same convergence pattern as <see cref="Calculate.FindSyzygy"/>/
        /// <see cref="Calculate.VedicBirthDate"/>, targeting the tithi's midpoint (6° into its 12°
        /// band) rather than its exact start boundary - <see cref="Calculate.LunarDay"/> derives the
        /// tithi number via ceiling, so converging to a start boundary risks landing a hair below it
        /// (floating-point/convergence-tolerance noise) and reading back as the previous tithi.
        /// </summary>
        private static Time FindTithiInstant(Time monthStart, int tithiNumber)
        {
            const double synodicDegreesPerDay = 360.0 / 29.530588;
            var targetElongation = (tithiNumber - 1) * 12.0 + 6.0;

            double Elongation(Time t)
            {
                var moon = PlanetNirayanaLongitude(Moon, t).TotalDegrees;
                var sun = PlanetNirayanaLongitude(Sun, t).TotalDegrees;
                return ((moon - sun) % 360.0 + 360.0) % 360.0;
            }

            var daysGuess = targetElongation / synodicDegreesPerDay;
            var candidate = monthStart.AddHours(daysGuess * 24);

            for (var i = 0; i < 8; i++)
            {
                var currentElongation = Elongation(candidate);
                var diff = ((targetElongation - currentElongation + 540.0) % 360.0) - 180.0;

                if (Math.Abs(diff) < 0.0005) { break; }

                var adjustDays = diff / synodicDegreesPerDay;
                candidate = candidate.AddHours(adjustDays * 24);
            }

            return candidate;
        }

        /// <summary>
        /// Gets the Chaturmas period (the ~4-month span during which Vishnu is classically said to
        /// sleep) for a Gregorian year: Ashadha Shukla Ekadashi (tithi 11) to Kartika Shukla
        /// Ekadashi (tithi 11), both found the same way <see cref="FestivalDate"/> finds any other
        /// named-month tithi.
        /// </summary>
        public static TimeRange ChaturmasPeriod(int year, GeoLocation location)
        {
            var start = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Aashaadha, 11, year, location);
            var end = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Kaarteeka, 11, year, location);

            return new TimeRange(start, end);
        }

        /// <summary>
        /// Classical Ekadashi names, keyed by this codebase's Amanta lunar month convention (see
        /// <see cref="LunarMonth"/>'s own doc comment) - most published Ekadashi-name calendars
        /// instead use Purnimanta month names, which are offset from Amanta by exactly one month
        /// on the Krishna-paksha half (the same shift <see cref="FestivalDate"/>'s doc comment
        /// describes for Diwali/Holi/MahaShivaratri/KarwaChauth): an Amanta month's Krishna
        /// Ekadashi carries the *next* Purnimanta month's Krishna-paksha name, while its Shukla
        /// Ekadashi keeps the same name under either convention.
        ///
        /// Cross-checked against 3 anchor points that are unambiguous regardless of convention
        /// (their Shukla-paksha names don't shift): Nirjala Ekadashi (Jyeshtha Shukla), Mokshada
        /// Ekadashi (Maargasira Shukla, Gita Jayanti), and Rama Ekadashi (Aaswayuja Krishna, falls
        /// shortly before Diwali - matches <see cref="FestivalDate"/>'s own confirmed Diwali
        /// date). Not independently verified against a full published year-calendar beyond that;
        /// treat as best-effort pending that check, same caveat this file already applies to its
        /// tithi-based festival conversions. Adhika (leap) months have no classical Ekadashi names
        /// of their own - conventionally called "Purushottama"/"Kamala" Ekadashi and not modelled
        /// here; an Adhika month's Ekadashi is named after its underlying Nija month instead (same
        /// modulo-12 fallback <see cref="VedicBirthDate"/> uses).
        /// </summary>
        private static readonly Dictionary<LunarMonth, (string Shukla, string Krishna)> EkadashiNamesByAmantaMonth = new()
        {
            [global::VedAstro.Library.LunarMonth.Chaitra] = ("Kamada", "Varuthini"),
            [global::VedAstro.Library.LunarMonth.Vaisaakha] = ("Mohini", "Apara"),
            [global::VedAstro.Library.LunarMonth.Jyeshtha] = ("Nirjala", "Yogini"),
            [global::VedAstro.Library.LunarMonth.Aashaadha] = ("Devshayani", "Kamika"),
            [global::VedAstro.Library.LunarMonth.Sraavana] = ("Putrada", "Aja"),
            [global::VedAstro.Library.LunarMonth.Bhaadrapada] = ("Parsva", "Indira"),
            [global::VedAstro.Library.LunarMonth.Aaswayuja] = ("Papankusha", "Rama"),
            [global::VedAstro.Library.LunarMonth.Kaarteeka] = ("Prabodhini", "Utpanna"),
            [global::VedAstro.Library.LunarMonth.Maargasira] = ("Mokshada", "Saphala"),
            [global::VedAstro.Library.LunarMonth.Pushya] = ("Putrada", "Shattila"),
            [global::VedAstro.Library.LunarMonth.Maagha] = ("Jaya", "Vijaya"),
            [global::VedAstro.Library.LunarMonth.Phaalguna] = ("Amalaki", "Papmochani"),
        };

        private static string EkadashiName(Time date, bool isShuklaPaksha)
        {
            var month = LunarMonth(date);
            var nijaMonth = (int)month > 12 ? (LunarMonth)((int)month - 12) : month; //Adhika falls back to its base Nija month's name

            var names = EkadashiNamesByAmantaMonth[nijaMonth];
            return isShuklaPaksha ? names.Shukla : names.Krishna;
        }

        /// <summary>
        /// Gets every occurrence of a given tithi number across every synodic month in and around
        /// a Gregorian year, filtered to that year - the shared "recurs every paksha/month, not
        /// once a year" search pattern behind <see cref="EkadashiCalendar"/> and the monthly Vrat
        /// calendars below (Purnima/Amavasya/Sankashti Chaturthi/Pradosh), extracted so those don't
        /// each duplicate the same ~15-synodic-month scan loop.
        /// </summary>
        private static List<Time> RecurringTithiCalendar(int year, GeoLocation location, int tithiNumber)
        {
            var dates = new List<Time>();
            var monthStart = PreviousNewMoon(new Time($"00:00 01/12/{year - 1} +00:00", location));

            for (var i = 0; i < 15; i++)
            {
                var occurrence = FindTithiInstant(monthStart, tithiNumber);
                if (occurrence.GetStdDateTimeOffset().Year == year) { dates.Add(occurrence); }

                monthStart = NextNewMoon(monthStart.AddHours(24));
            }

            return dates;
        }

        /// <summary>
        /// Gets every Ekadashi (tithi 11 of each Shukla paksha, tithi 26 of each Krishna paksha),
        /// with its classical name (see <see cref="EkadashiNamesByAmantaMonth"/>), in a Gregorian
        /// year - unlike <see cref="FestivalCalendar"/>'s once-per-year festivals, Ekadashi recurs
        /// roughly every 15 days, so this uses <see cref="RecurringTithiCalendar"/> rather than
        /// dispatching through <see cref="FestivalDate"/>. Returns ~24 occurrences in a normal
        /// year, more in a year containing an Adhika month.
        /// </summary>
        public static List<EkadashiOccurrence> EkadashiCalendar(int year, GeoLocation location)
        {
            var occurrences = new List<EkadashiOccurrence>();
            occurrences.AddRange(RecurringTithiCalendar(year, location, 11).Select(d => new EkadashiOccurrence(EkadashiName(d, true), d)));
            occurrences.AddRange(RecurringTithiCalendar(year, location, 26).Select(d => new EkadashiOccurrence(EkadashiName(d, false), d)));

            occurrences.Sort((a, b) => a.Date.GetStdDateTimeOffset().CompareTo(b.Date.GetStdDateTimeOffset()));
            return occurrences;
        }

        /// <summary>Gets every Purnima (full moon, tithi 15) Vrat date in a Gregorian year - one per synodic month, ~12/year.</summary>
        public static List<Time> PurnimaVratCalendar(int year, GeoLocation location)
        {
            var dates = RecurringTithiCalendar(year, location, 15);
            dates.Sort((a, b) => a.GetStdDateTimeOffset().CompareTo(b.GetStdDateTimeOffset()));
            return dates;
        }

        /// <summary>Gets every Amavasya (new moon, tithi 30) Vrat date in a Gregorian year - one per synodic month, ~12/year.</summary>
        public static List<Time> AmavasyaVratCalendar(int year, GeoLocation location)
        {
            var dates = RecurringTithiCalendar(year, location, 30);
            dates.Sort((a, b) => a.GetStdDateTimeOffset().CompareTo(b.GetStdDateTimeOffset()));
            return dates;
        }

        /// <summary>Gets every Sankashti Chaturthi (Krishna-paksha Chaturthi, tithi 19) date in a Gregorian year - one per synodic month, ~12/year.</summary>
        public static List<Time> SankashtiChaturthiCalendar(int year, GeoLocation location)
        {
            var dates = RecurringTithiCalendar(year, location, 19);
            dates.Sort((a, b) => a.GetStdDateTimeOffset().CompareTo(b.GetStdDateTimeOffset()));
            return dates;
        }

        /// <summary>Gets every Pradosh Vrat (Trayodashi, tithi 13 of each paksha) date in a Gregorian year - twice per synodic month, ~24/year, same shape as <see cref="EkadashiCalendar"/> but without a per-name lookup.</summary>
        public static List<Time> PradoshVratCalendar(int year, GeoLocation location)
        {
            var dates = new List<Time>();
            dates.AddRange(RecurringTithiCalendar(year, location, 13));
            dates.AddRange(RecurringTithiCalendar(year, location, 28));

            dates.Sort((a, b) => a.GetStdDateTimeOffset().CompareTo(b.GetStdDateTimeOffset()));
            return dates;
        }

        /// <summary>
        /// Gets the Pitru Paksha period (the 15-day span for ancestor remembrance) for a Gregorian
        /// year: Bhaadrapada Purnima (tithi 15) to the following Amavasya (tithi 30) - both within
        /// the same Amanta Bhaadrapada month, found the same way <see cref="ChaturmasPeriod"/>
        /// finds its own start/end tithis.
        /// </summary>
        public static TimeRange PitruPakshaPeriod(int year, GeoLocation location)
        {
            var start = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Bhaadrapada, 15, year, location);
            var end = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Bhaadrapada, 30, year, location);

            return new TimeRange(start, end);
        }

        /// <summary>
        /// Finds which sunrise within a search window around <paramref name="anchor"/> has the
        /// Moon in <paramref name="target"/> nakshatra - the shared search behind
        /// <see cref="JyeshthaGauriAvahana"/>/<see cref="JyeshthaGauriPoojan"/>/
        /// <see cref="JyeshthaGauriVisarjan"/>, which are each defined by nakshatra-at-sunrise, not
        /// by a fixed tithi offset (see their own doc comments).
        /// </summary>
        private static Time FindSunriseNakshatraDay(Time anchor, ConstellationName target)
        {
            for (var d = -4; d <= 10; d++)
            {
                var sunrise = SunriseTime(anchor.AddHours(24 * d));
                var nakshatra = ConstellationAtLongitude(PlanetNirayanaLongitude(Moon, sunrise)).GetConstellationName();
                if (nakshatra == target) { return sunrise; }
            }

            throw new Exception($"Could not find a {target} sunrise near {anchor} within a 14-day window");
        }

        /// <summary>
        /// Gets the Jyeshtha Gauri Avahana (invitation) date for a Gregorian year - the sunrise,
        /// within Bhaadrapada Shukla paksha, on which the Moon is in Anuradha nakshatra. Unlike
        /// every other festival in this file, the 3 Jyeshtha Gauri days aren't a fixed tithi -
        /// they're defined purely by which of 3 consecutive nakshatras (Anuradha/Jyeshtha/Moola)
        /// holds at sunrise, landing anywhere within roughly Bhaadrapada Shukla Panchami-Navami.
        /// Confirmed against a real published 2026 date (17 September, matching this method's
        /// output) - see <see cref="JyeshthaGauriPoojan"/>/<see cref="JyeshthaGauriVisarjan"/> for
        /// the other 2 days.
        /// </summary>
        public static Time JyeshthaGauriAvahana(int year, GeoLocation location)
        {
            var anchor = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Bhaadrapada, 5, year, location);
            return FindSunriseNakshatraDay(anchor, ConstellationName.Anuradha);
        }

        /// <summary>Gets the Jyeshtha Gauri Poojan (main worship) date - the sunrise Anuradha's day is followed by, when the Moon is in Jyeshtha nakshatra. See <see cref="JyeshthaGauriAvahana"/>.</summary>
        public static Time JyeshthaGauriPoojan(int year, GeoLocation location)
        {
            var anchor = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Bhaadrapada, 5, year, location);
            return FindSunriseNakshatraDay(anchor, ConstellationName.Jyesta);
        }

        /// <summary>Gets the Jyeshtha Gauri Visarjan (farewell) date - the day after Poojan, when the Moon is in Moola nakshatra. See <see cref="JyeshthaGauriAvahana"/>.</summary>
        public static Time JyeshthaGauriVisarjan(int year, GeoLocation location)
        {
            var anchor = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Bhaadrapada, 5, year, location);
            return FindSunriseNakshatraDay(anchor, ConstellationName.Moola);
        }

        /// <summary>
        /// Finds the moment the Sun's sidereal longitude reaches a given Rashi's start, searching
        /// from <paramref name="guess"/> - the same Newton-style search <see cref="MakarSankrantiDate"/>
        /// uses against a fixed Makara target, generalized to any of the 12 Rashis for
        /// <see cref="SankrantiCalendar"/>.
        /// </summary>
        private static Time SankrantiDateNear(ZodiacName rashi, Time guess)
        {
            var targetLongitude = (double)ZodiacSign.All12ZodiacNames.IndexOf(rashi) * 30.0;
            const double sunDegreesPerDay = 360.0 / 365.2422;
            var candidate = guess;

            for (var i = 0; i < 8; i++)
            {
                var currentLongitude = PlanetNirayanaLongitude(Sun, candidate).TotalDegrees;
                var diff = ((targetLongitude - currentLongitude + 540.0) % 360.0) - 180.0;

                if (Math.Abs(diff) < 0.0005) { break; }

                var adjustDays = diff / sunDegreesPerDay;
                candidate = candidate.AddHours(adjustDays * 24);
            }

            return candidate;
        }

        /// <summary>
        /// Gets all 12 Sankranti dates (the Sun's sidereal ingress into each Rashi) for a
        /// Gregorian year, keyed by Rashi. Reuses the already-validated <see cref="MakarSankrantiDate"/>
        /// as an anchor, seeding each other Rashi's search from that date offset by its distance
        /// (in Rashis) from Makara at ~30.44 days/Rashi - close enough for the Newton search in
        /// <see cref="SankrantiDateNear"/> to converge on the correct year's occurrence rather than
        /// an adjacent one.
        /// </summary>
        public static Dictionary<ZodiacName, Time> SankrantiCalendar(int year, GeoLocation location)
        {
            var makarSankranti = MakarSankrantiDate(year, location);
            var makarIndex = ZodiacSign.All12ZodiacNames.IndexOf(ZodiacName.Capricorn);
            const double avgDaysPerRashi = 365.2422 / 12.0;

            var result = new Dictionary<ZodiacName, Time>();
            foreach (var rashi in ZodiacSign.All12ZodiacNames)
            {
                //always step FORWARD from Makar (0..11 steps) - every other Rashi's occurrence in
                //this same Gregorian year falls after Makar Sankranti's own (mid-January) date,
                //all the way around to Dhanu (Sagittarius) in December, never before it
                var stepsFromMakar = ((ZodiacSign.All12ZodiacNames.IndexOf(rashi) - makarIndex) % 12 + 12) % 12;
                var guess = makarSankranti.AddHours(24 * avgDaysPerRashi * stepsFromMakar);
                result[rashi] = SankrantiDateNear(rashi, guess);
            }

            return result;
        }

        /// <summary>
        /// Finds the moment the Sun's sidereal longitude reaches Makara (Capricorn)'s start (270°)
        /// in a given Gregorian year - a purely solar event (Makar Sankranti), searched the same
        /// Newton-style way as <see cref="Calculate.TajikaDateForYear"/> but against a fixed target
        /// longitude instead of a natal one.
        /// </summary>
        private static Time MakarSankrantiDate(int year, GeoLocation location)
        {
            const double targetLongitude = 270.0; //start of Makara (Capricorn), sidereal
            const double sunDegreesPerDay = 360.0 / 365.2422;

            //coarse guess: Makar Sankranti reliably falls close to mid-January every year (Lahiri ayanamsa)
            var candidate = new Time($"00:00 10/01/{year} +00:00", location);

            for (var i = 0; i < 8; i++)
            {
                var currentLongitude = PlanetNirayanaLongitude(Sun, candidate).TotalDegrees;
                var diff = ((targetLongitude - currentLongitude + 540.0) % 360.0) - 180.0;

                if (Math.Abs(diff) < 0.0005) { break; }

                var adjustDays = diff / sunDegreesPerDay;
                candidate = candidate.AddHours(adjustDays * 24);
            }

            return candidate;
        }
    }
}
