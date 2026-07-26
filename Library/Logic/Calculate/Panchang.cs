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
    }
}
