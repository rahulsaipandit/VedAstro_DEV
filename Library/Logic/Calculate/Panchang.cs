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

        /// <summary>
        /// Finds the next instant at which <paramref name="keyAt"/>'s value changes from what it
        /// reads at <paramref name="time"/>, scanning forward hour by hour (safely finer than any
        /// Panchang limb's minimum duration - Karana, the shortest, never runs below ~10h) and
        /// bisecting the found hour-wide bracket to the exact boundary via <see cref="BisectBoundary"/>.
        /// Used by the 4 *Transition methods below instead of a per-limb closed-form formula,
        /// since it works uniformly regardless of each limb's underlying (non-constant-rate)
        /// angular motion.
        /// </summary>
        private static Time FindNextTransition<TKey>(Time time, Func<Time, TKey> keyAt) where TKey : struct
        {
            var comparer = EqualityComparer<TKey>.Default;
            var currentKey = keyAt(time);
            var previous = time;

            for (var hours = 1.0; hours <= 48.0; hours += 1.0)
            {
                var candidate = time.AddHours(hours);
                if (!comparer.Equals(keyAt(candidate), currentKey))
                {
                    return BisectBoundary(previous, candidate, t => !comparer.Equals(keyAt(t), currentKey), true);
                }
                previous = candidate;
            }

            throw new Exception($"Could not find a Panchang-limb transition for {time} within 48 hours");
        }

        /// <summary>
        /// Gets the Tithi in effect at <paramref name="time"/>, the instant it ends, and the Tithi
        /// that follows - the "current value + when it changes" row style used throughout the
        /// reference app's detailed Panchang table (as opposed to <see cref="DailyPanchang"/>'s
        /// single sunrise-anchored snapshot).
        /// </summary>
        public static TithiTransition TithiTransition(Time time)
        {
            var current = LunarDay(time);
            var endTime = FindNextTransition(time, t => LunarDay(t).GetLunarDateNumber());
            var next = LunarDay(endTime.AddHours(1.0 / 60));
            return new TithiTransition(current, endTime, next);
        }

        /// <summary>Gets the Nakshatra in effect at <paramref name="time"/>, the instant it ends, and the Nakshatra that follows.</summary>
        public static NakshatraTransition NakshatraTransition(Time time)
        {
            Constellation NakshatraAt(Time t) => ConstellationAtLongitude(PlanetNirayanaLongitude(Moon, t));

            var current = NakshatraAt(time);
            var endTime = FindNextTransition(time, t => NakshatraAt(t).GetConstellationName());
            var next = NakshatraAt(endTime.AddHours(1.0 / 60));
            return new NakshatraTransition(current, endTime, next);
        }

        /// <summary>Gets the Nithya Yoga in effect at <paramref name="time"/>, the instant it ends, and the Yoga that follows.</summary>
        public static YogaTransition YogaTransition(Time time)
        {
            var current = NithyaYoga(time);
            var endTime = FindNextTransition(time, t => NithyaYoga(t).Name);
            var next = NithyaYoga(endTime.AddHours(1.0 / 60));
            return new YogaTransition(current, endTime, next);
        }

        /// <summary>Gets the Karana in effect at <paramref name="time"/>, the instant it ends, and the Karana that follows.</summary>
        public static KaranaTransition KaranaTransition(Time time)
        {
            var current = Karana(time);
            var endTime = FindNextTransition(time, Karana);
            var next = Karana(endTime.AddHours(1.0 / 60));
            return new KaranaTransition(current, endTime, next);
        }

        /// <summary>
        /// Which of the day's (or, for Tuesday's second occurrence, the night's) 15 equal
        /// sunrise-to-sunset (or sunset-to-sunrise) divisions are Dur Muhurta - a fixed,
        /// classically inauspicious 1-2-per-day pair of ~48-minute windows, distinct from
        /// Rahu/Gulika/Yamaganda Kaal's single 1/8-daytime window. Weekday table cross-checked
        /// against 2 independently phrased sources (oursubhakaryam.com, bhaktibharat.org) stating
        /// the same "H hrs MM mins after sunrise/sunset" figures for every weekday; all 8 stated
        /// offsets resolve to exact multiples of 1/15 of a 12-hour reference day (48 minutes),
        /// which is what gives confidence this reduces cleanly to muhurta-index boundaries rather
        /// than being an imprecise or garbled secondary source. Not independently verified beyond
        /// that internal-consistency check - treat as best-effort pending a from-first-principles
        /// source, same caveat this codebase already applies to its other tithi/weekday tables.
        /// </summary>
        private static readonly Dictionary<DayOfWeek, (int StartIndex, int SpanCount, bool IsDayPeriod)[]> DurMuhurtaSlotsByWeekday = new()
        {
            [global::VedAstro.Library.DayOfWeek.Sunday] = new[] { (13, 1, true) },
            [global::VedAstro.Library.DayOfWeek.Monday] = new[] { (8, 1, true), (11, 1, true) },
            [global::VedAstro.Library.DayOfWeek.Tuesday] = new[] { (3, 1, true), (7, 1, false) },
            [global::VedAstro.Library.DayOfWeek.Wednesday] = new[] { (7, 1, true) },
            [global::VedAstro.Library.DayOfWeek.Thursday] = new[] { (5, 1, true), (11, 1, true) },
            [global::VedAstro.Library.DayOfWeek.Friday] = new[] { (3, 1, true), (11, 1, true) },
            [global::VedAstro.Library.DayOfWeek.Saturday] = new[] { (0, 2, true) },
        };

        /// <summary>Gets the Dur Muhurta window(s) for the calendar day containing <paramref name="date"/> - see <see cref="DurMuhurtaSlotsByWeekday"/>.</summary>
        public static List<TimeRange> DurMuhurtaPeriods(Time date)
        {
            var sunrise = SunriseTime(date);
            var sunset = SunsetTime(date);
            var nextSunrise = SunriseTime(sunset.AddHours(18));
            var weekday = DayOfWeek(sunrise);

            var dayMuhurtaHours = (sunset.GetStdDateTimeOffset() - sunrise.GetStdDateTimeOffset()).TotalHours / 15.0;
            var nightMuhurtaHours = (nextSunrise.GetStdDateTimeOffset() - sunset.GetStdDateTimeOffset()).TotalHours / 15.0;

            var periods = new List<TimeRange>();
            foreach (var (startIndex, spanCount, isDayPeriod) in DurMuhurtaSlotsByWeekday[weekday])
            {
                var anchor = isDayPeriod ? sunrise : sunset;
                var muhurtaHours = isDayPeriod ? dayMuhurtaHours : nightMuhurtaHours;
                periods.Add(new TimeRange(anchor.AddHours(muhurtaHours * startIndex), anchor.AddHours(muhurtaHours * (startIndex + spanCount))));
            }

            return periods;
        }

        /// <summary>
        /// Gets the Vikram Samvat year for the calendar day containing <paramref name="time"/> -
        /// this era's New Year is Chaitra Shukla Pratipada (tithi 1), found the same way
        /// <see cref="FestivalDate"/> finds any other named-month tithi. Standard conversion:
        /// Gregorian year + 57 from that New Year onward, + 56 before it (since Vikram Samvat
        /// began 57 years ahead of the Common Era and its New Year falls partway through the
        /// Gregorian year, typically March/April).
        /// </summary>
        public static int VikramSamvatYear(Time time)
        {
            var gregorianYear = time.GetStdDateTimeOffset().Year;
            var newYear = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Chaitra, 1, gregorianYear, time.GetGeoLocation());

            return time.GetStdDateTimeOffset() >= newYear.GetStdDateTimeOffset() ? gregorianYear + 57 : gregorianYear + 56;
        }

        /// <summary>
        /// Gets the Gujarati Samvat year - numerically the same series as Vikram Samvat, but
        /// rolling over at Kartika Shukla Pratipada (the day after Diwali) instead of Chaitra
        /// Shukla Pratipada, so between those two dates each year it trails the main Vikram Samvat
        /// number by 1 (e.g. Sept 2026: Vikram Samvat 2083, Gujarati Samvat still 2082 until this
        /// year's Kartika Shukla Pratipada).
        /// </summary>
        public static int GujaratiSamvatYear(Time time)
        {
            var gregorianYear = time.GetStdDateTimeOffset().Year;
            var newYear = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Kaarteeka, 1, gregorianYear, time.GetGeoLocation());

            return time.GetStdDateTimeOffset() >= newYear.GetStdDateTimeOffset() ? gregorianYear + 57 : gregorianYear + 56;
        }

        /// <summary>
        /// Gets the Saka Samvat year - same New Year (Chaitra Shukla Pratipada) as Vikram Samvat,
        /// but a 78-year-younger epoch: Gregorian year - 78 from that New Year onward, - 79 before it.
        /// </summary>
        public static int SakaSamvatYear(Time time)
        {
            var gregorianYear = time.GetStdDateTimeOffset().Year;
            var newYear = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Chaitra, 1, gregorianYear, time.GetGeoLocation());

            return time.GetStdDateTimeOffset() >= newYear.GetStdDateTimeOffset() ? gregorianYear - 78 : gregorianYear - 79;
        }

        /// <summary>
        /// Gets the Kali Samvat (Kali Yuga) year - same New Year as Vikram Samvat, epoch 3102 BCE:
        /// Gregorian year + 3101 from that New Year onward, + 3100 before it (no year zero between
        /// 3102 BCE and 1 CE).
        /// </summary>
        public static int KaliSamvatYear(Time time)
        {
            var gregorianYear = time.GetStdDateTimeOffset().Year;
            var newYear = FindTithiInNijaMonth(global::VedAstro.Library.LunarMonth.Chaitra, 1, gregorianYear, time.GetGeoLocation());

            return time.GetStdDateTimeOffset() >= newYear.GetStdDateTimeOffset() ? gregorianYear + 3101 : gregorianYear + 3100;
        }

        private static readonly string[] SamvatsaraNames =
        {
            "Prabhava", "Vibhava", "Sukla", "Pramoduta", "Prajapati", "Angirasa", "Srimukha", "Bhava", "Yuva", "Dhatri",
            "Iswara", "Bahudhanya", "Pramathi", "Vikrama", "Vrishaprajapati", "Chitrabhanu", "Svabhanu", "Tarana", "Parthiva", "Vyaya",
            "Sarvajit", "Sarvadhari", "Virodhi", "Vikriti", "Khara", "Nandana", "Vijaya", "Jaya", "Manmatha", "Durmukhi",
            "Hevilambi", "Vilambi", "Vikari", "Sarvari", "Plava", "Subhakrit", "Sobhakrit", "Krodhi", "Vishvavasu", "Parabhava",
            "Plavanga", "Kilaka", "Saumya", "Sadharana", "Virodhakrita", "Paridhavi", "Pramadi", "Ananda", "Rakshasa", "Nala",
            "Pingala", "Kalayukta", "Siddharthi", "Raudri", "Durmati", "Dundubhi", "Rudhirodgari", "Raktakshi", "Krodhana", "Akshaya",
        };

        /// <summary>
        /// Gets the 60-year-cycle Samvatsara name for the Vikram Samvat year containing
        /// <paramref name="time"/>. Cross-checked against a published Vikram-Samvat-to-Samvatsara
        /// table (Wikipedia's "Samvatsara" article): VS 2080-2085 map there to indices 37-42
        /// (Sobhakrit..Kilaka, 1-based), which the formula below reproduces exactly - 0-based
        /// index = ((VikramSamvat - 2044) mod 60) into <see cref="SamvatsaraNames"/> (Prabhava
        /// first). This is the mechanical/South Indian 60-year civil count, not the North Indian
        /// variant that periodically skips a name to resync with Jupiter's real position - see
        /// <see cref="SamvatsaraNameNorth"/>.
        /// </summary>
        public static string SamvatsaraName(Time time)
        {
            var vikramSamvat = VikramSamvatYear(time);
            var index = (((vikramSamvat - 2044) % 60) + 60) % 60;
            return SamvatsaraNames[index];
        }

        /// <summary>
        /// Gets the North Indian Samvatsara name, which runs a fixed 14 names ahead of the South
        /// Indian count (<see cref="SamvatsaraName"/>) due to accumulated historical Jupiter-resync
        /// skips. This fixed +14 offset is calibrated against a single confirmed real-world
        /// example (2026's VS 2083 -&gt; South "Parabhava" [index 40], North "Raudri" [index 54],
        /// a difference of exactly 14) rather than derived from tracking the actual historical
        /// skip years one by one, so treat it as best-effort pending independent verification
        /// against a dedicated North Indian Samvatsara table.
        /// </summary>
        public static string SamvatsaraNameNorth(Time time)
        {
            var southIndex = (((VikramSamvatYear(time) - 2044) % 60) + 60) % 60;
            return SamvatsaraNames[(southIndex + 14) % 60];
        }

        /// <summary>
        /// Gets the Purnimanta (full-moon-to-full-moon) name of the lunar month containing
        /// <paramref name="time"/> - this codebase's own <see cref="LunarMonth"/> calculator only
        /// implements Amanta (new-moon-to-new-moon) reckoning (see its doc comment), so this
        /// derives Purnimanta from it using the same shift <see cref="FestivalDate"/>'s doc
        /// comment already documents: the two conventions agree on the Shukla-paksha half of a
        /// month, but a Krishna-paksha tithi's Purnimanta month name is one month ahead of its
        /// Amanta name.
        /// </summary>
        public static LunarMonth PurnimantaMonth(Time time)
        {
            var amantaMonth = LunarMonth(time);
            var isKrishnaPaksha = LunarDay(time).GetLunarDateNumber() > 15;
            if (!isKrishnaPaksha) { return amantaMonth; }

            var monthNumber = (int)amantaMonth;
            var isAdhika = monthNumber > 12;
            var baseNumber = isAdhika ? monthNumber - 12 : monthNumber;
            var nextBaseNumber = baseNumber == 12 ? 1 : baseNumber + 1;

            return (LunarMonth)(isAdhika ? nextBaseNumber + 12 : nextBaseNumber);
        }

        /// <summary>
        /// Which of the 6 classical Ritu (season) a lunar month belongs to - 2 months per Ritu,
        /// starting Chaitra+Vaisaakha = Vasanta. Cross-checked against 3 independent sources
        /// (learnreligions.com, Wikipedia's per-season articles, bhaktibharat.com), all agreeing
        /// on this exact month pairing. Keyed by the Amanta month (Adhika months fall back to
        /// their base Nija month, same as <see cref="VikramSamvatYear"/>'s sibling calculators) -
        /// note this may read one Ritu earlier/later than an app that instead keys Ritu off the
        /// Sun's real sidereal Sankranti dates, since sidereal Rashi boundaries drift relative to
        /// the lunar month names other than via leap-month correction.
        /// </summary>
        private static readonly Dictionary<LunarMonth, RituName> RituByAmantaMonth = new()
        {
            [global::VedAstro.Library.LunarMonth.Chaitra] = RituName.Vasanta,
            [global::VedAstro.Library.LunarMonth.Vaisaakha] = RituName.Vasanta,
            [global::VedAstro.Library.LunarMonth.Jyeshtha] = RituName.Grishma,
            [global::VedAstro.Library.LunarMonth.Aashaadha] = RituName.Grishma,
            [global::VedAstro.Library.LunarMonth.Sraavana] = RituName.Varsha,
            [global::VedAstro.Library.LunarMonth.Bhaadrapada] = RituName.Varsha,
            [global::VedAstro.Library.LunarMonth.Aaswayuja] = RituName.Sharad,
            [global::VedAstro.Library.LunarMonth.Kaarteeka] = RituName.Sharad,
            [global::VedAstro.Library.LunarMonth.Maargasira] = RituName.Hemanta,
            [global::VedAstro.Library.LunarMonth.Pushya] = RituName.Hemanta,
            [global::VedAstro.Library.LunarMonth.Maagha] = RituName.Shishira,
            [global::VedAstro.Library.LunarMonth.Phaalguna] = RituName.Shishira,
        };

        /// <summary>Gets the Ritu (season) for the lunar month containing <paramref name="time"/> - see <see cref="RituByAmantaMonth"/>.</summary>
        public static RituName Ritu(Time time)
        {
            var month = LunarMonth(time);
            var nijaMonth = (int)month > 12 ? (LunarMonth)((int)month - 12) : month;
            return RituByAmantaMonth[nijaMonth];
        }

        /// <summary>
        /// Gets the Sun's Ayana (northward/southward half-year journey) at <paramref name="time"/>
        /// - a purely solar-longitude boundary, unambiguous regardless of lunar/Amanta/Purnimanta
        /// convention: Uttarayana runs Makara Sankranti (270°) to Karka Sankranti (90°), Dakshinayana
        /// the other half.
        /// </summary>
        public static AyanaName Ayana(Time time)
        {
            var sunLongitude = PlanetNirayanaLongitude(Sun, time).TotalDegrees;
            return sunLongitude is >= 90.0 and < 270.0 ? AyanaName.Dakshinayana : AyanaName.Uttarayana;
        }

        /// <summary>Gets the Moon's zodiac sign in effect at <paramref name="time"/>, the instant it changes, and the sign that follows.</summary>
        public static ZodiacTransition MoonZodiacTransition(Time time)
        {
            ZodiacName SignAt(Time t) => PlanetZodiacSign(Moon, t).GetSignName();

            var current = SignAt(time);
            var endTime = FindNextTransition(time, SignAt);
            var next = SignAt(endTime.AddHours(1.0 / 60));
            return new ZodiacTransition(current, endTime, next);
        }
    }
}
