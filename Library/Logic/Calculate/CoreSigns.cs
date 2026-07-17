using System.Collections.Generic;

namespace VedAstro.Library
{
    /// <summary>
    /// Sign, house & divisional-chart (varga) facade methods. Reconstructed the same way
    /// as CoreTime.cs - see that file's header note.
    /// </summary>
    public partial class Calculate
    {
        //█░█ █▀█ █░█ █▀ █▀▀   █░█ █▀▀ █░░ █▀█ █▀▀ █▀█ █▀
        //█▀█ █▄█ █▄█ ▄█ ██▄   █▀█ ██▄ █▄▄ █▀▀ ██▄ █▀▄ ▄█

        /// <summary>Gets only the zodiac sign name at the middle longitude of the house.</summary>
        public static ZodiacName HouseSignName(HouseName houseNumber, Time time) => HouseZodiacSign(houseNumber, time).GetSignName();

        /// <summary>Gets the zodiac sign at the middle longitude of the house, with degrees data (Bhava Chalit).</summary>
        public static ZodiacSign HouseZodiacSign(HouseName houseNumber, Time time) => ZodiacSignAtLongitude(HouseLongitude(houseNumber, time).GetMiddleLongitude());

        /// <summary>Gets zodiac sign for a given house counted from Lagna (whole-sign house system).</summary>
        public static ZodiacSign HouseRasiSign(HouseName houseNumber, Time time)
        {
            var signName = SignCountedFromLagnaSign((int)houseNumber, time);

            //preserve the lagna's degree offset within the sign, since whole-sign houses shift by a full sign each time
            var lagnaDegreesInSign = HouseZodiacSign(HouseName.House1, time).GetDegreesInSign();

            return new ZodiacSign(signName, lagnaDegreesInSign);
        }

        /// <summary>Gets the zodiac sign at middle longitude of the house, for all 12 houses.</summary>
        public static Dictionary<HouseName, ZodiacSign> AllHouseZodiacSigns(Time time)
        {
            var result = new Dictionary<HouseName, ZodiacSign>();
            foreach (var house in House.AllHouses) { result[house] = HouseZodiacSign(house, time); }
            return result;
        }

        /// <summary>Gets the Rasi (whole-sign) sign for all 12 houses.</summary>
        public static Dictionary<HouseName, ZodiacSign> AllHouseRasiSigns(Time time)
        {
            var result = new Dictionary<HouseName, ZodiacSign>();
            foreach (var house in House.AllHouses) { result[house] = HouseRasiSign(house, time); }
            return result;
        }

        /// <summary>Gets planet which is the lord (ruler) of a given zodiac sign.</summary>
        public static PlanetName LordOfZodiacSign(ZodiacName signName)
        {
            switch (signName)
            {
                case ZodiacName.Aries: return PlanetName.Mars;
                case ZodiacName.Taurus: return PlanetName.Venus;
                case ZodiacName.Gemini: return PlanetName.Mercury;
                case ZodiacName.Cancer: return PlanetName.Moon;
                case ZodiacName.Leo: return PlanetName.Sun;
                case ZodiacName.Virgo: return PlanetName.Mercury;
                case ZodiacName.Libra: return PlanetName.Venus;
                case ZodiacName.Scorpio: return PlanetName.Mars;
                case ZodiacName.Sagittarius: return PlanetName.Jupiter;
                case ZodiacName.Capricorn: return PlanetName.Saturn;
                case ZodiacName.Aquarius: return PlanetName.Saturn;
                case ZodiacName.Pisces: return PlanetName.Jupiter;
                default: return PlanetName.Empty;
            }
        }

        /// <summary>Given a planet name, returns list of signs that the planet rules.</summary>
        public static List<ZodiacName> ZodiacSignsOwnedByPlanet(PlanetName planetName)
        {
            var result = new List<ZodiacName>();
            foreach (var sign in ZodiacSign.All12ZodiacNames)
            {
                if (LordOfZodiacSign(sign) == planetName) { result.Add(sign); }
            }
            return result;
        }

        /// <summary>Gets next zodiac sign after input sign.</summary>
        public static ZodiacName NextZodiacSign(ZodiacName inputSign)
        {
            var index = ZodiacSign.All12ZodiacNames.IndexOf(inputSign);
            var nextIndex = (index + 1) % ZodiacSign.All12ZodiacNames.Count;
            return ZodiacSign.All12ZodiacNames[nextIndex];
        }

        /// <summary>Gets next house number after input house number, goes to 1 after 12.</summary>
        public static int NextHouseNumber(int inputHouseNumber)
        {
            var next = inputHouseNumber + 1;
            return next > 12 ? 1 : next;
        }

        /// <summary>Fixed signs: Taurus, Leo, Scorpio, Aquarius.</summary>
        public static bool IsFixedSign(ZodiacName sunSign) => sunSign is ZodiacName.Taurus or ZodiacName.Leo or ZodiacName.Scorpio or ZodiacName.Aquarius;

        /// <summary>Movable (Cardinal) signs: Aries, Cancer, Libra, Capricorn.</summary>
        public static bool IsMovableSign(ZodiacName sunSign) => sunSign is ZodiacName.Aries or ZodiacName.Cancer or ZodiacName.Libra or ZodiacName.Capricorn;

        /// <summary>Common (Mutable) signs: Gemini, Virgo, Sagittarius, Pisces.</summary>
        public static bool IsCommonSign(ZodiacName sunSign) => sunSign is ZodiacName.Gemini or ZodiacName.Virgo or ZodiacName.Sagittarius or ZodiacName.Pisces;

        /// <summary>
        /// Gets the junction point (sandhi) between 2 consecutive houses, where one house begins and the other ends.
        /// This is the midpoint angle between the end of the previous house and the start of the next.
        /// </summary>
        public static Angle HouseJunctionPoint(Angle previousHouse, Angle nextHouse)
        {
            //account for wrap-around past 360°
            var diff = nextHouse.TotalDegrees - previousHouse.TotalDegrees;
            if (diff < 0) { diff += 360.0; }

            var junction = (previousHouse.TotalDegrees + (diff / 2.0)) % 360.0;

            return Angle.FromDegrees(junction);
        }

        /// <summary>
        /// Gets the exact sign & degree where a planet is Exalted (maximum dignity).
        /// Ref: Astrology for Beginners, pg. 12. Rahu/Ketu values follow commonly used convention.
        /// </summary>
        public static ZodiacSign PlanetExaltationPoint(PlanetName planetName) => ZodiacSignAtLongitude(PlanetExaltationPointAngle(planetName));

        /// <summary>Absolute 0-360° longitude form of PlanetExaltationPoint, for internal angle math.</summary>
        public static Angle PlanetExaltationPointAngle(PlanetName planetName)
        {
            //(sign, degree-in-sign) of exaltation, converted to absolute 0-360 longitude
            (ZodiacName sign, double degree) point = planetName.Name switch
            {
                PlanetName.PlanetNameEnum.Sun => (ZodiacName.Aries, 10),
                PlanetName.PlanetNameEnum.Moon => (ZodiacName.Taurus, 3),
                PlanetName.PlanetNameEnum.Mars => (ZodiacName.Capricorn, 28),
                PlanetName.PlanetNameEnum.Mercury => (ZodiacName.Virgo, 15),
                PlanetName.PlanetNameEnum.Jupiter => (ZodiacName.Cancer, 5),
                PlanetName.PlanetNameEnum.Venus => (ZodiacName.Pisces, 27),
                PlanetName.PlanetNameEnum.Saturn => (ZodiacName.Libra, 20),
                PlanetName.PlanetNameEnum.Rahu => (ZodiacName.Taurus, 20),
                PlanetName.PlanetNameEnum.Ketu => (ZodiacName.Scorpio, 20),
                //Upagrahas: no exact degree of exaltation defined, whole sign counted, set at degree 1
                _ => (ZodiacName.Aries, 1)
            };

            var signIndex = ZodiacSign.All12ZodiacNames.IndexOf(point.sign);
            return Angle.FromDegrees((signIndex * 30.0) + point.degree);
        }

        /// <summary>
        /// Gets the exact sign & degree where a planet is Debilitated (minimum dignity),
        /// which is always 180° from its exaltation point.
        /// Ref: Astrology for Beginners, pg. 11.
        /// </summary>
        public static ZodiacSign PlanetDebilitationPoint(PlanetName planetName) => ZodiacSignAtLongitude(PlanetDebilitationPointAngle(planetName));

        /// <summary>Absolute 0-360° longitude form of PlanetDebilitationPoint, for internal angle math.</summary>
        public static Angle PlanetDebilitationPointAngle(PlanetName planetName)
        {
            var exaltation = PlanetExaltationPointAngle(planetName);
            return Angle.FromDegrees((exaltation.TotalDegrees + 180.0) % 360.0);
        }


        //█▀▄ █ █░█ █ █▀ █ █▀█ █▄░█ ▄▀█ █░░   █▀▀ █░█ ▄▀█ █▀█ ▀█▀ █▀
        //█▄▀ █ ▀▄▀ █ ▄█ █ █▄█ █░▀█ █▀█ █▄▄   █▄▄ █▀█ █▀█ █▀▄ ░█░ ▄█

        /// <summary>D1 : Get zodiac sign planet is in (Rasi chart).</summary>
        public static ZodiacSign PlanetRasiD1Sign(PlanetName planetName, Time time) => ZodiacSignAtLongitude(PlanetNirayanaLongitude(planetName, time));

        /// <summary>Alias of PlanetRasiD1Sign, kept for callers expecting this name.</summary>
        public static ZodiacSign PlanetZodiacSign(PlanetName planetName, Time time) => PlanetRasiD1Sign(planetName, time);

        /// <summary>D9 : Navamsha sign of a planet.</summary>
        public static ZodiacSign PlanetNavamshaD9Sign(PlanetName planetName, Time time) =>
            Vargas.VargasCoreCalculator(PlanetRasiD1Sign(planetName, time), Vargas.NavamshaTable, 9);

        /// <summary>Gets Navamsa (D9) sign at a house's middle point.</summary>
        public static ZodiacSign HouseNavamshaD9Sign(HouseName houseNumber, Time time) =>
            Vargas.VargasCoreCalculator(HouseZodiacSign(houseNumber, time), Vargas.NavamshaTable, 9);

        private static ZodiacSign HouseVargaSign(HouseName houseNumber, Time time, Dictionary<ZodiacName, Dictionary<DegreeRange, ZodiacName>> table, int divisionNo) =>
            Vargas.VargasCoreCalculator(HouseZodiacSign(houseNumber, time), table, divisionNo);

        private static ZodiacSign PlanetVargaSign(PlanetName planetName, Time time, Dictionary<ZodiacName, Dictionary<DegreeRange, ZodiacName>> table, int divisionNo) =>
            Vargas.VargasCoreCalculator(PlanetRasiD1Sign(planetName, time), table, divisionNo);

        public static ZodiacSign HouseHoraD2Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.HoraTable, 2);
        public static ZodiacSign HouseDrekkanaD3Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.DrekkanaTable, 3);
        public static ZodiacSign HouseChaturthamshaD4Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.ChaturthamshaTable, 4);
        public static ZodiacSign HouseSaptamshaD7Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.SaptamshaTable, 7);
        public static ZodiacSign HouseDashamamshaD10Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.DashamamshaTable, 10);
        public static ZodiacSign HouseDwadashamshaD12Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.DwadashamshaTable, 12);
        public static ZodiacSign HouseShodashamshaD16Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.ShodashamshaTable, 16);
        public static ZodiacSign HouseVimshamshaD20Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.VimshamshaTable, 20);
        public static ZodiacSign HouseChaturvimshamshaD24Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.ChaturvimshamshaTable, 24);
        public static ZodiacSign HouseBhamshaD27Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.BhamshaTable, 27);
        public static ZodiacSign HouseTrimshamshaD30Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.TrimshamshaTable, 30);
        public static ZodiacSign HouseKhavedamshaD40Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.KhavedamshaTable, 40);
        public static ZodiacSign HouseAkshavedamshaD45Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.AkshavedamshaTable, 45);
        public static ZodiacSign HouseShashtyamshaD60Sign(HouseName houseNumber, Time time) => HouseVargaSign(houseNumber, time, Vargas.ShashtyamshaTable, 60);

        public static ZodiacSign PlanetHoraD2Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.HoraTable, 2);
        public static ZodiacSign PlanetDrekkanaD3Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.DrekkanaTable, 3);
        public static ZodiacSign PlanetChaturthamshaD4Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.ChaturthamshaTable, 4);
        public static ZodiacSign PlanetSaptamshaD7Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.SaptamshaTable, 7);
        public static ZodiacSign PlanetDashamamshaD10Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.DashamamshaTable, 10);
        public static ZodiacSign PlanetDwadashamshaD12Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.DwadashamshaTable, 12);
        public static ZodiacSign PlanetShodashamshaD16Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.ShodashamshaTable, 16);
        public static ZodiacSign PlanetVimshamshaD20Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.VimshamshaTable, 20);
        public static ZodiacSign PlanetChaturvimshamshaD24Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.ChaturvimshamshaTable, 24);
        public static ZodiacSign PlanetBhamshaD27Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.BhamshaTable, 27);
        public static ZodiacSign PlanetTrimshamshaD30Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.TrimshamshaTable, 30);
        public static ZodiacSign PlanetKhavedamshaD40Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.KhavedamshaTable, 40);
        public static ZodiacSign PlanetAkshavedamshaD45Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.AkshavedamshaTable, 45);
        public static ZodiacSign PlanetShashtyamshaD60Sign(PlanetName planetName, Time time) => PlanetVargaSign(planetName, time, Vargas.ShashtyamshaTable, 60);

        //convenience aliases matching the shorter public API names used elsewhere
        public static ZodiacSign AllHouseHoraSign_For(HouseName h, Time t) => HouseHoraD2Sign(h, t);

        public static Dictionary<HouseName, ZodiacSign> AllHouseHoraSign(Time time) => AllHouseVarga(time, HouseHoraD2Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseDrekkanaSign(Time time) => AllHouseVarga(time, HouseDrekkanaD3Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseChaturthamsaSign(Time time) => AllHouseVarga(time, HouseChaturthamshaD4Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseSaptamshaSign(Time time) => AllHouseVarga(time, HouseSaptamshaD7Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseNavamshaSign(Time time) => AllHouseVarga(time, HouseNavamshaD9Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseDashamamshaSign(Time time) => AllHouseVarga(time, HouseDashamamshaD10Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseDwadashamshaSign(Time time) => AllHouseVarga(time, HouseDwadashamshaD12Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseShodashamshaSign(Time time) => AllHouseVarga(time, HouseShodashamshaD16Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseVimshamshaSign(Time time) => AllHouseVarga(time, HouseVimshamshaD20Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseChaturvimshamshaSign(Time time) => AllHouseVarga(time, HouseChaturvimshamshaD24Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseBhamshaSign(Time time) => AllHouseVarga(time, HouseBhamshaD27Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseTrimshamshaSign(Time time) => AllHouseVarga(time, HouseTrimshamshaD30Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseKhavedamshaSign(Time time) => AllHouseVarga(time, HouseKhavedamshaD40Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseAkshavedamshaSign(Time time) => AllHouseVarga(time, HouseAkshavedamshaD45Sign);
        public static Dictionary<HouseName, ZodiacSign> AllHouseShashtyamshaSign(Time time) => AllHouseVarga(time, HouseShashtyamshaD60Sign);

        public static Dictionary<PlanetName, ZodiacSign> AllPlanetRasiSigns(Time time) => AllPlanetVarga(time, PlanetRasiD1Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetHoraSign(Time time) => AllPlanetVarga(time, PlanetHoraD2Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetDrekkanaSign(Time time) => AllPlanetVarga(time, PlanetDrekkanaD3Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetChaturthamsaSign(Time time) => AllPlanetVarga(time, PlanetChaturthamshaD4Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetSaptamshaSign(Time time) => AllPlanetVarga(time, PlanetSaptamshaD7Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetNavamshaSign(Time time) => AllPlanetVarga(time, PlanetNavamshaD9Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetDashamamshaSign(Time time) => AllPlanetVarga(time, PlanetDashamamshaD10Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetDwadashamshaSign(Time time) => AllPlanetVarga(time, PlanetDwadashamshaD12Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetShodashamshaSign(Time time) => AllPlanetVarga(time, PlanetShodashamshaD16Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetVimshamshaSign(Time time) => AllPlanetVarga(time, PlanetVimshamshaD20Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetChaturvimshamshaSign(Time time) => AllPlanetVarga(time, PlanetChaturvimshamshaD24Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetBhamshaSign(Time time) => AllPlanetVarga(time, PlanetBhamshaD27Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetTrimshamshaSign(Time time) => AllPlanetVarga(time, PlanetTrimshamshaD30Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetKhavedamshaSign(Time time) => AllPlanetVarga(time, PlanetKhavedamshaD40Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetAkshavedamshaSign(Time time) => AllPlanetVarga(time, PlanetAkshavedamshaD45Sign);
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetShashtyamshaSign(Time time) => AllPlanetVarga(time, PlanetShashtyamshaD60Sign);

        private static Dictionary<HouseName, ZodiacSign> AllHouseVarga(Time time, System.Func<HouseName, Time, ZodiacSign> calculator)
        {
            var result = new Dictionary<HouseName, ZodiacSign>();
            foreach (var house in House.AllHouses) { result[house] = calculator(house, time); }
            return result;
        }

        private static Dictionary<PlanetName, ZodiacSign> AllPlanetVarga(Time time, System.Func<PlanetName, Time, ZodiacSign> calculator)
        {
            var result = new Dictionary<PlanetName, ZodiacSign>();
            foreach (var planet in PlanetName.All9Planets) { result[planet] = calculator(planet, time); }
            return result;
        }

        /// <summary>Gets list of all planets and the zodiac signs they are in, based on house (Bhava) longitudes.</summary>
        public static Dictionary<PlanetName, ZodiacSign> AllPlanetSignsBasedOnHouseLongitudes(Time time)
        {
            var result = new Dictionary<PlanetName, ZodiacSign>();
            foreach (var planet in PlanetName.All9Planets)
            {
                result[planet] = ZodiacSignAtLongitude(PlanetNirayanaLongitude(planet, time));
            }
            return result;
        }
    }
}
