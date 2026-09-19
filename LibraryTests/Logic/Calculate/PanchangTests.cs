using System;
using System.Collections.Generic;
using System.Linq;
using VedAstro.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests
{
    [TestClass()]
    public class PanchangTests
    {
        [TestMethod()]
        public void ChoghadiyaPeriods_ReturnsSixteenContiguousPeriods()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            var periods = Calculate.ChoghadiyaPeriods(time);

            Assert.AreEqual(16, periods.Count);
            Assert.IsTrue(periods.Take(8).All(p => p.IsDayPeriod), "First 8 periods should be day periods");
            Assert.IsTrue(periods.Skip(8).All(p => !p.IsDayPeriod), "Last 8 periods should be night periods");

            for (var i = 0; i < periods.Count; i++)
            {
                Assert.IsTrue(periods[i].End.GetStdDateTimeOffset() > periods[i].Start.GetStdDateTimeOffset(),
                    $"Period {i} ({periods[i].Name}) should end after it starts");

                if (i > 0)
                {
                    var gapMinutes = Math.Abs((periods[i].Start.GetStdDateTimeOffset() - periods[i - 1].End.GetStdDateTimeOffset()).TotalMinutes);
                    Assert.IsTrue(gapMinutes < 1, $"Expected period {i} to start where period {i - 1} ended, gap was {gapMinutes} min");
                }
            }
        }

        /// <summary>
        /// Checks every Vedic (sunrise-anchored) weekday's day-Choghadiya starting name against
        /// the classical table, scanning a full week so each weekday is hit at least once.
        /// </summary>
        [TestMethod()]
        public void ChoghadiyaPeriods_FirstDayPeriodName_MatchesClassicalWeekdayTable()
        {
            var expectedFirstNameByWeekday = new Dictionary<DayOfWeek, ChoghadiyaName>
            {
                [DayOfWeek.Sunday] = ChoghadiyaName.Udveg,
                [DayOfWeek.Monday] = ChoghadiyaName.Amrit,
                [DayOfWeek.Tuesday] = ChoghadiyaName.Rog,
                [DayOfWeek.Wednesday] = ChoghadiyaName.Labh,
                [DayOfWeek.Thursday] = ChoghadiyaName.Shubh,
                [DayOfWeek.Friday] = ChoghadiyaName.Chal,
                [DayOfWeek.Saturday] = ChoghadiyaName.Kaal,
            };

            var start = new Time("12:00 01/01/2024 +05:30", GeoLocation.Bangalore);
            var seenWeekdays = new HashSet<DayOfWeek>();

            for (var d = 0; d < 7; d++)
            {
                var day = start.AddHours(24 * d);
                var vedicWeekday = Calculate.DayOfWeek(day);
                var periods = Calculate.ChoghadiyaPeriods(day);

                Assert.AreEqual(expectedFirstNameByWeekday[vedicWeekday], periods[0].Name, $"Mismatch for {vedicWeekday}");
                seenWeekdays.Add(vedicWeekday);
            }

            Assert.AreEqual(7, seenWeekdays.Count, "Expected all 7 weekdays to be covered by this 7-day scan");
        }

        [TestMethod()]
        public void DailyPanchang_TithiAndVaraMatchTheirOwnStandaloneCalculators()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            //noon is after that day's own sunrise, so the day's own sunrise is the anchor instant
            var sunrise = Calculate.SunriseTime(time);

            var panchang = Calculate.DailyPanchang(time);

            Assert.AreEqual(Calculate.LunarDay(sunrise).GetLunarDateNumber(), panchang.Tithi.GetLunarDateNumber());
            Assert.AreEqual(Calculate.LunarMonth(sunrise), panchang.LunarMonth);
            Assert.AreEqual(Calculate.DayOfWeek(time), panchang.Vara);
            Assert.AreEqual(Calculate.Karana(sunrise), panchang.Karana);
            Assert.IsTrue(panchang.Sunrise.GetStdDateTimeOffset() < panchang.Sunset.GetStdDateTimeOffset());
        }

        /// <summary>
        /// A calendar day's Tithi/Nakshatra/Yoga/Karana must be fixed at that Vedic day's sunrise,
        /// not at whatever raw time-of-day happens to be passed in - see <see cref="Calculate.DailyPanchang"/>'s
        /// doc comment (compared against github.com/Vedic-Panchanga/sastro.mant's day-boundary
        /// convention). A time before that calendar date's sunrise belongs to the *previous* Vedic
        /// day, same rule as <see cref="Calculate.DayOfWeek"/>/<see cref="Calculate.HoraAtBirth"/>.
        /// </summary>
        [TestMethod()]
        public void DailyPanchang_TimeBeforeSunrise_AnchorsToPreviousDaysSunrise()
        {
            var location = GeoLocation.Bangalore;
            var sunriseOn15th = Calculate.SunriseTime(new Time("12:00 15/06/2024 +05:30", location));
            var sunriseOn14th = Calculate.SunriseTime(new Time("12:00 14/06/2024 +05:30", location));

            //well before sunrise - this instant belongs to the 14th's Vedic day, not the 15th's
            var earlyMorning = new Time("02:00 15/06/2024 +05:30", location);
            var panchangBeforeSunrise = Calculate.DailyPanchang(earlyMorning);

            Assert.AreEqual(sunriseOn14th.GetStdDateTimeOffset(), panchangBeforeSunrise.Sunrise.GetStdDateTimeOffset());
            Assert.AreEqual(Calculate.LunarDay(sunriseOn14th).GetLunarDateNumber(), panchangBeforeSunrise.Tithi.GetLunarDateNumber());

            //well after sunrise on the same calendar date - anchors to that date's own sunrise
            var afterSunrise = new Time("12:00 15/06/2024 +05:30", location);
            var panchangAfterSunrise = Calculate.DailyPanchang(afterSunrise);

            Assert.AreEqual(sunriseOn15th.GetStdDateTimeOffset(), panchangAfterSunrise.Sunrise.GetStdDateTimeOffset());
        }

        [TestMethod()]
        public void HoraPeriods_ReturnsTwentyFourContiguousPeriods()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            var periods = Calculate.HoraPeriods(time);

            Assert.AreEqual(24, periods.Count);
            Assert.IsTrue(periods.Take(12).All(p => p.IsDayPeriod), "First 12 periods should be day periods");
            Assert.IsTrue(periods.Skip(12).All(p => !p.IsDayPeriod), "Last 12 periods should be night periods");

            for (var i = 0; i < periods.Count; i++)
            {
                Assert.IsTrue(periods[i].End.GetStdDateTimeOffset() > periods[i].Start.GetStdDateTimeOffset(),
                    $"Period {i} ({periods[i].Lord}) should end after it starts");

                if (i > 0)
                {
                    var gapMinutes = Math.Abs((periods[i].Start.GetStdDateTimeOffset() - periods[i - 1].End.GetStdDateTimeOffset()).TotalMinutes);
                    Assert.IsTrue(gapMinutes < 1, $"Expected period {i} to start where period {i - 1} ended, gap was {gapMinutes} min");
                }
            }
        }

        /// <summary>
        /// First hora of the day must be that Vedic weekday's own ruling planet (Sun on Sunday,
        /// Moon on Monday, etc.) - the classical starting point of the 24-hora cycle.
        /// </summary>
        [TestMethod()]
        public void HoraPeriods_FirstDayPeriodLord_MatchesWeekdaysOwnRulingPlanet()
        {
            var expectedFirstLordByWeekday = new Dictionary<DayOfWeek, PlanetName>
            {
                [DayOfWeek.Sunday] = PlanetName.Sun,
                [DayOfWeek.Monday] = PlanetName.Moon,
                [DayOfWeek.Tuesday] = PlanetName.Mars,
                [DayOfWeek.Wednesday] = PlanetName.Mercury,
                [DayOfWeek.Thursday] = PlanetName.Jupiter,
                [DayOfWeek.Friday] = PlanetName.Venus,
                [DayOfWeek.Saturday] = PlanetName.Saturn,
            };

            var start = new Time("12:00 01/01/2024 +05:30", GeoLocation.Bangalore);
            var seenWeekdays = new HashSet<DayOfWeek>();

            for (var d = 0; d < 7; d++)
            {
                var day = start.AddHours(24 * d);
                var vedicWeekday = Calculate.DayOfWeek(day);
                var periods = Calculate.HoraPeriods(day);

                Assert.AreEqual(expectedFirstLordByWeekday[vedicWeekday], periods[0].Lord, $"Mismatch for {vedicWeekday}");
                seenWeekdays.Add(vedicWeekday);
            }

            Assert.AreEqual(7, seenWeekdays.Count, "Expected all 7 weekdays to be covered by this 7-day scan");
        }

        /// <summary>
        /// The hora sequence must not reset or skip at the sunset boundary - the 13th hora
        /// (first night hora) continues the same fixed cycle 12 steps on from the 1st.
        /// </summary>
        [TestMethod()]
        public void HoraPeriods_CycleIsUnbrokenAcrossSunsetBoundary()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);
            var periods = Calculate.HoraPeriods(time);

            var cycle = new[] { PlanetName.Sun, PlanetName.Venus, PlanetName.Mercury, PlanetName.Moon, PlanetName.Saturn, PlanetName.Jupiter, PlanetName.Mars };
            var startIndex = Array.IndexOf(cycle, periods[0].Lord);

            for (var i = 0; i < periods.Count; i++)
            {
                Assert.AreEqual(cycle[(startIndex + i) % 7], periods[i].Lord, $"Period {i} lord mismatch");
            }
        }

        [TestMethod()]
        [DataRow("RahuKaal")]
        [DataRow("GulikaKaal")]
        [DataRow("YamagandaKaal")]
        public void KaalPeriods_FallWithinDaytimeAndAreOneEighthOfDayLength(string methodName)
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);
            var sunrise = Calculate.SunriseTime(time);
            var sunset = Calculate.SunsetTime(time);
            var dayLengthHours = (sunset.GetStdDateTimeOffset() - sunrise.GetStdDateTimeOffset()).TotalHours;

            var method = typeof(Calculate).GetMethod(methodName, new[] { typeof(Time) });
            var range = (TimeRange)method!.Invoke(null, new object[] { time })!;

            Assert.IsTrue(range.start.GetStdDateTimeOffset() >= sunrise.GetStdDateTimeOffset());
            Assert.IsTrue(range.end.GetStdDateTimeOffset() <= sunset.GetStdDateTimeOffset());
            Assert.AreEqual(dayLengthHours / 8.0, (range.end.GetStdDateTimeOffset() - range.start.GetStdDateTimeOffset()).TotalHours, 0.01);
        }

        /// <summary>
        /// Rahu Kaal, Gulika Kaal and Yamaganda Kaal must never coincide - each weekday maps to a
        /// distinct one of the day's 8 segments across all three tables.
        /// </summary>
        [TestMethod()]
        public void KaalPeriods_NeverOverlapForAnyWeekday()
        {
            var start = new Time("12:00 01/01/2024 +05:30", GeoLocation.Bangalore);

            for (var d = 0; d < 7; d++)
            {
                var day = start.AddHours(24 * d);

                var rahu = Calculate.RahuKaal(day);
                var gulika = Calculate.GulikaKaal(day);
                var yamaganda = Calculate.YamagandaKaal(day);

                Assert.AreNotEqual(rahu.start.GetStdDateTimeOffset(), gulika.start.GetStdDateTimeOffset());
                Assert.AreNotEqual(rahu.start.GetStdDateTimeOffset(), yamaganda.start.GetStdDateTimeOffset());
                Assert.AreNotEqual(gulika.start.GetStdDateTimeOffset(), yamaganda.start.GetStdDateTimeOffset());
            }
        }

        private static readonly ConstellationName[] ExpectedGandMoolNakshatras =
        {
            ConstellationName.Aswini, ConstellationName.Aslesha, ConstellationName.Makha,
            ConstellationName.Jyesta, ConstellationName.Moola, ConstellationName.Revathi,
        };

        [TestMethod()]
        public void GandMoolPeriods_EveryReturnedPeriod_IsAMoonTransitOfAGandMoolNakshatra()
        {
            var time = new Time("00:00 01/06/2024 +05:30", GeoLocation.Bangalore);

            var periods = Calculate.GandMoolPeriods(time, 30);

            Assert.IsTrue(periods.Count > 0, "Expected at least one Gand Mool period in a 30-day scan");

            foreach (var period in periods)
            {
                Assert.IsTrue(period.end.GetStdDateTimeOffset() > period.start.GetStdDateTimeOffset());

                //sample the midpoint - it must fall in one of the 6 Gand Mool nakshatras
                var midpoint = period.start.AddHours(period.DaysBetween * 24 / 2.0);
                var nakshatra = Calculate.ConstellationAtLongitude(Calculate.PlanetNirayanaLongitude(PlanetName.Moon, midpoint)).GetConstellationName();
                CollectionAssert.Contains(ExpectedGandMoolNakshatras, nakshatra);

                //a single Gand Mool nakshatra transit is ~1 sidereal day (~24-27h), but 2 of the 6
                //(Aslesha+Makha, Jyeshtha+Moola) are back-to-back pairs and Revati+Aswini are
                //cyclically adjacent too, so a merged ~2-day (~48-54h) period is equally valid
                var hours = period.DaysBetween * 24;
                Assert.IsTrue(hours is > 18 and < 56, $"Expected ~1 or ~2 day transit, was {hours} hours");
            }
        }

        [TestMethod()]
        public void LagnaPeriods_AreContiguousAndSpanTheFullVedicDay()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);
            var sunrise = Calculate.SunriseTime(time);
            var nextSunrise = Calculate.SunriseTime(sunrise.AddHours(30));

            var periods = Calculate.LagnaPeriods(time);

            Assert.IsTrue(periods.Count is >= 8 and <= 14, $"Expected roughly 12 sign changes per day, got {periods.Count}");
            Assert.AreEqual(sunrise.GetStdDateTimeOffset(), periods[0].Start.GetStdDateTimeOffset());
            Assert.AreEqual(nextSunrise.GetStdDateTimeOffset(), periods[^1].End.GetStdDateTimeOffset());

            for (var i = 0; i < periods.Count; i++)
            {
                Assert.IsTrue(periods[i].End.GetStdDateTimeOffset() > periods[i].Start.GetStdDateTimeOffset());

                if (i > 0)
                {
                    var gapMinutes = Math.Abs((periods[i].Start.GetStdDateTimeOffset() - periods[i - 1].End.GetStdDateTimeOffset()).TotalMinutes);
                    Assert.IsTrue(gapMinutes < 1, $"Expected period {i} to start where period {i - 1} ended, gap was {gapMinutes} min");
                    Assert.AreNotEqual(periods[i - 1].Sign, periods[i].Sign, "Consecutive periods should never share a sign");
                }
            }
        }
    }
}
