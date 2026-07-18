using System;
using System.Collections.Generic;
using System.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Planet relationships, aspects, conjunctions & benefic/malefic classification.
    /// Reconstructed from scratch - see CoreTime.cs header note.
    /// </summary>
    public partial class Calculate
    {
        /// <summary>
        /// Gets a planet's permanent (Naisargika) relationship with another planet.
        /// Based on Hindu Predictive Astrology, pg. 21 (B.V. Raman).
        /// Note: Rahu/Ketu are not mentioned in any permanent relationship by Raman -
        /// treated as Neutral to everyone here since no authoritative source defines it.
        /// </summary>
        public static PlanetToPlanetRelationship PlanetPermanentRelationshipWithPlanet(PlanetName mainPlanet, PlanetName secondaryPlanet)
        {
            if (mainPlanet == secondaryPlanet) { return PlanetToPlanetRelationship.SamePlanet; }

            if (mainPlanet == PlanetName.Rahu || mainPlanet == PlanetName.Ketu ||
                secondaryPlanet == PlanetName.Rahu || secondaryPlanet == PlanetName.Ketu)
            {
                return PlanetToPlanetRelationship.Neutral;
            }

            var friends = new Dictionary<PlanetName, List<PlanetName>>
            {
                [PlanetName.Sun] = new() { PlanetName.Moon, PlanetName.Mars, PlanetName.Jupiter },
                [PlanetName.Moon] = new() { PlanetName.Sun, PlanetName.Mercury },
                [PlanetName.Mars] = new() { PlanetName.Sun, PlanetName.Moon, PlanetName.Jupiter },
                [PlanetName.Mercury] = new() { PlanetName.Sun, PlanetName.Venus },
                [PlanetName.Jupiter] = new() { PlanetName.Sun, PlanetName.Moon, PlanetName.Mars },
                [PlanetName.Venus] = new() { PlanetName.Mercury, PlanetName.Saturn },
                [PlanetName.Saturn] = new() { PlanetName.Mercury, PlanetName.Venus },
            };

            var enemies = new Dictionary<PlanetName, List<PlanetName>>
            {
                [PlanetName.Sun] = new() { PlanetName.Venus, PlanetName.Saturn },
                [PlanetName.Moon] = new(),
                [PlanetName.Mars] = new() { PlanetName.Mercury },
                [PlanetName.Mercury] = new() { PlanetName.Moon },
                [PlanetName.Jupiter] = new() { PlanetName.Mercury, PlanetName.Venus },
                [PlanetName.Venus] = new() { PlanetName.Sun, PlanetName.Moon },
                [PlanetName.Saturn] = new() { PlanetName.Sun, PlanetName.Moon, PlanetName.Mars },
            };

            if (friends.TryGetValue(mainPlanet, out var friendList) && friendList.Contains(secondaryPlanet)) { return PlanetToPlanetRelationship.Friend; }
            if (enemies.TryGetValue(mainPlanet, out var enemyList) && enemyList.Contains(secondaryPlanet)) { return PlanetToPlanetRelationship.Enemy; }

            return PlanetToPlanetRelationship.Neutral;
        }

        /// <summary>
        /// Gets all planets that are benefics at a given time (Sun excluded by default as a mild/neutral,
        /// traditionally counted separately). Jupiter & Venus are always benefic. Mercury is benefic when
        /// not conjunct with malefics. Moon is benefic when waxing (Shukla Paksha, bright half).
        /// </summary>
        public static List<PlanetName> BeneficPlanetList(Time time)
        {
            var result = new List<PlanetName> { PlanetName.Jupiter, PlanetName.Venus };

            //Moon: benefic when waxing (bright half of the lunar month)
            var moonLongitude = PlanetNirayanaLongitude(PlanetName.Moon, time).TotalDegrees;
            var sunLongitude = PlanetNirayanaLongitude(PlanetName.Sun, time).TotalDegrees;
            var moonFromSun = (moonLongitude - sunLongitude + 360.0) % 360.0;
            var isWaxing = moonFromSun < 180.0;
            if (isWaxing) { result.Add(PlanetName.Moon); }

            //Mercury: benefic unless conjunct with a malefic (Sun/Mars/Saturn/Rahu/Ketu)
            var permanentMalefics = new List<PlanetName> { PlanetName.Sun, PlanetName.Mars, PlanetName.Saturn, PlanetName.Rahu, PlanetName.Ketu };
            var mercuryConjunctMalefic = permanentMalefics.Any(malefic => IsPlanetConjunctWithPlanet(PlanetName.Mercury, malefic, time));
            if (!mercuryConjunctMalefic) { result.Add(PlanetName.Mercury); }

            return result;
        }

        /// <summary>
        /// Gets list of malefic planets at a given time. Sun, Mars, Saturn, Rahu, Ketu are permanent malefics.
        /// Moon is malefic when waning (dark half). Mercury is malefic when conjunct with a malefic.
        /// </summary>
        public static List<PlanetName> MaleficPlanetList(Time time)
        {
            var result = new List<PlanetName> { PlanetName.Sun, PlanetName.Mars, PlanetName.Saturn, PlanetName.Rahu, PlanetName.Ketu };

            var beneficList = BeneficPlanetList(time);
            if (!beneficList.Contains(PlanetName.Moon)) { result.Add(PlanetName.Moon); }
            if (!beneficList.Contains(PlanetName.Mercury)) { result.Add(PlanetName.Mercury); }

            return result;
        }

        /// <summary>
        /// Checks if a planet is conjunct with another planet, based on Nirayana longitudes.
        /// Uses the classical whole-sign definition: conjunct if both planets share the same Rasi (D1) sign.
        /// </summary>
        public static bool IsPlanetConjunctWithPlanet(PlanetName planetA, PlanetName planetB, Time time)
        {
            if (planetA == planetB) { return true; }

            var signA = PlanetRasiD1Sign(planetA, time).GetSignName();
            var signB = PlanetRasiD1Sign(planetB, time).GetSignName();

            return signA == signB;
        }

        /// <summary>
        /// Gets the longitudinal space between 2 planets, in Nirayana longitudes.
        /// Note: longitude wraps at 360°/0°, accounted for here.
        /// </summary>
        public static Angle DistanceBetweenPlanets(PlanetName planet1, PlanetName planet2, Time time) =>
            DistanceBetweenPlanets(PlanetNirayanaLongitude(planet1, time), PlanetNirayanaLongitude(planet2, time));

        /// <summary>Overload taking already-computed longitudes directly, to avoid recomputing them in a loop.</summary>
        public static Angle DistanceBetweenPlanets(Angle longitude1, Angle longitude2)
        {
            var diff = (longitude2.TotalDegrees - longitude1.TotalDegrees + 360.0) % 360.0;
            return Angle.FromDegrees(diff);
        }

        /// <summary>
        /// Gets the house-count (1 to 12) from one house to another, counting inclusively as per
        /// classical Vedic astrology (same house = 1st).
        /// </summary>
        private static int HouseCountFrom(ZodiacName fromSign, ZodiacName toSign)
        {
            var fromIndex = ZodiacSign.All12ZodiacNames.IndexOf(fromSign);
            var toIndex = ZodiacSign.All12ZodiacNames.IndexOf(toSign);

            var diff = toIndex - fromIndex;
            if (diff < 0) { diff += 12; }

            return diff + 1; //1-based, "1st from itself"
        }

        /// <summary>
        /// Checks if a transmitting planet aspects a given house-count away from it.
        /// Every planet aspects the 7th house from itself. Mars additionally aspects 4th & 8th.
        /// Jupiter additionally aspects 5th & 9th. Saturn additionally aspects 3rd & 10th.
        /// </summary>
        private static bool IsAspectingHouseCount(PlanetName transmittingAspect, int houseCount)
        {
            if (houseCount == 7) { return true; }

            if (transmittingAspect == PlanetName.Mars && (houseCount == 4 || houseCount == 8)) { return true; }
            if (transmittingAspect == PlanetName.Jupiter && (houseCount == 5 || houseCount == 9)) { return true; }
            if (transmittingAspect == PlanetName.Saturn && (houseCount == 3 || houseCount == 10)) { return true; }

            return false;
        }

        /// <summary>Checks if a planet is aspected by another planet.</summary>
        public static bool IsPlanetAspectedByPlanet(PlanetName receiveingAspect, PlanetName transmitingAspect, Time time)
        {
            if (receiveingAspect == transmitingAspect) { return false; }

            var fromSign = PlanetRasiD1Sign(transmitingAspect, time).GetSignName();
            var toSign = PlanetRasiD1Sign(receiveingAspect, time).GetSignName();

            return IsAspectingHouseCount(transmitingAspect, HouseCountFrom(fromSign, toSign));
        }

        /// <summary>Checks if a house is aspected by a planet.</summary>
        public static bool IsHouseAspectedByPlanet(HouseName receiveingAspect, PlanetName transmitingAspect, Time time)
        {
            var fromSign = PlanetRasiD1Sign(transmitingAspect, time).GetSignName();
            var toSign = HouseRasiSign(receiveingAspect, time).GetSignName();

            return IsAspectingHouseCount(transmitingAspect, HouseCountFrom(fromSign, toSign));
        }

        /// <summary>Gets all planets that transmit an aspect to the inputted planet.</summary>
        public static List<PlanetName> PlanetsAspectingPlanet(PlanetName receivingAspect, Time time)
        {
            return PlanetName.All9Planets.Where(planet => IsPlanetAspectedByPlanet(receivingAspect, planet, time)).ToList();
        }

        /// <summary>
        /// Checks if a planet is in the same house (not necessarily conjunct) as the lord of a certain house.
        /// Example: Is Sun joined with lord of 9th.
        /// </summary>
        public static bool IsPlanetSameHouseWithHouseLord(int houseNumber, PlanetName planet, Time birthTime)
        {
            var house = (HouseName)houseNumber;
            var houseLord = LordOfZodiacSign(HouseRasiSign(house, birthTime).GetSignName());

            var planetSign = PlanetRasiD1Sign(planet, birthTime).GetSignName();
            var lordSign = PlanetRasiD1Sign(houseLord, birthTime).GetSignName();

            return planetSign == lordSign;
        }

        /// <summary>
        /// Given a constellation, gives the Yoni Kuta animal (with gender), used for
        /// yoni kuta compatibility calculations and body-appearance prediction.
        /// Standard 27-nakshatra Yoni table.
        /// </summary>
        public static ConstellationAnimal YoniKutaAnimalFromConstellation(ConstellationName sign)
        {
            var table = new Dictionary<ConstellationName, ConstellationAnimal>
            {
                [ConstellationName.Aswini] = new("Male", AnimalName.Horse),
                [ConstellationName.Bharani] = new("Male", AnimalName.Elephant),
                [ConstellationName.Krithika] = new("Female", AnimalName.Sheep),
                [ConstellationName.Rohini] = new("Male", AnimalName.Serpent),
                [ConstellationName.Mrigasira] = new("Female", AnimalName.Serpent),
                [ConstellationName.Aridra] = new("Female", AnimalName.Dog),
                [ConstellationName.Punarvasu] = new("Male", AnimalName.Cat),
                [ConstellationName.Pushyami] = new("Male", AnimalName.Sheep),
                [ConstellationName.Aslesha] = new("Female", AnimalName.Cat),
                [ConstellationName.Makha] = new("Male", AnimalName.Rat),
                [ConstellationName.Pubba] = new("Female", AnimalName.Rat),
                [ConstellationName.Uttara] = new("Male", AnimalName.Cow),
                [ConstellationName.Hasta] = new("Female", AnimalName.Buffalo),
                [ConstellationName.Chitta] = new("Female", AnimalName.Tiger),
                [ConstellationName.Swathi] = new("Male", AnimalName.Buffalo),
                [ConstellationName.Vishhaka] = new("Male", AnimalName.Tiger),
                [ConstellationName.Anuradha] = new("Female", AnimalName.Hare),
                [ConstellationName.Jyesta] = new("Male", AnimalName.Hare),
                [ConstellationName.Moola] = new("Male", AnimalName.Dog),
                [ConstellationName.Poorvashada] = new("Female", AnimalName.Monkey),
                [ConstellationName.Uttarashada] = new("Male", AnimalName.Mongoose),
                [ConstellationName.Sravana] = new("Male", AnimalName.Monkey),
                [ConstellationName.Dhanishta] = new("Female", AnimalName.Lion),
                [ConstellationName.Satabhisha] = new("Female", AnimalName.Horse),
                [ConstellationName.Poorvabhadra] = new("Male", AnimalName.Lion),
                [ConstellationName.Uttarabhadra] = new("Female", AnimalName.Cow),
                [ConstellationName.Revathi] = new("Female", AnimalName.Elephant),
            };

            return table.TryGetValue(sign, out var animal) ? animal : new ConstellationAnimal("Male", AnimalName.Horse);
        }

        /// <summary>
        /// Gets the Vedic Day Of Week. The Hindu day begins with sunrise and continues till the next sunrise -
        /// so a time before that calendar day's sunrise belongs to the previous weekday.
        /// </summary>
        public static DayOfWeek DayOfWeek(Time time)
        {
            var sunrise = SunriseTime(time);

            var effectiveInstant = time.GetStdDateTimeOffset() < sunrise.GetStdDateTimeOffset()
                ? time.GetStdDateTimeOffset().AddDays(-1)
                : time.GetStdDateTimeOffset();

            //System.DayOfWeek is 0-based (Sunday=0), VedAstro.Library.DayOfWeek is 1-based (Sunday=1)
            return (DayOfWeek)((int)effectiveInstant.DayOfWeek + 1);
        }

        //-------------------------------------------------------------------------
        // Restored from pre-existing history (see docs/Library/Logic/Calculate/Calculate.cs)
        // for use by the restored CalculateHoroscope.cs. Only renamed dependency:
        // AllHouseMiddleLongitudes -> AllHouseLongitudes (current name for the same method).

        /// <summary>
        /// Gets the House number a given planet is in at a time
        /// </summary>
        public static HouseName HousePlanetOccupies(PlanetName planetName, Time time)
        {
            //get the planets longitude
            var planetLongitude = PlanetNirayanaLongitude(planetName, time);

            //get all houses
            var houseList = AllHouseLongitudes(time);

            //loop through all houses
            foreach (var house in houseList)
            {
                //check if planet is in house's range
                var planetIsInHouse = house.IsLongitudeInHouseRange(planetLongitude);

                //if planet is in house
                if (planetIsInHouse)
                {
                    //return house's number
                    return house.GetHouseName();
                }
            }

            //if planet not found in any house, raise error
            throw new Exception("Planet not in any house, error!");
        }

        /// <summary>
        /// Checks if the lord of a house is in the specified house.
        /// Example question : Is Lord of 1st house in 2nd house?
        /// </summary>
        public static bool IsHouseLordInHouse(HouseName lordHouse, HouseName occupiedHouse, Time time)
        {
            //get the house lord
            var houseLord = LordOfHouse(lordHouse, time);

            //get house the lord is in
            var houseIsIn = HousePlanetOccupies(houseLord, time);

            //if it matches then occuring
            return houseIsIn == occupiedHouse;
        }

        /// <summary>
        /// Checks if any planet in list is at a given house at a specified time
        /// </summary>
        public static bool IsAnyPlanetInHouse(List<PlanetName> planetList, HouseName houseNumber, Time time)
        {
            //calculate each planet, even if 1 planet is out, then return as false
            foreach (var planetName in planetList)
            {
                var tempVal = IsPlanetInHouse(planetName, houseNumber, time);
                if (tempVal == true) { return true; }
            }

            // if control reaches here then no planet is in house
            return false;
        }

        /// <summary>
        /// Checks if a given house's zodiac sign matches the input sign
        /// </summary>
        public static bool IsHouseSignName(HouseName house, ZodiacName sign, Time time) => HouseSignName(house, time) == sign;

        /// <summary>
        /// Checks if a given planet is Malefic
        /// </summary>
        public static bool IsPlanetMalefic(PlanetName planetName, Time time)
        {
            //get list of malefic planets
            var maleficPlanetList = MaleficPlanetList(time);

            //check if input planet is in the list
            var planetIsMalefic = maleficPlanetList.Contains(planetName);

            return planetIsMalefic;
        }

        /// <summary>
        /// Given a birth time will calculate all predictions that match for given birth time.
        /// Default includes all predictions, ie: Yoga, Planets in Sign, AshtakavargaYoga
        /// Can be filtered.
        /// </summary>
        /// <param name="filterTag">Set to only show certain types of predictions</param>
        public static List<HoroscopePrediction> HoroscopePredictions(Time birthTime, EventTag filterTag = EventTag.Empty) =>
            Tools.GetHoroscopePrediction(birthTime, filterTag);

        /// <summary>
        /// Given a birth time will calculate all prediction name's that match for given birth time
        /// example : "Moon House 8", "10th Lord in 8th House"
        /// note : used by AI Chat, when talking to Astro tuned LLM server
        /// </summary>
        public static List<string> HoroscopePredictionNames(Time birthTime) =>
            Tools.GetHoroscopePrediction(birthTime).Select(x => x.Name.ToString()).ToList();
    }
}
