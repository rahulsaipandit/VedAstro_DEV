using System;
using VedAstro.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests
{
    [TestClass()]
    public class FestivalCalendarTests
    {
        /// <summary>
        /// Every tithi/month-based festival must land in the requested year, on the exact tithi and
        /// Nija (non-Adhika) month its rule specifies - the internal-consistency check that matters
        /// most, since the exact civil date (which follows sunrise/sunset tithi conventions this
        /// method doesn't model) can differ by up to a day from the raw astronomical tithi instant.
        /// </summary>
        /// <summary>
        /// Diwali's Amavasya falls in Amanta Aaswayuja (Ashwin), not Amanta Kaarteeka - it's
        /// popularly known as "Kartik Amavasya" only under the Purnimanta convention, which is
        /// exactly one month name ahead of Amanta on the Krishna-paksha half of a month (see
        /// FestivalDate's doc comment).
        /// </summary>
        [TestMethod()]
        public void FestivalDate_Diwali2023_MatchesAmavasyaInAaswayuja()
        {
            var result = Calculate.FestivalDate(FestivalName.Diwali, 2023, GeoLocation.Bangalore);

            Assert.AreEqual(2023, result.GetStdDateTimeOffset().Year);
            Assert.AreEqual(30, Calculate.LunarDay(result).GetLunarDateNumber());
            Assert.AreEqual(LunarMonth.Aaswayuja, Calculate.LunarMonth(result));

            // sanity check against the well-known civil date (12/11/2023) - loose tolerance since
            // the civil date follows a sunset-tithi convention this method doesn't model
            var daysFromKnownDate = Math.Abs((result.GetStdDateTimeOffset() - new DateTimeOffset(2023, 11, 12, 0, 0, 0, TimeSpan.FromHours(5.5))).TotalDays);
            Assert.IsTrue(daysFromKnownDate < 2, $"Expected close to 12/11/2023, was {result.GetStdDateTimeOffset()}");
        }

        [TestMethod()]
        public void FestivalDate_Ramnavami2024_MatchesNavamiInChaitra()
        {
            var result = Calculate.FestivalDate(FestivalName.Ramnavami, 2024, GeoLocation.Bangalore);

            Assert.AreEqual(2024, result.GetStdDateTimeOffset().Year);
            Assert.AreEqual(9, Calculate.LunarDay(result).GetLunarDateNumber());
            Assert.AreEqual(LunarMonth.Chaitra, Calculate.LunarMonth(result));

            // sanity check against the well-known civil date (17/04/2024)
            var daysFromKnownDate = Math.Abs((result.GetStdDateTimeOffset() - new DateTimeOffset(2024, 4, 17, 0, 0, 0, TimeSpan.FromHours(5.5))).TotalDays);
            Assert.IsTrue(daysFromKnownDate < 2, $"Expected close to 17/04/2024, was {result.GetStdDateTimeOffset()}");
        }

        [TestMethod()]
        public void FestivalDate_MakarSankranti_IsAPurelySolarEventNearMidJanuary()
        {
            var result2024 = Calculate.FestivalDate(FestivalName.MakarSankranti, 2024, GeoLocation.Bangalore);
            var result2025 = Calculate.FestivalDate(FestivalName.MakarSankranti, 2025, GeoLocation.Bangalore);

            Assert.AreEqual(1, result2024.GetStdDateTimeOffset().Month);
            Assert.AreEqual(1, result2025.GetStdDateTimeOffset().Month);

            // unlike every other festival, this is solar - the day-of-month barely moves year to year
            var dayDiff = Math.Abs(result2024.GetStdDateTimeOffset().Day - result2025.GetStdDateTimeOffset().Day);
            Assert.IsTrue(dayDiff <= 1, $"Expected Sankranti day to barely shift year to year, was {result2024.GetStdDateTimeOffset().Day} vs {result2025.GetStdDateTimeOffset().Day}");
        }

        /// <summary>
        /// FestivalCalendar must return exactly one entry per FestivalName, each landing in the
        /// requested year.
        /// </summary>
        [TestMethod()]
        public void FestivalCalendar_2024_ReturnsAllFestivalsInRequestedYear()
        {
            var calendar = Calculate.FestivalCalendar(2024, GeoLocation.Bangalore);

            Assert.AreEqual(Enum.GetValues(typeof(FestivalName)).Length, calendar.Count);
            foreach (var entry in calendar)
            {
                Assert.AreEqual(2024, entry.Value.GetStdDateTimeOffset().Year, $"{entry.Key} did not land in 2024");
            }
        }
    }
}
