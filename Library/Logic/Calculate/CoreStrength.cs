using System.Collections.Generic;
using System.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Planetary & house strength (Shadbala-family) facade methods.
    ///
    /// IMPORTANT CAVEAT: authentic classical Shadbala is a large system with 6 independent
    /// strength components (Sthana/positional, Dig/directional, Kala/temporal, Cheshta/motional,
    /// Naisargika/natural, Drik/aspectual), each built from multiple textbook sub-tables. None of
    /// that survived in this repository (see CoreTime.cs header note) - what follows is a
    /// deliberately SIMPLIFIED approximation (Naisargika + a simplified Sthana/Uchcha + a simplified
    /// Drik/aspect component) that is internally consistent and gives plausible relative strengths,
    /// but is NOT a faithful reproduction of the full classical calculation. Treat all outputs here
    /// as indicative only, and replace with a verified implementation before relying on this for
    /// precise predictive/matching work.
    /// </summary>
    public partial class Calculate
    {
        /// <summary>Classical fixed Naisargika (natural) strength, in rupas (virupas/60).</summary>
        private static readonly Dictionary<PlanetName, double> NaisargikaBalaRupas = new()
        {
            [PlanetName.Sun] = 1.0,
            [PlanetName.Moon] = 0.857,
            [PlanetName.Venus] = 0.714,
            [PlanetName.Jupiter] = 0.571,
            [PlanetName.Mercury] = 0.429,
            [PlanetName.Mars] = 0.286,
            [PlanetName.Saturn] = 0.143,
            [PlanetName.Rahu] = 0.143,
            [PlanetName.Ketu] = 0.143,
        };

        /// <summary>
        /// Simplified positional (Sthana) strength - Uchcha Bala only: proportional to angular
        /// distance from the planet's debilitation point (0 rupas at debilitation, 3 rupas at exaltation).
        /// See class-level caveat: this omits Sapta-Vargiya/Oja-Yugma/Kendradi/Drekkana sub-components.
        /// </summary>
        public static Shashtiamsa PlanetSthanaBala(PlanetName planetName, Time time) => new(PlanetSthanaBalaRupas(planetName, time) * 60.0);

        private static double PlanetSthanaBalaRupas(PlanetName planetName, Time time)
        {
            if (planetName == PlanetName.Rahu || planetName == PlanetName.Ketu) { return 1.5; } //no dignity states defined, neutral mid value

            var longitude = PlanetNirayanaLongitude(planetName, time).TotalDegrees;
            var debilitation = PlanetDebilitationPointAngle(planetName).TotalDegrees;

            //0 at debilitation, 180 at exaltation
            var distanceFromDebilitation = System.Math.Abs(((longitude - debilitation + 540.0) % 360.0) - 180.0);

            return distanceFromDebilitation.Remap(0, 180, 0, 3);
        }

        /// <summary>
        /// Simplified aspectual (Drik) strength: +0.5 rupas per benefic aspect received, -0.5 rupas
        /// per malefic aspect received. See class-level caveat.
        /// </summary>
        public static Shashtiamsa PlanetDrikBala(PlanetName planetName, Time time) => new(PlanetDrikBalaRupas(planetName, time) * 60.0);

        private static double PlanetDrikBalaRupas(PlanetName planetName, Time time)
        {
            var aspectingPlanets = PlanetsAspectingPlanet(planetName, time);
            var benefics = BeneficPlanetList(time);
            var malefics = MaleficPlanetList(time);

            double score = 0;
            foreach (var aspector in aspectingPlanets)
            {
                if (benefics.Contains(aspector)) { score += 0.5; }
                if (malefics.Contains(aspector)) { score -= 0.5; }
            }

            return score;
        }

        /// <summary>
        /// Gets total combined strength of the inputted planet (simplified Shadbala total).
        /// Alias: same as "GetPlanetShadbalaPinda", named PlanetStrength for ease of recall.
        /// </summary>
        public static Shashtiamsa PlanetStrength(PlanetName planetName, Time time) => new(PlanetStrengthRupas(planetName, time) * 60.0);

        /// <summary>Alias of PlanetStrength, kept for callers expecting this name.</summary>
        public static Shashtiamsa PlanetShadbalaPinda(PlanetName planetName, Time time) => PlanetStrength(planetName, time);

        /// <summary>Gets all 9 planets with their (simplified) strength, unordered.</summary>
        public static List<(double, PlanetName)> AllPlanetStrength(Time time) =>
            PlanetName.All9Planets.Select(p => (PlanetStrengthRupas(p, time), p)).ToList();

        /// <summary>
        /// Gets a short human-readable summary of a house's typical significations (Karakatwa),
        /// used for simple predictive text. Simplified/best-effort classical house meanings.
        /// </summary>
        public static string GetHouseTags(HouseName house) => house switch
        {
            HouseName.House1 => "Self, Personality, Health",
            HouseName.House2 => "Wealth, Family, Speech",
            HouseName.House3 => "Siblings, Courage, Communication",
            HouseName.House4 => "Home, Mother, Comforts",
            HouseName.House5 => "Children, Intellect, Creativity",
            HouseName.House6 => "Health, Enemies, Obstacles",
            HouseName.House7 => "Marriage, Partnerships",
            HouseName.House8 => "Transformation, Longevity, Mysteries",
            HouseName.House9 => "Fortune, Higher Learning, Spirituality",
            HouseName.House10 => "Career, Status, Achievements",
            HouseName.House11 => "Gains, Aspirations, Social Circle",
            HouseName.House12 => "Loss, Expenditure, Liberation",
            _ => "Unknown"
        };

        /// <summary>Gets the Yoni Kuta animal for a person, based on their Moon's birth constellation.</summary>
        public static string YoniKutaAnimalFromPerson(Person person)
        {
            var constellation = ConstellationAtLongitude(PlanetNirayanaLongitude(PlanetName.Moon, person.BirthTime)).GetConstellationName();
            return YoniKutaAnimalFromConstellation(constellation).ToString();
        }

        private static double PlanetStrengthRupas(PlanetName planetName, Time time)
        {
            var naisargika = NaisargikaBalaRupas.TryGetValue(planetName, out var n) ? n : 0;
            var sthana = PlanetSthanaBalaRupas(planetName, time);
            var drik = PlanetDrikBalaRupas(planetName, time);

            return naisargika + sthana + drik;
        }

        /// <summary>
        /// Converts a planet's strength into a value over 100, scaled by the strongest/weakest planet
        /// among the 9 at the given time.
        /// </summary>
        public static double PlanetPowerPercentage(PlanetName inputPlanet, Time time)
        {
            var allStrengths = PlanetName.All9Planets.Select(p => PlanetStrengthRupas(p, time)).ToList();
            var min = allStrengths.Min();
            var max = allStrengths.Max();

            var inputStrength = PlanetStrengthRupas(inputPlanet, time);

            if (System.Math.Abs(max - min) < 0.0001) { return 50; } //avoid divide-by-zero when all equal

            return inputStrength.Remap(min, max, 0, 100);
        }

        /// <summary>Given a list of planets, picks out the strongest based on (simplified) Shadbala.</summary>
        public static PlanetName PickOutStrongestPlanet(List<PlanetName> relatedPlanets, Time birthTime)
        {
            return relatedPlanets.OrderByDescending(p => PlanetStrengthRupas(p, birthTime)).First();
        }

        /// <summary>
        /// Checks if a planet is "strong" in (simplified) Shadbala, using the classical per-planet
        /// rupa thresholds (Sun≥5, Moon≥6, Mars≥5, Mercury≥7, Jupiter≥6.5, Venus≥5.5, Saturn≥5) -
        /// NOTE: those classical thresholds are calibrated for the FULL 6-component Shadbala (max ~60
        /// rupas for Sun), not this simplified ~4-rupas-max approximation, so they are rescaled here
        /// proportionally. See class-level caveat.
        /// </summary>
        public static bool IsPlanetStrongInShadbala(PlanetName planet, Time time)
        {
            var classicalThreshold = planet.Name switch
            {
                PlanetName.PlanetNameEnum.Sun => 5.0,
                PlanetName.PlanetNameEnum.Moon => 6.0,
                PlanetName.PlanetNameEnum.Mars => 5.0,
                PlanetName.PlanetNameEnum.Mercury => 7.0,
                PlanetName.PlanetNameEnum.Jupiter => 6.5,
                PlanetName.PlanetNameEnum.Venus => 5.5,
                PlanetName.PlanetNameEnum.Saturn => 5.0,
                _ => 5.0
            };

            //classical thresholds assume max ~60 rupas total shadbala, this approximation maxes ~4.5
            var rescaledThreshold = classicalThreshold / 60.0 * 4.5;

            return PlanetStrengthRupas(planet, time) >= rescaledThreshold;
        }

        /// <summary>Returns all 9 planets sorted by (simplified) strength, index 0 strongest to index 8 weakest.</summary>
        public static List<PlanetName> AllPlanetOrderedByStrength(Time time) =>
            PlanetName.All9Planets.OrderByDescending(p => PlanetStrengthRupas(p, time)).ToList();

        /// <summary>
        /// Bhava Bala (house strength): simplified to the combined strength of the house's lord plus
        /// any planets occupying that house. See class-level caveat.
        /// </summary>
        public static Shashtiamsa HouseStrength(HouseName inputHouse, Time time) => new(HouseStrengthRupas(inputHouse, time) * 60.0);

        private static double HouseStrengthRupas(HouseName inputHouse, Time time)
        {
            var houseSign = HouseRasiSign(inputHouse, time).GetSignName();
            var houseLord = LordOfZodiacSign(houseSign);

            var score = PlanetStrengthRupas(houseLord, time);

            var occupants = PlanetsInHouseBasedOnSign(inputHouse, time);
            score += occupants.Sum(p => PlanetStrengthRupas(p, time));

            //scale up to a "score out of ~600" range to match the classical 450-threshold convention
            return score * 100;
        }

        /// <summary>Sets a house as benefic in Shadbala if its (simplified) HouseStrength is above the given threshold.</summary>
        public static bool IsHouseBeneficInShadbala(HouseName house, Time birthTime, double threshold) =>
            HouseStrengthRupas(house, birthTime) >= threshold;

        /// <summary>Returns all 12 houses sorted by (simplified) strength, index 0 strongest to index 11 weakest.</summary>
        public static List<HouseName> AllHousesOrderedByStrength(Time time) =>
            House.AllHouses.OrderByDescending(h => HouseStrengthRupas(h, time)).ToList();
    }
}
