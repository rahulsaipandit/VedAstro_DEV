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

        [TestMethod()]
        public void TithiTransition_EndTimeMatchesLunarDayBoundary()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            var transition = Calculate.TithiTransition(time);

            Assert.AreEqual(Calculate.LunarDay(time).GetLunarDateNumber(), transition.Current.GetLunarDateNumber());
            Assert.IsTrue(transition.EndTime.GetStdDateTimeOffset() > time.GetStdDateTimeOffset());
            Assert.AreNotEqual(transition.Current.GetLunarDateNumber(), transition.Next.GetLunarDateNumber());

            // just after EndTime must already read as Next; just before must still read as Current
            var justAfter = transition.EndTime.AddHours(1.0 / 60);
            var justBefore = transition.EndTime.AddHours(-1.0 / 60);
            Assert.AreEqual(transition.Next.GetLunarDateNumber(), Calculate.LunarDay(justAfter).GetLunarDateNumber());
            Assert.AreEqual(transition.Current.GetLunarDateNumber(), Calculate.LunarDay(justBefore).GetLunarDateNumber());
        }

        [TestMethod()]
        public void NakshatraTransition_CurrentAndNextDiffer()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            var transition = Calculate.NakshatraTransition(time);

            Assert.AreEqual(Calculate.MoonConstellation(time).GetConstellationName(), transition.Current.GetConstellationName());
            Assert.AreNotEqual(transition.Current.GetConstellationName(), transition.Next.GetConstellationName());
            Assert.IsTrue(transition.EndTime.GetStdDateTimeOffset() > time.GetStdDateTimeOffset());
        }

        [TestMethod()]
        public void YogaTransition_CurrentAndNextDiffer()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            var transition = Calculate.YogaTransition(time);

            Assert.AreEqual(Calculate.NithyaYoga(time).Name, transition.Current.Name);
            Assert.AreNotEqual(transition.Current.Name, transition.Next.Name);
            Assert.IsTrue(transition.EndTime.GetStdDateTimeOffset() > time.GetStdDateTimeOffset());
        }

        [TestMethod()]
        public void KaranaTransition_CurrentAndNextDiffer()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            var transition = Calculate.KaranaTransition(time);

            Assert.AreEqual(Calculate.Karana(time), transition.Current);
            Assert.AreNotEqual(transition.Current, transition.Next);
            Assert.IsTrue(transition.EndTime.GetStdDateTimeOffset() > time.GetStdDateTimeOffset());
        }

        [TestMethod()]
        public void DurMuhurtaPeriods_FallWithinDayOrNightAndMatchWeekdayCount()
        {
            var expectedCountByWeekday = new Dictionary<DayOfWeek, int>
            {
                [DayOfWeek.Sunday] = 1,
                [DayOfWeek.Monday] = 2,
                [DayOfWeek.Tuesday] = 2,
                [DayOfWeek.Wednesday] = 1,
                [DayOfWeek.Thursday] = 2,
                [DayOfWeek.Friday] = 2,
                [DayOfWeek.Saturday] = 1,
            };

            var start = new Time("12:00 01/01/2024 +05:30", GeoLocation.Bangalore);
            var seenWeekdays = new HashSet<DayOfWeek>();

            for (var d = 0; d < 7; d++)
            {
                var day = start.AddHours(24 * d);
                var vedicWeekday = Calculate.DayOfWeek(day);
                var periods = Calculate.DurMuhurtaPeriods(day);

                Assert.AreEqual(expectedCountByWeekday[vedicWeekday], periods.Count, $"Mismatch for {vedicWeekday}");
                foreach (var period in periods)
                {
                    Assert.IsTrue(period.end.GetStdDateTimeOffset() > period.start.GetStdDateTimeOffset());
                }
                seenWeekdays.Add(vedicWeekday);
            }

            Assert.AreEqual(7, seenWeekdays.Count, "Expected all 7 weekdays to be covered by this 7-day scan");
        }

        /// <summary>
        /// Mid-June is always well after that Gregorian year's Chaitra Shukla Pratipada (always
        /// March/April), so this must land on Gregorian year + 57 regardless of exact New Year
        /// search precision.
        /// </summary>
        [TestMethod()]
        public void VikramSamvatYear_MidJune_IsGregorianPlus57()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            Assert.AreEqual(2081, Calculate.VikramSamvatYear(time));
        }

        /// <summary>Mid-January is always well before that Gregorian year's own Chaitra Shukla Pratipada, so it's still the previous Vikram Samvat year (+56).</summary>
        [TestMethod()]
        public void VikramSamvatYear_MidJanuary_IsGregorianPlus56()
        {
            var time = new Time("12:00 15/01/2024 +05:30", GeoLocation.Bangalore);

            Assert.AreEqual(2080, Calculate.VikramSamvatYear(time));
        }

        /// <summary>
        /// Between Chaitra Shukla Pratipada and Kartika Shukla Pratipada, Gujarati Samvat trails
        /// the main Vikram Samvat by 1 (it hasn't rolled over at Diwali/Kartika yet).
        /// </summary>
        [TestMethod()]
        public void GujaratiSamvatYear_MidJune_TrailsVikramSamvatByOne()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            Assert.AreEqual(2081, Calculate.VikramSamvatYear(time));
            Assert.AreEqual(2080, Calculate.GujaratiSamvatYear(time));
        }

        /// <summary>After Kartika Shukla Pratipada (just after Diwali), Gujarati Samvat catches up to match Vikram Samvat for the rest of the year.</summary>
        [TestMethod()]
        public void GujaratiSamvatYear_LateNovember_MatchesVikramSamvat()
        {
            var time = new Time("12:00 20/11/2024 +05:30", GeoLocation.Bangalore);

            Assert.AreEqual(Calculate.VikramSamvatYear(time), Calculate.GujaratiSamvatYear(time));
        }

        [TestMethod()]
        public void SakaSamvatYear_IsGregorianMinus78Or79MatchingVikramSamvatBoundary()
        {
            var afterChaitra = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);
            var beforeChaitra = new Time("12:00 15/01/2024 +05:30", GeoLocation.Bangalore);

            Assert.AreEqual(1946, Calculate.SakaSamvatYear(afterChaitra));
            Assert.AreEqual(1945, Calculate.SakaSamvatYear(beforeChaitra));
        }

        [TestMethod()]
        public void KaliSamvatYear_IsGregorianPlus3101Or3100MatchingVikramSamvatBoundary()
        {
            var afterChaitra = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);
            var beforeChaitra = new Time("12:00 15/01/2024 +05:30", GeoLocation.Bangalore);

            Assert.AreEqual(5125, Calculate.KaliSamvatYear(afterChaitra));
            Assert.AreEqual(5124, Calculate.KaliSamvatYear(beforeChaitra));
        }

        /// <summary>
        /// Pinned against a confirmed real-world example: Vikram Samvat 2083 (mid-2026) is
        /// "Parabhava" (South) / "Raudri" (North) - see SamvatsaraNameNorth's doc comment.
        /// </summary>
        [TestMethod()]
        public void SamvatsaraName_VikramSamvat2083_IsParabhavaSouthRaudriNorth()
        {
            var time = new Time("12:00 15/06/2026 +05:30", GeoLocation.Bangalore);
            Assert.AreEqual(2083, Calculate.VikramSamvatYear(time));

            Assert.AreEqual("Parabhava", Calculate.SamvatsaraName(time));
            Assert.AreEqual("Raudri", Calculate.SamvatsaraNameNorth(time));
        }

        /// <summary>Diwali's Amavasya is Amanta Aaswayuja (Krishna paksha) - its Purnimanta name shifts one month ahead, to Kaarteeka, matching how "Kartik Amavasya" is popularly named.</summary>
        [TestMethod()]
        public void PurnimantaMonth_Diwali_IsKaarteekaNotAaswayuja()
        {
            var diwali = Calculate.FestivalDate(FestivalName.Diwali, 2023, GeoLocation.Bangalore);

            Assert.AreEqual(LunarMonth.Aaswayuja, Calculate.LunarMonth(diwali));
            Assert.AreEqual(LunarMonth.Kaarteeka, Calculate.PurnimantaMonth(diwali));
        }

        /// <summary>Ramnavami (Chaitra Shukla, Shukla paksha) needs no Amanta/Purnimanta shift.</summary>
        [TestMethod()]
        public void PurnimantaMonth_ShuklaPaksha_MatchesAmantaMonth()
        {
            var ramnavami = Calculate.FestivalDate(FestivalName.Ramnavami, 2024, GeoLocation.Bangalore);

            Assert.AreEqual(LunarMonth.Chaitra, Calculate.LunarMonth(ramnavami));
            Assert.AreEqual(LunarMonth.Chaitra, Calculate.PurnimantaMonth(ramnavami));
        }

        [TestMethod()]
        public void Ritu_RamnavamiInChaitra_IsVasanta()
        {
            var ramnavami = Calculate.FestivalDate(FestivalName.Ramnavami, 2024, GeoLocation.Bangalore);
            Assert.AreEqual(RituName.Vasanta, Calculate.Ritu(ramnavami));
        }

        [TestMethod()]
        public void Ayana_JustAfterMakarSankranti_IsUttarayana()
        {
            var makarSankranti = Calculate.FestivalDate(FestivalName.MakarSankranti, 2024, GeoLocation.Bangalore);
            var justAfter = makarSankranti.AddHours(24);

            Assert.AreEqual(AyanaName.Uttarayana, Calculate.Ayana(justAfter));
        }

        /// <summary>
        /// Sidereal Karka Sankranti (Dakshinayana's start) falls in mid-July, not the tropical
        /// summer solstice's June 21 - Lahiri ayanamsa has drifted the sidereal Rashi boundary
        /// ~24 days later than the real solstice. Picking a date safely after that drift.
        /// </summary>
        [TestMethod()]
        public void Ayana_EarlyAugust_IsDakshinayana()
        {
            var earlyAugust = new Time("12:00 01/08/2024 +05:30", GeoLocation.Bangalore);
            Assert.AreEqual(AyanaName.Dakshinayana, Calculate.Ayana(earlyAugust));
        }

        [TestMethod()]
        public void MoonZodiacTransition_CurrentAndNextDiffer()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            var transition = Calculate.MoonZodiacTransition(time);

            Assert.AreNotEqual(transition.Current, transition.Next);
            Assert.IsTrue(transition.EndTime.GetStdDateTimeOffset() > time.GetStdDateTimeOffset());
        }

        [TestMethod()]
        public void MoonriseAndMoonsetTime_ReturnDistinctValidTimesNearQueriedDay()
        {
            var time = new Time("12:00 15/06/2024 +05:30", GeoLocation.Bangalore);

            var moonrise = Calculate.MoonriseTime(time);
            var moonset = Calculate.MoonsetTime(time);

            Assert.AreNotEqual(moonrise.GetStdDateTimeOffset(), moonset.GetStdDateTimeOffset());

            var dayStart = new Time("00:00 14/06/2024 +05:30", GeoLocation.Bangalore).GetStdDateTimeOffset();
            var dayEnd = new Time("00:00 17/06/2024 +05:30", GeoLocation.Bangalore).GetStdDateTimeOffset();
            Assert.IsTrue(moonrise.GetStdDateTimeOffset() > dayStart && moonrise.GetStdDateTimeOffset() < dayEnd);
            Assert.IsTrue(moonset.GetStdDateTimeOffset() > dayStart && moonset.GetStdDateTimeOffset() < dayEnd);
        }
    }
}
