namespace VedAstro.Library
{
    /// <summary>
    /// The 7 Choghadiya names, in their fixed cyclic order (each successive period, day or night,
    /// advances one step through this cycle - which name starts the cycle depends on weekday, see
    /// Calculate.ChoghadiyaPeriods).
    /// </summary>
    public enum ChoghadiyaName
    {
        Udveg,
        Chal,
        Labh,
        Amrit,
        Kaal,
        Shubh,
        Rog,
    }

    /// <summary>Whether a Choghadiya period is a good, neutral, or bad time to start something.</summary>
    public enum ChoghadiyaQuality
    {
        Auspicious,
        Neutral,
        Inauspicious,
    }
}
