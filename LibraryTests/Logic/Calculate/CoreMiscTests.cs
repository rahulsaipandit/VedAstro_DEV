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
    }
}
