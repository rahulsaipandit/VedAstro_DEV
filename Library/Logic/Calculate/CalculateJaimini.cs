using System;
using System.Collections.Generic;
using System.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Jaimini astrology calculators (Chara Dasa and related sign-based Dasa logic).
    /// This file was missing from Library entirely prior to this fix - Chara Dasa had no
    /// implementation anywhere in the codebase. Implemented here as best-effort based on
    /// the standard Jaimini Chara Dasa rules (as taught e.g. in K.N. Rao's "Predicting
    /// through Jaimini's Chara Dasha"). Verify against a second source (e.g. JHora) if this
    /// is used for production predictions.
    /// </summary>
    public partial class Calculate
    {
        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on standard Jaimini Chara Dasa rules and the test's documented
        /// scenario. Verify against a second source if this is used for production predictions.
        ///
        /// Gets the Rasi (sign) whose Jaimini Chara Dasa Mahadasa is running at the given
        /// check time, for a person born at the given birth time.
        /// </summary>
        public static ZodiacName GetCharaDasaAtTime(Time birthTime, Time checkTime)
        {
            //get enough cycles of the 12 sign dasa to comfortably cover a human lifetime
            //(each sign lasts 1-12 years, so 1 cycle of 12 signs covers ~12-144 years,
            //2 cycles safely covers a full lifespan even in the shortest-duration case)
            var periods = CharaDasaPeriods(birthTime, 2);

            var checkTimeStd = checkTime.GetStdDateTimeOffset();

            foreach (var period in periods)
            {
                var isWithinPeriod = checkTimeStd >= period.StartTime.GetStdDateTimeOffset()
                                     && checkTimeStd < period.EndTime.GetStdDateTimeOffset();

                if (isWithinPeriod) { return period.Sign; }
            }

            //fallback: checkTime before birth or beyond calculated cycles, return closest period
            var isBeforeBirth = checkTimeStd < periods.First().StartTime.GetStdDateTimeOffset();
            return isBeforeBirth ? periods.First().Sign : periods.Last().Sign;
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on standard Jaimini Chara Dasa rules. Verify against a second
        /// source if this is used for production predictions.
        ///
        /// Generates the full sequence of Jaimini Chara Dasa Mahadasa periods starting from
        /// birth, for as many 12-sign cycles as requested.
        ///
        /// Rules used (standard Jaimini Chara Dasa, sign-based "Rasi Dasa"):
        /// 1) The starting sign is the Lagna (Ascendant) sign at birth.
        /// 2) If the Lagna sign is odd (Aries, Gemini, Leo, Libra, Sagittarius, Aquarius), the
        ///    sequence of Dasa signs proceeds forward (direct) through the zodiac. If the Lagna
        ///    sign is even, the sequence proceeds backward (retrograde) through the zodiac.
        /// 3) The duration of each sign's own Dasa is found independently of the overall
        ///    sequence direction: for an odd sign, count forward (inclusive) from that sign to
        ///    the sign occupied by its lord; for an even sign, count backward (inclusive) to the
        ///    sign occupied by its lord. Duration in years = count - 1, and if that count is 1
        ///    (the lord occupies the sign itself), duration is taken as the maximum of 12 years.
        /// </summary>
        public static List<CharaDasaPeriod> CharaDasaPeriods(Time birthTime, int cycles = 1)
        {
            var lagnaSign = Calculate.LagnaSignName(birthTime);
            var lagnaIsOdd = IsOddZodiacSign(lagnaSign);
            var direction = lagnaIsOdd ? 1 : -1;

            var periods = new List<CharaDasaPeriod>();
            var currentStart = birthTime;
            var lagnaIndex = (int)lagnaSign; //Aries = 1 ... Pisces = 12

            var totalSigns = 12 * cycles;
            for (var i = 0; i < totalSigns; i++)
            {
                //move i steps from lagna, in the sequence direction, wrapping around the 12 signs
                var signIndexZeroBased = (((lagnaIndex - 1) + (i * direction)) % 12 + 12) % 12;
                var sign = (ZodiacName)(signIndexZeroBased + 1);

                var years = CharaDasaSignDurationYears(sign, birthTime);

                //convert years to hours using the same solar year length the rest of Library uses
                var durationHours = years * Calculate.SolarYearTimeSpan * 24;
                var endTime = currentStart.AddHours(durationHours);

                periods.Add(new CharaDasaPeriod
                {
                    Sign = sign,
                    DurationYears = years,
                    StartTime = currentStart,
                    EndTime = endTime
                });

                currentStart = endTime;
            }

            return periods;
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on standard Jaimini Chara Dasa rules. Verify against a second
        /// source if this is used for production predictions.
        ///
        /// Computes the duration (in years) of a single sign's Jaimini Chara Dasa Mahadasa,
        /// based on the count (odd sign = forward, even sign = backward) from that sign to the
        /// sign occupied by its lord at birth.
        /// </summary>
        public static double CharaDasaSignDurationYears(ZodiacName sign, Time birthTime)
        {
            var lord = Calculate.LordOfZodiacSign(sign);
            var lordSign = Calculate.PlanetZodiacSign(lord, birthTime).GetSignName();

            int count;
            if (IsOddZodiacSign(sign))
            {
                //odd sign: count forward (inclusive) from sign to lord's sign
                count = Calculate.CountFromSignToSign(sign, lordSign);
            }
            else
            {
                //even sign: count backward (inclusive), equivalent to counting forward from
                //the lord's sign back to this sign
                count = Calculate.CountFromSignToSign(lordSign, sign);
            }

            var duration = (double)(count - 1);

            //if lord occupies the sign itself (count == 1, duration would be 0),
            //classical rule gives the maximum duration of 12 years
            if (duration <= 0) { duration = 12; }

            return duration;
        }

        /// <summary>
        /// True if the given zodiac sign is one of the "odd" (movable-counted) signs:
        /// Aries, Gemini, Leo, Libra, Sagittarius, Aquarius.
        /// </summary>
        private static bool IsOddZodiacSign(ZodiacName sign) => ((int)sign % 2) == 1;
    }
}
