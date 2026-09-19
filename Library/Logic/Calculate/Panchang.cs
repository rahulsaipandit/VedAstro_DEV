using System;
using System.Collections.Generic;
using static VedAstro.Library.PlanetName;

namespace VedAstro.Library
{
    /// <summary>
    /// The daily Panchang: two alternative ways to present a day's auspicious-timing info, so a
    /// caller (the WebsiteNative calendar day-detail view) can offer both and let the user choose.
    /// <see cref="ChoghadiyaPeriods"/> divides the day/night into 8+8 named, qualified periods
    /// (conceptually the same classical algorithm as
    /// github.com/vishalnagda1/choghadiya - a reference JS implementation not diffed line-by-line
    /// here since GitHub was unreachable in this environment while this was built; the weekday
    /// tables and 8-part sunrise/sunset division are standard, uncontested Panchang material, not
    /// any one repo's original derivation). <see cref="DailyPanchang"/> instead gives the 5 core
    /// Panchang limbs (Tithi/Vara/Nakshatra/Yoga/Karana) as a single reference snapshot for the
    /// day - the same elements
    /// github.com/Vedic-Panchanga/Shri-Jagannath-Panchang's engine computes (see its comparison in
    /// the "Adhika-Masa Detection &amp; Festival Calendar Generator" section above), but derived
    /// here from VedAstro's own Swiss-Ephemeris-backed longitudes rather than a ported
    /// manda/sighra epicycle series.
    ///
    /// Also compared against github.com/Vedic-Panchanga/sastro.mant (a WASM Swiss-Ephemeris
    /// front-end, see its src/chart-components/Chart.tsx and src/vedic-components/Vedic.tsx). Its
    /// tithi formula - <c>ceil(((moon.lon - sun.lon + 360) % 360) / 12)</c> on sidereal longitudes
    /// - is identical to <see cref="LunarDay"/>. Two real differences found: (1) it defaults to
    /// True Chitrapaksha ayanamsa (sidMode 27) rather than Lahiri - VedAstro always uses Lahiri, see
    /// <see cref="CoreTime.Ayanamsa"/>; (2) its month-start rule is "next day after new moon *or*
    /// full moon" - i.e. it supports both Amanta and Purnimanta reckoning - while
    /// <see cref="LunarMonth"/> only implements Amanta (new-moon-to-new-moon; see its own doc
    /// comment). Neither is a bug, just a default/scope mismatch to account for when diffing
    /// output against that repo.
    /// </summary>
    public partial class Calculate
    {
        /// <summary>
        /// Gets the 5 core Panchang limbs (Tithi, Vara, Nakshatra, Yoga, Karana) plus Sunrise/Sunset
        /// for the calendar day containing <paramref name="time"/> - <see cref="NithyaYoga"/> and
        /// <see cref="Karana"/> are pre-existing calculators (`Core.cs`), reused as-is here. See
        /// <see cref="DailyPanchang"/> for what is and isn't included.
        ///
        /// Tithi/LunarMonth/Nakshatra/Yoga/Karana are all evaluated at that Vedic day's *sunrise*,
        /// not at the raw <paramref name="time"/> passed in - the classical convention (and the one
        /// github.com/Vedic-Panchanga/sastro.mant documents: "solar day's name is following lunar
        /// day at sunrise") is that a calendar day's Panchang is fixed at sunrise and holds for the
        /// whole day, so a caller passing e.g. midnight or noon must still get the same limbs as one
        /// passing sunrise itself. Uses the same before-sunrise-means-previous-day rule as
        /// <see cref="DayOfWeek"/>/<see cref="HoraAtBirth"/>.
        /// </summary>
        public static DailyPanchang DailyPanchang(Time time)
        {
            var sunriseSameDate = SunriseTime(time);
            var effectiveSunrise = time.GetLmtDateTimeOffset() < sunriseSameDate.GetLmtDateTimeOffset()
                ? SunriseTime(new Time(time.GetLmtDateTimeOffset().DateTime.AddDays(-1), time.GetStdDateTimeOffset().Offset, time.GetGeoLocation()))
                : sunriseSameDate;

            var nakshatra = ConstellationAtLongitude(PlanetNirayanaLongitude(Moon, effectiveSunrise));

            return new DailyPanchang(
                LunarDay(effectiveSunrise),
                LunarMonth(effectiveSunrise),
                DayOfWeek(time),
                nakshatra,
                NithyaYoga(effectiveSunrise),
                Karana(effectiveSunrise),
                effectiveSunrise,
                SunsetTime(effectiveSunrise));
        }

        private static readonly ChoghadiyaName[] ChoghadiyaCycle =
        {
            ChoghadiyaName.Udveg, ChoghadiyaName.Chal, ChoghadiyaName.Labh, ChoghadiyaName.Amrit,
            ChoghadiyaName.Kaal, ChoghadiyaName.Shubh, ChoghadiyaName.Rog,
        };

        private static readonly Dictionary<DayOfWeek, ChoghadiyaName> ChoghadiyaDayStartByWeekday = new()
        {
            [global::VedAstro.Library.DayOfWeek.Sunday] = ChoghadiyaName.Udveg,
            [global::VedAstro.Library.DayOfWeek.Monday] = ChoghadiyaName.Amrit,
            [global::VedAstro.Library.DayOfWeek.Tuesday] = ChoghadiyaName.Rog,
            [global::VedAstro.Library.DayOfWeek.Wednesday] = ChoghadiyaName.Labh,
            [global::VedAstro.Library.DayOfWeek.Thursday] = ChoghadiyaName.Shubh,
            [global::VedAstro.Library.DayOfWeek.Friday] = ChoghadiyaName.Chal,
            [global::VedAstro.Library.DayOfWeek.Saturday] = ChoghadiyaName.Kaal,
        };

        private static readonly Dictionary<DayOfWeek, ChoghadiyaName> ChoghadiyaNightStartByWeekday = new()
        {
            [global::VedAstro.Library.DayOfWeek.Sunday] = ChoghadiyaName.Shubh,
            [global::VedAstro.Library.DayOfWeek.Monday] = ChoghadiyaName.Chal,
            [global::VedAstro.Library.DayOfWeek.Tuesday] = ChoghadiyaName.Kaal,
            [global::VedAstro.Library.DayOfWeek.Wednesday] = ChoghadiyaName.Udveg,
            [global::VedAstro.Library.DayOfWeek.Thursday] = ChoghadiyaName.Amrit,
            [global::VedAstro.Library.DayOfWeek.Friday] = ChoghadiyaName.Rog,
            [global::VedAstro.Library.DayOfWeek.Saturday] = ChoghadiyaName.Labh,
        };

        private static readonly Dictionary<ChoghadiyaName, ChoghadiyaQuality> ChoghadiyaQualityByName = new()
        {
            [ChoghadiyaName.Amrit] = ChoghadiyaQuality.Auspicious,
            [ChoghadiyaName.Shubh] = ChoghadiyaQuality.Auspicious,
            [ChoghadiyaName.Labh] = ChoghadiyaQuality.Auspicious,
            [ChoghadiyaName.Chal] = ChoghadiyaQuality.Neutral,
            [ChoghadiyaName.Udveg] = ChoghadiyaQuality.Inauspicious,
            [ChoghadiyaName.Kaal] = ChoghadiyaQuality.Inauspicious,
            [ChoghadiyaName.Rog] = ChoghadiyaQuality.Inauspicious,
        };

        /// <summary>
        /// Gets all 16 Choghadiya periods (8 day + 8 night) for the calendar day containing
        /// <paramref name="date"/>. The day half runs sunrise-to-sunset divided into 8 equal parts,
        /// the night half sunset-to-next-sunrise divided into 8 equal parts; each part's name
        /// cycles through the fixed 7-name <see cref="ChoghadiyaName"/> sequence, starting from
        /// whichever name the day-of-week's classical table assigns to the first day/night period.
        /// </summary>
        public static List<ChoghadiyaPeriod> ChoghadiyaPeriods(Time date)
        {
            var sunrise = SunriseTime(date);
            var sunset = SunsetTime(date);
            var nextSunrise = SunriseTime(sunset.AddHours(18)); //well past sunset, safely into the next day

            var weekday = DayOfWeek(sunrise); //Vedic (sunrise-anchored) weekday, not the civil-midnight one
            var periods = new List<ChoghadiyaPeriod>();

            var dayPartHours = (sunset.GetStdDateTimeOffset() - sunrise.GetStdDateTimeOffset()).TotalHours / 8.0;
            var dayStartIndex = Array.IndexOf(ChoghadiyaCycle, ChoghadiyaDayStartByWeekday[weekday]);
            for (var i = 0; i < 8; i++)
            {
                var name = ChoghadiyaCycle[(dayStartIndex + i) % 7];
                var start = sunrise.AddHours(dayPartHours * i);
                var end = sunrise.AddHours(dayPartHours * (i + 1));
                periods.Add(new ChoghadiyaPeriod(name, ChoghadiyaQualityByName[name], start, end, true));
            }

            var nightPartHours = (nextSunrise.GetStdDateTimeOffset() - sunset.GetStdDateTimeOffset()).TotalHours / 8.0;
            var nightStartIndex = Array.IndexOf(ChoghadiyaCycle, ChoghadiyaNightStartByWeekday[weekday]);
            for (var i = 0; i < 8; i++)
            {
                var name = ChoghadiyaCycle[(nightStartIndex + i) % 7];
                var start = sunset.AddHours(nightPartHours * i);
                var end = sunset.AddHours(nightPartHours * (i + 1));
                periods.Add(new ChoghadiyaPeriod(name, ChoghadiyaQualityByName[name], start, end, false));
            }

            return periods;
        }

        /// <summary>
        /// The fixed cyclic order Hora lords advance through, one step per hora - this is the
        /// classical "Chaldean order" (slowest to fastest planet), not the weekday order. Which
        /// planet starts the cycle each day is that day's own weekday lord (<see cref="HoraLordByWeekday"/>);
        /// see <see cref="HoraPeriods"/>.
        /// </summary>
        private static readonly PlanetName[] HoraCycle = { Sun, Venus, Mercury, Moon, Saturn, Jupiter, Mars };

        private static readonly Dictionary<DayOfWeek, PlanetName> HoraLordByWeekday = new()
        {
            [global::VedAstro.Library.DayOfWeek.Sunday] = Sun,
            [global::VedAstro.Library.DayOfWeek.Monday] = Moon,
            [global::VedAstro.Library.DayOfWeek.Tuesday] = Mars,
            [global::VedAstro.Library.DayOfWeek.Wednesday] = Mercury,
            [global::VedAstro.Library.DayOfWeek.Thursday] = Jupiter,
            [global::VedAstro.Library.DayOfWeek.Friday] = Venus,
            [global::VedAstro.Library.DayOfWeek.Saturday] = Saturn,
        };

        /// <summary>
        /// Gets all 24 Hora periods (12 day + 12 night) for the calendar day containing
        /// <paramref name="date"/>. Same sunrise/sunset-halved structure as
        /// <see cref="ChoghadiyaPeriods"/>, but each half is split into 12 equal parts (not 8),
        /// and the lord sequence is the fixed <see cref="HoraCycle"/> rather than a per-weekday
        /// lookup table - only the *starting* lord of the day (that weekday's own ruling planet)
        /// depends on weekday, every hora after it just advances through the fixed cycle,
        /// uninterrupted across the sunset boundary.
        /// </summary>
        public static List<HoraPeriod> HoraPeriods(Time date)
        {
            var sunrise = SunriseTime(date);
            var sunset = SunsetTime(date);
            var nextSunrise = SunriseTime(sunset.AddHours(18)); //well past sunset, safely into the next day

            var weekday = DayOfWeek(sunrise); //Vedic (sunrise-anchored) weekday
            var startIndex = Array.IndexOf(HoraCycle, HoraLordByWeekday[weekday]);
            var periods = new List<HoraPeriod>();

            var dayPartHours = (sunset.GetStdDateTimeOffset() - sunrise.GetStdDateTimeOffset()).TotalHours / 12.0;
            for (var i = 0; i < 12; i++)
            {
                var lord = HoraCycle[(startIndex + i) % 7];
                var start = sunrise.AddHours(dayPartHours * i);
                var end = sunrise.AddHours(dayPartHours * (i + 1));
                periods.Add(new HoraPeriod(lord, start, end, true));
            }

            var nightPartHours = (nextSunrise.GetStdDateTimeOffset() - sunset.GetStdDateTimeOffset()).TotalHours / 12.0;
            for (var i = 0; i < 12; i++)
            {
                //night horas continue the same unbroken cycle, 12 horas further on from the day's start
                var lord = HoraCycle[(startIndex + 12 + i) % 7];
                var start = sunset.AddHours(nightPartHours * i);
                var end = sunset.AddHours(nightPartHours * (i + 1));
                periods.Add(new HoraPeriod(lord, start, end, false));
            }

            return periods;
        }

        /// <summary>
        /// Which of the day's 8 equal sunrise-to-sunset segments (0-indexed, same division as
        /// <see cref="ChoghadiyaPeriods"/>'s day half) is Rahu Kaal, per the classical weekday
        /// table (cross-checked against github.com/vishalnagda1/choghadiya and drikpanchang.com's
        /// published table - both agree on this mapping).
        /// </summary>
        private static readonly Dictionary<DayOfWeek, int> RahuKaalSegmentByWeekday = new()
        {
            [global::VedAstro.Library.DayOfWeek.Sunday] = 7,
            [global::VedAstro.Library.DayOfWeek.Monday] = 1,
            [global::VedAstro.Library.DayOfWeek.Tuesday] = 6,
            [global::VedAstro.Library.DayOfWeek.Wednesday] = 4,
            [global::VedAstro.Library.DayOfWeek.Thursday] = 5,
            [global::VedAstro.Library.DayOfWeek.Friday] = 3,
            [global::VedAstro.Library.DayOfWeek.Saturday] = 2,
        };

        /// <summary>Which day-segment (0-indexed) is Gulika Kaal, per the classical weekday table.</summary>
        private static readonly Dictionary<DayOfWeek, int> GulikaKaalSegmentByWeekday = new()
        {
            [global::VedAstro.Library.DayOfWeek.Sunday] = 6,
            [global::VedAstro.Library.DayOfWeek.Monday] = 5,
            [global::VedAstro.Library.DayOfWeek.Tuesday] = 4,
            [global::VedAstro.Library.DayOfWeek.Wednesday] = 3,
            [global::VedAstro.Library.DayOfWeek.Thursday] = 2,
            [global::VedAstro.Library.DayOfWeek.Friday] = 1,
            [global::VedAstro.Library.DayOfWeek.Saturday] = 0,
        };

        /// <summary>Which day-segment (0-indexed) is Yamaganda Kaal, per the classical weekday table.</summary>
        private static readonly Dictionary<DayOfWeek, int> YamagandaKaalSegmentByWeekday = new()
        {
            [global::VedAstro.Library.DayOfWeek.Sunday] = 4,
            [global::VedAstro.Library.DayOfWeek.Monday] = 3,
            [global::VedAstro.Library.DayOfWeek.Tuesday] = 2,
            [global::VedAstro.Library.DayOfWeek.Wednesday] = 1,
            [global::VedAstro.Library.DayOfWeek.Thursday] = 0,
            [global::VedAstro.Library.DayOfWeek.Friday] = 6,
            [global::VedAstro.Library.DayOfWeek.Saturday] = 5,
        };

        /// <summary>
        /// Shared implementation for Rahu/Gulika/Yamaganda Kaal - each is exactly one of the
        /// day's 8 equal sunrise-to-sunset segments (same division <see cref="ChoghadiyaPeriods"/>
        /// uses for its day half), the segment picked by a fixed per-weekday table. Unlike
        /// Choghadiya's 7-quality-level periods, these are single binary "avoid this window"
        /// spans with no separate quality concept.
        /// </summary>
        private static TimeRange KaalPeriod(Time date, Dictionary<DayOfWeek, int> segmentByWeekday)
        {
            var sunrise = SunriseTime(date);
            var sunset = SunsetTime(date);
            var weekday = DayOfWeek(sunrise); //Vedic (sunrise-anchored) weekday

            var segmentHours = (sunset.GetStdDateTimeOffset() - sunrise.GetStdDateTimeOffset()).TotalHours / 8.0;
            var segment = segmentByWeekday[weekday];

            return new TimeRange(sunrise.AddHours(segmentHours * segment), sunrise.AddHours(segmentHours * (segment + 1)));
        }

        /// <summary>Gets Rahu Kaal - the daily inauspicious window ruled by Rahu - for the calendar day containing <paramref name="date"/>.</summary>
        public static TimeRange RahuKaal(Time date) => KaalPeriod(date, RahuKaalSegmentByWeekday);

        /// <summary>Gets Gulika Kaal - the daily inauspicious window ruled by Gulika (Saturn's upagraha) - for the calendar day containing <paramref name="date"/>.</summary>
        public static TimeRange GulikaKaal(Time date) => KaalPeriod(date, GulikaKaalSegmentByWeekday);

        /// <summary>Gets Yamaganda Kaal - the daily inauspicious window ruled by Yama - for the calendar day containing <paramref name="date"/>.</summary>
        public static TimeRange YamagandaKaal(Time date) => KaalPeriod(date, YamagandaKaalSegmentByWeekday);

        /// <summary>
        /// Narrows a (before, after) instant pair - where <paramref name="predicate"/> is known to
        /// read <c>!targetValue</c> at <paramref name="before"/> and <paramref name="targetValue"/>
        /// at <paramref name="after"/> - down to the boundary instant between them via bisection.
        /// 15 halvings of even a multi-hour starting gap converge to sub-second precision, which is
        /// what both <see cref="GandMoolPeriods"/> (Moon/nakshatra boundaries) and
        /// <see cref="LagnaPeriods"/> (ascendant/sign boundaries) need - both track a longitude that
        /// moves at a non-constant rate, so bisection on the boolean state is simpler and more
        /// robust here than a Newton search on a target degree (contrast
        /// <see cref="FindTithiInstant"/>, which can assume a near-constant synodic rate).
        /// </summary>
        private static Time BisectBoundary(Time before, Time after, Func<Time, bool> predicate, bool targetValue)
        {
            for (var i = 0; i < 15; i++)
            {
                var midHours = (after.GetStdDateTimeOffset() - before.GetStdDateTimeOffset()).TotalHours / 2.0;
                var mid = before.AddHours(midHours);

                if (predicate(mid) == targetValue) { after = mid; }
                else { before = mid; }
            }

            return after;
        }

        private static readonly ConstellationName[] GandMoolNakshatras =
        {
            ConstellationName.Aswini, ConstellationName.Aslesha, ConstellationName.Makha,
            ConstellationName.Jyesta, ConstellationName.Moola, ConstellationName.Revathi,
        };

        /// <summary>
        /// Finds every window in the next <paramref name="daysAhead"/> days (starting at
        /// <paramref name="date"/>) during which the Moon transits one of the 6 Gand Mool
        /// nakshatras (Aswini, Aslesha, Makha, Jyeshtha, Moola, Revati). Scans in 6-hour steps
        /// (comfortably finer than the Moon's ~13°/day motion crossing one ~13°20' nakshatra
        /// per ~1 day) and bisects each transition to the exact boundary - see
        /// <see cref="BisectBoundary"/>. If the window starts or ends mid-transit, that partial
        /// period's open edge is clamped to <paramref name="date"/>/the window end rather than
        /// searched for outside the requested range.
        /// </summary>
        public static List<TimeRange> GandMoolPeriods(Time date, int daysAhead)
        {
            bool IsGandMool(Time t) => Array.IndexOf(GandMoolNakshatras, ConstellationAtLongitude(PlanetNirayanaLongitude(Moon, t)).GetConstellationName()) >= 0;

            var periods = new List<TimeRange>();
            const double stepHours = 6.0;
            var totalHours = daysAhead * 24.0;

            var previous = date;
            var previousIsGandMool = IsGandMool(previous);
            Time? periodStart = previousIsGandMool ? previous : null;

            for (var elapsed = stepHours; elapsed <= totalHours; elapsed += stepHours)
            {
                var current = date.AddHours(elapsed);
                var currentIsGandMool = IsGandMool(current);

                if (currentIsGandMool && !previousIsGandMool)
                {
                    periodStart = BisectBoundary(previous, current, IsGandMool, true);
                }
                else if (!currentIsGandMool && previousIsGandMool && periodStart.HasValue)
                {
                    periods.Add(new TimeRange(periodStart.Value, BisectBoundary(previous, current, IsGandMool, false)));
                    periodStart = null;
                }

                previous = current;
                previousIsGandMool = currentIsGandMool;
            }

            //still mid-transit when the requested window ran out - close it off at the window's end
            if (periodStart.HasValue) { periods.Add(new TimeRange(periodStart.Value, previous)); }

            return periods;
        }

        /// <summary>
        /// Gets the ascendant (Lagna) sign's change-times across the Vedic day (sunrise to next
        /// sunrise) containing <paramref name="date"/> - typically ~10-12 sign changes per day,
        /// since the ascendant advances roughly one sign every ~2 hours (varying with latitude and
        /// ecliptic obliquity, hence bisection rather than an assumed constant rate - see
        /// <see cref="BisectBoundary"/>).
        /// </summary>
        public static List<LagnaPeriod> LagnaPeriods(Time date)
        {
            ZodiacName SignAt(Time t) => ZodiacSignAtLongitude(HouseLongitude(HouseName.House1, t).GetMiddleLongitude()).GetSignName();

            var sunrise = SunriseTime(date);
            var nextSunrise = SunriseTime(sunrise.AddHours(30)); //well past sunrise, safely into the next Vedic day

            var periods = new List<LagnaPeriod>();
            const double stepHours = 0.5; //comfortably finer than the fastest a sign can pass (~1.7h at the equator)
            var totalHours = (nextSunrise.GetStdDateTimeOffset() - sunrise.GetStdDateTimeOffset()).TotalHours;

            var periodStart = sunrise;
            var currentSign = SignAt(sunrise);
            var previous = sunrise;

            for (var elapsed = stepHours; elapsed <= totalHours; elapsed += stepHours)
            {
                var current = sunrise.AddHours(elapsed);
                var sampledSign = SignAt(current);

                if (sampledSign != currentSign)
                {
                    var boundary = BisectBoundary(previous, current, t => SignAt(t) != currentSign, true);
                    periods.Add(new LagnaPeriod(currentSign, periodStart, boundary));
                    periodStart = boundary;
                    currentSign = sampledSign;
                }

                previous = current;
            }

            periods.Add(new LagnaPeriod(currentSign, periodStart, nextSunrise));

            return periods;
        }
    }
}
