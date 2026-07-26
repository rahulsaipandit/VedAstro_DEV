using System;
using VedAstro.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VedAstro.Library.Tests
{
    [TestClass()]
    public class CoreMiscTests
    {
        /// <summary>
        /// Regression test for a bug reported live against the deployed Blazor GoodTimeFinder page:
        /// for a person born 01/01/1980, clicking "+1 Year" today was expected to show
        /// today..+1 year (a forward-looking window), but showed 01/01/1980..01/01/1981 instead
        /// (the person's first year of life) because this method anchored "Nyear" presets on
        /// birth. Start must be "now" (within the test's execution window), end must be start + N.
        /// </summary>
        [TestMethod()]
        public void AutoCalculateTimeRange_SingularYearPreset_AddsYearsFromNow()
        {
            var birthTime = CalculateTests.StandardHoroscope; // 14:20 16/10/1918 +05:30

            var before = DateTimeOffset.UtcNow;
            var (start, end) = Calculate.AutoCalculateTimeRange(birthTime, "1year", TimeSpan.Zero);
            var after = DateTimeOffset.UtcNow;

            Assert.IsTrue(start.GetStdDateTimeOffset() >= before && start.GetStdDateTimeOffset() <= after);
            Assert.AreEqual(start.GetStdDateTimeOffset().AddYears(1), end.GetStdDateTimeOffset());
        }

        [TestMethod()]
        public void AutoCalculateTimeRange_PluralYearPreset_StillWorksAndIsAlsoFromNow()
        {
            var birthTime = CalculateTests.StandardHoroscope;

            var before = DateTimeOffset.UtcNow;
            var (start, end) = Calculate.AutoCalculateTimeRange(birthTime, "3years", TimeSpan.Zero);
            var after = DateTimeOffset.UtcNow;

            Assert.IsTrue(start.GetStdDateTimeOffset() >= before && start.GetStdDateTimeOffset() <= after);
            Assert.AreEqual(start.GetStdDateTimeOffset().AddYears(3), end.GetStdDateTimeOffset());
        }

        [TestMethod()]
        public void AutoCalculateTimeRange_SingularDayAndMonthPresets_AddFromNow()
        {
            var birthTime = CalculateTests.StandardHoroscope;

            var (dayStart, dayEnd) = Calculate.AutoCalculateTimeRange(birthTime, "1day", TimeSpan.Zero);
            Assert.AreEqual(dayStart.GetStdDateTimeOffset().AddDays(1), dayEnd.GetStdDateTimeOffset());

            var (monthStart, monthEnd) = Calculate.AutoCalculateTimeRange(birthTime, "3month", TimeSpan.Zero);
            Assert.AreEqual(monthStart.GetStdDateTimeOffset().AddMonths(3), monthEnd.GetStdDateTimeOffset());
        }

        /// <summary>
        /// "age"/"fulllife"/literal-year-range presets remain birth-anchored — those are inherently
        /// about the person's life span, unlike the short/medium "Nyear"-style forward windows.
        /// </summary>
        [TestMethod()]
        public void AutoCalculateTimeRange_AgeRangePreset_StaysAnchoredOnBirth()
        {
            var birthTime = CalculateTests.StandardHoroscope;

            var (start, end) = Calculate.AutoCalculateTimeRange(birthTime, "age1to10", birthTime.GetStdDateTimeOffset().Offset);

            Assert.AreEqual(birthTime.GetStdDateTimeOffset().AddYears(1), start.GetStdDateTimeOffset());
            Assert.AreEqual(birthTime.GetStdDateTimeOffset().AddYears(10), end.GetStdDateTimeOffset());
        }

        [TestMethod()]
        public void AutoCalculateTimeRange_UnrecognizedPreset_FallsBackToFullLifeFromBirth()
        {
            var birthTime = CalculateTests.StandardHoroscope;

            var (start, end) = Calculate.AutoCalculateTimeRange(birthTime, "fulllife", birthTime.GetStdDateTimeOffset().Offset);

            Assert.AreEqual(birthTime.GetStdDateTimeOffset(), start.GetStdDateTimeOffset());
            Assert.AreEqual(birthTime.GetStdDateTimeOffset().AddYears(100), end.GetStdDateTimeOffset());
        }

        /// <summary>
        /// Searching the birth year itself should converge immediately on (very near) birthTime,
        /// since the starting guess already has zero elongation difference from the natal value.
        /// </summary>
        [TestMethod()]
        public void VedicBirthDate_SameYearAsBirth_ReturnsBirthTime()
        {
            var birthTime = CalculateTests.StandardHoroscope; // 14:20 16/10/1918 +05:30

            var result = Calculate.VedicBirthDate(birthTime, birthTime.GetStdDateTimeOffset().Year);

            var hoursDiff = Math.Abs((result.GetStdDateTimeOffset() - birthTime.GetStdDateTimeOffset()).TotalHours);
            Assert.IsTrue(hoursDiff < 0.01, $"Expected result to be essentially birthTime, was {hoursDiff} hours away");
        }

        /// <summary>
        /// A future year's Vedic birthday must land on the same tithi (lunar day) as birth, and land
        /// close to the Gregorian month/day anniversary - not on some unrelated same-tithi day
        /// elsewhere in the year (there are ~12 occurrences of any given tithi per year).
        /// </summary>
        [TestMethod()]
        public void VedicBirthDate_FutureYear_MatchesNatalTithiNearAnniversary()
        {
            var birthTime = CalculateTests.StandardHoroscope; // 14:20 16/10/1918 +05:30
            var natalTithi = Calculate.LunarDay(birthTime).GetLunarDateNumber();

            var result = Calculate.VedicBirthDate(birthTime, 1923);

            Assert.AreEqual(natalTithi, Calculate.LunarDay(result).GetLunarDateNumber());
            Assert.AreEqual(1923, result.GetStdDateTimeOffset().Year);

            var daysFromAnniversary = Math.Abs((result.GetStdDateTimeOffset().DayOfYear - birthTime.GetStdDateTimeOffset().DayOfYear));
            Assert.IsTrue(daysFromAnniversary < 15, $"Expected result near the birth anniversary, was {daysFromAnniversary} days off");
        }

        /// <summary>
        /// 2023 had a well-documented Adhik Maas ("Adhik Shravan"/Purushottam Maas), reported
        /// widely as spanning approximately 18/07/2023 - 16/08/2023. A time square in the middle
        /// of that window must resolve to the Adhika variant of Sraavana.
        /// </summary>
        [TestMethod()]
        public void LunarMonth_KnownAdhikMaas2023_ReturnsAdhikaVariant()
        {
            var midAdhikMaas = new Time("12:00 01/08/2023 +05:30", GeoLocation.Bangalore);

            var result = Calculate.LunarMonth(midAdhikMaas);

            Assert.AreEqual(LunarMonth.SraavanaAdhika, result);
        }

        /// <summary>
        /// A time clearly outside any known Adhik Maas window must resolve to a plain (Nija) month,
        /// not an Adhika variant.
        /// </summary>
        [TestMethod()]
        public void LunarMonth_OrdinaryPeriod_ReturnsNijaVariant()
        {
            var earlyApril2023 = new Time("12:00 01/04/2023 +05:30", GeoLocation.Bangalore);

            var result = Calculate.LunarMonth(earlyApril2023);

            Assert.AreEqual(LunarMonth.Chaitra, result);
        }
    }
}
