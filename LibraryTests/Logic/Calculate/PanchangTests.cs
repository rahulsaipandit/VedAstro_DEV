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

            var panchang = Calculate.DailyPanchang(time);

            Assert.AreEqual(Calculate.LunarDay(time).GetLunarDateNumber(), panchang.Tithi.GetLunarDateNumber());
            Assert.AreEqual(Calculate.LunarMonth(time), panchang.LunarMonth);
            Assert.AreEqual(Calculate.DayOfWeek(time), panchang.Vara);
            Assert.AreEqual(Calculate.Karana(time), panchang.Karana);
            Assert.IsTrue(panchang.Sunrise.GetStdDateTimeOffset() < panchang.Sunset.GetStdDateTimeOffset());
        }
    }
}
