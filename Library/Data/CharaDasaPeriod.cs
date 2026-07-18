namespace VedAstro.Library
{
    /// <summary>
    /// This type was missing from Library entirely prior to this fix; implemented here as
    /// best-effort based on standard Jaimini Chara Dasa astrology rules. Verify against a
    /// second source if this is used for production predictions.
    ///
    /// Represents a single Jaimini Chara Dasa "Mahadasa" period - a sign (Rasi) ruling a
    /// span of years, counted from the sign occupied by that sign's lord.
    /// </summary>
    public struct CharaDasaPeriod
    {
        /// <summary>The Rasi (zodiac sign) ruling this Dasa period</summary>
        public ZodiacName Sign { get; set; }

        /// <summary>Length of this Dasa period in years (Jaimini Chara Dasa rule, 1-12 years each)</summary>
        public double DurationYears { get; set; }

        /// <summary>Start of this Dasa period</summary>
        public Time StartTime { get; set; }

        /// <summary>End of this Dasa period (exclusive)</summary>
        public Time EndTime { get; set; }
    }
}
