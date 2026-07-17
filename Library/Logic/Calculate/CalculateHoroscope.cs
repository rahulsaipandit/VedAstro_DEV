namespace VedAstro.Library
{
    /// <summary>
    /// Standalone horoscope rule checks used by match-making (MatchReportFactory).
    /// Reconstructed from scratch - see CoreTime.cs header note for context.
    /// </summary>
    public static class CalculateHoroscope
    {
        /// <summary>Checks if Mars or Venus occupies the 7th house from Lagna.</summary>
        public static CalculatorResult MarsVenusIn7th(Time birthTime)
        {
            var planetsIn7th = Calculate.PlanetsInHouseBasedOnSign(HouseName.House7, birthTime);
            var occuring = planetsIn7th.Contains(PlanetName.Mars) || planetsIn7th.Contains(PlanetName.Venus);

            return CalculatorResult.New(occuring, new[] { PlanetName.Mars, PlanetName.Venus }, birthTime);
        }

        /// <summary>Checks if Mercury or Jupiter occupies the 7th house from Lagna.</summary>
        public static CalculatorResult MercuryOrJupiterIn7th(Time birthTime)
        {
            var planetsIn7th = Calculate.PlanetsInHouseBasedOnSign(HouseName.House7, birthTime);
            var occuring = planetsIn7th.Contains(PlanetName.Mercury) || planetsIn7th.Contains(PlanetName.Jupiter);

            return CalculatorResult.New(occuring, new[] { PlanetName.Mercury, PlanetName.Jupiter }, birthTime);
        }
    }
}
