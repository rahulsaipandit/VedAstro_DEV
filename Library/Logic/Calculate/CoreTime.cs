using System;
using SwissEphNet;

namespace VedAstro.Library
{
    /// <summary>
    /// Foundational time, longitude & ephemeris primitives that the rest of the
    /// Calculate facade (Core.cs, Vargas.cs, Muhurtha.cs, Tools.cs) depends on.
    ///
    /// NOTE: this file was reconstructed from scratch. The original implementation
    /// was never committed to git (only its auto-generated API doc metadata in
    /// OpenAPIStaticTable.cs survived) so these bodies were rewritten using that
    /// metadata plus the existing Swiss Ephemeris call patterns already present
    /// in Core.cs/Tools.cs as a guide.
    /// </summary>
    public partial class Calculate
    {
        /// <summary>
        /// Sidereal mode (ayanamsa) used for all sidereal (Nirayana) calculations.
        /// Lahiri is the standard ayanamsa used in mainstream Vedic astrology.
        /// </summary>
        public static int Ayanamsa = SwissEph.SE_SIDM_LAHIRI;

        /// <summary>
        /// Whether to use the Mean lunar node (default, classical) instead of the
        /// osculating True node for Rahu/Ketu.
        /// </summary>
        public static bool UseMeanRahuKetu = true;

        /// <summary>
        /// Alias of TimeToJulianUniversalTime, kept for callers expecting this name.
        /// </summary>
        public static double TimeToJulianDay(Time time) => TimeToJulianUniversalTime(time);

        /// <summary>
        /// The distance between the Hindu First Point and the Vernal Equinox measured at an epoch,
        /// as an Angle (see GetAyanamsaDegrees for the raw double).
        /// </summary>
        public static Angle AyanamsaDegree(Time time) => Angle.FromDegrees(GetAyanamsaDegrees(time));

        /// <summary>
        /// True obliquity of the Ecliptic, includes nutation.
        /// </summary>
        public static double EclipticObliquity(Time time)
        {
            var julDayUt = TimeToJulianUniversalTime(time);

            using var swissEph = new SwissEph();
            double[] x = new double[6];
            string serr = "";
            swissEph.swe_calc(julDayUt, SwissEph.SE_ECL_NUT, 0, x, ref serr);

            return x[0];
        }

        /// <summary>
        /// Nirayana (sidereal) Constellation of all 9 planets.
        /// </summary>
        public static System.Collections.Generic.Dictionary<PlanetName, Constellation> AllPlanetConstellation(Time time)
        {
            var result = new System.Collections.Generic.Dictionary<PlanetName, Constellation>();

            foreach (var planet in PlanetName.All9Planets)
            {
                result[planet] = ConstellationAtLongitude(PlanetNirayanaLongitude(planet, time));
            }

            return result;
        }

        /// <summary>
        /// Converts standard time to Julian Universal Time (UT), consumed by Swiss Ephemeris
        /// </summary>
        public static double TimeToJulianUniversalTime(Time time)
        {
            var utc = time.GetStdDateTimeOffset().UtcDateTime;
            double hour = utc.Hour + (utc.Minute / 60.0) + (utc.Second / 3600.0);

            using var swissEph = new SwissEph();
            return swissEph.swe_julday(utc.Year, utc.Month, utc.Day, hour, SwissEph.SE_GREG_CAL);
        }

        /// <summary>
        /// Gets the ephemeris time (ET/TT) that is consumed by Swiss Ephemeris.
        /// Converts normal time to Ephemeris time shown as a Julian day number.
        /// </summary>
        public static double TimeToJulianEphemerisTime(Time time)
        {
            var julDayUt = TimeToJulianUniversalTime(time);

            using var swissEph = new SwissEph();
            var deltaT = swissEph.swe_deltat(julDayUt);

            return julDayUt + deltaT;
        }

        /// <summary>
        /// Gets Local Mean Time (LMT) at Greenwich (UTC) in Julian days, based on the inputted time.
        /// Note: at Greenwich (0° longitude) LMT is by definition equal to UT.
        /// </summary>
        public static double GreenwichLmtInJulianDays(Time time) => TimeToJulianUniversalTime(time);

        /// <summary>
        /// Gets Greenwich time in normal format from Julian days at Greenwich.
        /// Note: inputted time is Julian days at Greenwich, caller's responsibility to ensure that.
        /// </summary>
        public static DateTimeOffset GreenwichTimeFromJulianDays(double julianTime)
        {
            using var swissEph = new SwissEph();

            int year = 0, month = 0, day = 0;
            double hour = 0;
            swissEph.swe_revjul(julianTime, SwissEph.SE_GREG_CAL, ref year, ref month, ref day, ref hour);

            var wholeHour = (int)Math.Floor(hour);
            var remMinutes = (hour - wholeHour) * 60;
            var wholeMinute = (int)Math.Floor(remMinutes);
            var wholeSecond = (int)Math.Round((remMinutes - wholeMinute) * 60);

            var utcDateTime = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc)
                .AddHours(wholeHour).AddMinutes(wholeMinute).AddSeconds(wholeSecond);

            return new DateTimeOffset(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Unspecified), TimeSpan.Zero);
        }

        /// <summary>
        /// Convert longitude to LMT offset. Input longitude range -180 to 180.
        /// 15 degrees of longitude equals 1 hour of time offset from Greenwich.
        /// </summary>
        public static TimeSpan LongitudeToLMTOffset(double longitudeDeg) => TimeSpan.FromHours(longitudeDeg / 15.0);

        /// <summary>
        /// Converts a time offset back to longitude. Reverse of LongitudeToLMTOffset.
        /// Exp: 5h. 10m. 20s. E. Long. to 77°35' E. Long
        /// </summary>
        public static double TimeOffsetToLongitude(TimeSpan time) => time.TotalHours * 15.0;

        /// <summary>
        /// Convert Local Mean Time (LMT) to Standard Time (STD)
        /// API URL: ../LmtToStd/Time/05:45/03/05/1932/Longitude/75/STDOffset/05:30
        /// </summary>
        public static DateTimeOffset LmtToStd(LocalMeanTime lmtDateTime, TimeSpan stdOffset)
        {
            var lmtOffset = LongitudeToLMTOffset(lmtDateTime.Longitude);
            var lmtTime = new DateTimeOffset(lmtDateTime.Date, lmtOffset);
            return lmtTime.ToOffset(stdOffset);
        }

        /// <summary>Overload that tags an already-parsed LMT wall-clock DateTime with its LMT offset directly.</summary>
        public static DateTimeOffset LmtToStd(DateTimeOffset lmtDateTime, TimeSpan lmtOffset) =>
            new(lmtDateTime.DateTime, lmtOffset);

        /// <summary>
        /// Shows local apparent (true solar) time from Swiss Eph.
        /// Apparent time = Local Mean Time + Equation of Time.
        /// </summary>
        public static DateTime LocalApparentTime(Time time)
        {
            var julDayUt = TimeToJulianUniversalTime(time);

            using var swissEph = new SwissEph();
            double timeEquation = 0;
            string serr = "";
            swissEph.swe_time_equ(julDayUt, out timeEquation, ref serr);

            var lmt = time.GetLmtDateTimeOffset();
            return lmt.DateTime.AddDays(timeEquation);
        }

        /// <summary>
        /// Get fixed (tropical) longitude used in western systems, connects SwissEph Library with VedAstro.
        /// NOTE: This method connects SwissEph Library with VedAstro Library.
        /// </summary>
        public static Angle PlanetSayanaLongitude(PlanetName planetName, Time time)
        {
            //Upagraha (shadow/calculated) points are not real bodies in Swiss Ephemeris,
            //their longitude is derived instead, already in sidereal (Nirayana) terms
            if (IsUpagraha(planetName))
            {
                throw new Exception($"{planetName} is a calculated (Upagraha) point, it has no Sayana (tropical) longitude. Use PlanetNirayanaLongitude instead.");
            }

            var swePlanet = Tools.VedAstroToSwissEph(planetName);
            var result = Tools.ephemeris_swe_calc(time, swePlanet);
            double longitude = result.Longitude;

            //Ketu is always exactly opposite (180°) of Rahu
            if (planetName == PlanetName.Ketu) { longitude = (longitude + 180.0) % 360.0; }

            return Angle.FromDegrees(longitude).Normalize360();
        }

        /// <summary>
        /// Planet longitude that has been corrected with Ayanamsa. Gets planet longitude used in Vedic astrology.
        /// Nirayana Longitude = Sayana Longitude corrected to Ayanamsa.
        /// Number from 0 to 360 represents the degrees in the zodiac as viewed from earth.
        /// Note: Since Nirayana is corrected, in actuality 0 degrees will start at Taurus, not Aries.
        /// </summary>
        public static Angle PlanetNirayanaLongitude(PlanetName planetName, Time time)
        {
            //Upagrahas (shadow points) are calculated directly from Sun/other data, already sidereal
            if (IsUpagraha(planetName))
            {
                return UpagrahaNirayanaLongitude(planetName, time);
            }

            var sayanaLongitude = PlanetSayanaLongitude(planetName, time);
            var ayanamsaDeg = GetAyanamsaDegrees(time);

            var nirayana = (sayanaLongitude.TotalDegrees - ayanamsaDeg + 360.0) % 360.0;

            return Angle.FromDegrees(nirayana);
        }

        /// <summary>
        /// Gets the ayanamsa (precession correction) in degrees at a given time, using the configured sidereal mode.
        /// </summary>
        public static double GetAyanamsaDegrees(Time time)
        {
            var julDayUt = TimeToJulianUniversalTime(time);

            using var swissEph = new SwissEph();
            swissEph.swe_set_sid_mode(Ayanamsa, 0, 0);

            return swissEph.swe_get_ayanamsa_ut(julDayUt);
        }

        private static bool IsUpagraha(PlanetName planetName) =>
            planetName == PlanetName.Dhuma || planetName == PlanetName.Vyatipaata ||
            planetName == PlanetName.Parivesha || planetName == PlanetName.Indrachaapa ||
            planetName == PlanetName.Upaketu || planetName == PlanetName.Kaala ||
            planetName == PlanetName.Mrityu || planetName == PlanetName.Arthaprahaara ||
            planetName == PlanetName.Yamaghantaka || planetName == PlanetName.Gulika ||
            planetName == PlanetName.Maandi;

        /// <summary>
        /// Dispatches to the correct Upagraha (shadow point) longitude calculator.
        /// NOTE: Kaala/Mrityu/Arthaprahaara/Yamaghantaka/Gulika/Maandi (the "Kalavela" Upagrahas)
        /// use a simplified day/night-part model here. This is a best-effort reconstruction and
        /// should be reviewed against a classical reference (e.g. BPHS) before being relied upon
        /// for precise predictive work.
        /// </summary>
        private static Angle UpagrahaNirayanaLongitude(PlanetName planetName, Time time)
        {
            if (planetName == PlanetName.Dhuma) { return DhumaLongitude(time); }
            if (planetName == PlanetName.Vyatipaata) { return VyatipaataLongitude(time); }
            if (planetName == PlanetName.Parivesha) { return PariveshaLongitude(time); }
            if (planetName == PlanetName.Indrachaapa) { return IndrachaapaLongitude(time); }
            if (planetName == PlanetName.Upaketu) { return UpaketuLongitude(time); }
            if (planetName == PlanetName.Kaala) { return KaalaVelaLongitude(time, PlanetName.Sun, atMiddle: true); }
            if (planetName == PlanetName.Mrityu) { return KaalaVelaLongitude(time, PlanetName.Mars, atMiddle: true); }
            if (planetName == PlanetName.Arthaprahaara) { return KaalaVelaLongitude(time, PlanetName.Mercury, atMiddle: true); }
            if (planetName == PlanetName.Yamaghantaka) { return KaalaVelaLongitude(time, PlanetName.Jupiter, atMiddle: true); }
            if (planetName == PlanetName.Gulika) { return KaalaVelaLongitude(time, PlanetName.Saturn, atMiddle: true); }
            if (planetName == PlanetName.Maandi) { return KaalaVelaLongitude(time, PlanetName.Saturn, atMiddle: false); }

            throw new Exception($"{planetName} is not a supported Upagraha.");
        }

        /// <summary>Dhuma : Sun's longitude + 133°20'</summary>
        public static Angle DhumaLongitude(Time time)
        {
            var sunLongitude = PlanetSayanaLongitude(PlanetName.Sun, time).TotalDegrees - GetAyanamsaDegrees(time);
            sunLongitude = (sunLongitude + 360.0) % 360.0;
            return Angle.FromDegrees((sunLongitude + 133.0 + (20.0 / 60.0)) % 360.0);
        }

        /// <summary>Vyatipaata : 360° - Dhuma's longitude</summary>
        public static Angle VyatipaataLongitude(Time time) => Angle.FromDegrees((360.0 - DhumaLongitude(time).TotalDegrees + 360.0) % 360.0);

        /// <summary>Parivesha : Vyatipaata's longitude + 180°</summary>
        public static Angle PariveshaLongitude(Time time) => Angle.FromDegrees((VyatipaataLongitude(time).TotalDegrees + 180.0) % 360.0);

        /// <summary>Indrachaapa : 360° - Parivesha's longitude</summary>
        public static Angle IndrachaapaLongitude(Time time) => Angle.FromDegrees((360.0 - PariveshaLongitude(time).TotalDegrees + 360.0) % 360.0);

        /// <summary>Upaketu : Indrachaapa's longitude + 16°40'</summary>
        public static Angle UpaketuLongitude(Time time) => Angle.FromDegrees((IndrachaapaLongitude(time).TotalDegrees + 16.0 + (40.0 / 60.0)) % 360.0);

        /// <summary>
        /// Simplified "Kalavela" Upagraha longitude: divides the day (sunrise-sunset) or night
        /// (sunset-sunrise) into 8 parts ruled in weekday-lord order starting from the day's lord,
        /// and returns the Lagna (ascendant) rising at the middle (or beginning) of the given
        /// planet's part.
        /// </summary>
        private static Angle KaalaVelaLongitude(Time time, PlanetName partLord, bool atMiddle)
        {
            //classical order of the 8 daytime/nighttime part-lords, cycling from the weekday lord
            var order = new[]
            {
                PlanetName.Saturn, PlanetName.Jupiter, PlanetName.Mars, PlanetName.Sun,
                PlanetName.Venus, PlanetName.Mercury, PlanetName.Moon
            };

            var sunrise = SunriseTime(time);
            var sunset = SunsetTime(time);
            var isDayBirth = time.GetStdDateTimeOffset() >= sunrise.GetStdDateTimeOffset() &&
                              time.GetStdDateTimeOffset() < sunset.GetStdDateTimeOffset();

            var dayLord = WeekdayLord(sunrise);
            var startIndex = Array.IndexOf(order, dayLord);
            if (startIndex < 0) { startIndex = 0; }

            //build the 8-part rotation starting from the day lord
            var partLords = new PlanetName[8];
            for (int i = 0; i < 8; i++) { partLords[i] = order[(startIndex + i) % order.Length]; }

            var partIndex = Array.IndexOf(partLords, partLord);
            if (partIndex < 0) { partIndex = 0; }

            var spanStart = isDayBirth ? sunrise.GetStdDateTimeOffset() : sunset.GetStdDateTimeOffset();
            var spanEnd = isDayBirth ? sunset.GetStdDateTimeOffset() : sunrise.GetStdDateTimeOffset().AddDays(1);
            var partLength = (spanEnd - spanStart) / 8.0;

            var partStart = spanStart + (partLength * partIndex);
            var targetInstant = atMiddle ? partStart + (partLength / 2.0) : partStart;

            var targetTime = new Time(targetInstant, time.GetGeoLocation());

            //Lagna (ascendant) rising at that instant, in Nirayana longitude
            return AllHouseLongitudes(targetTime)[0].GetMiddleLongitude();
        }

        /// <summary>Gets the planetary lord of a given weekday, based on the day's sunrise.</summary>
        private static PlanetName WeekdayLord(Time sunriseTime)
        {
            var dayOfWeek = sunriseTime.GetStdDateTimeOffset().DayOfWeek;
            return dayOfWeek switch
            {
                System.DayOfWeek.Sunday => PlanetName.Sun,
                System.DayOfWeek.Monday => PlanetName.Moon,
                System.DayOfWeek.Tuesday => PlanetName.Mars,
                System.DayOfWeek.Wednesday => PlanetName.Mercury,
                System.DayOfWeek.Thursday => PlanetName.Jupiter,
                System.DayOfWeek.Friday => PlanetName.Venus,
                System.DayOfWeek.Saturday => PlanetName.Saturn,
                _ => PlanetName.Sun
            };
        }

        /// <summary>
        /// Divides a planet/house's Rasi (D1) longitude into a divisional (varga) chart's degree,
        /// per the given division number (e.g. 9 for Navamsha). Used by the Vargas calculator.
        /// </summary>
        public static Angle DivisionalLongitude(double totalDegrees, int divisionalNo)
        {
            //length in degrees of a single division of the current sign (30° / divisionalNo)
            var divisionSizeInSign = 30.0 / divisionalNo;

            //which division (0-based) within the current sign the degree falls into
            var divisionIndex = (int)(totalDegrees / divisionSizeInSign);

            //degrees traversed into that specific division
            var degreesIntoDivision = totalDegrees - (divisionIndex * divisionSizeInSign);

            //re-projected across a full 30° sign for the divisional chart's own sign
            var projectedDegrees = (degreesIntoDivision / divisionSizeInSign) * 30.0;

            return Angle.FromDegrees(projectedDegrees % 30.0);
        }

        /// <summary>Converts Planet Longitude to Zodiac Sign equivalent.</summary>
        public static ZodiacSign ZodiacSignAtLongitude(Angle longitude)
        {
            var normalized = longitude.TotalDegrees % 360.0;
            if (normalized < 0) { normalized += 360.0; }

            var signIndex = (int)(normalized / 30.0);
            var degreesInSign = normalized - (signIndex * 30.0);

            var signName = ZodiacSign.All12ZodiacNames[signIndex];

            return new ZodiacSign(signName, Angle.FromDegrees(degreesInSign));
        }

        /// <summary>
        /// Converts Planet Longitude to Constellation (Nakshatra) equivalent.
        /// Gets info about the constellation at a given longitude: Constellation Name, Quarter, Degrees in constellation, etc.
        /// </summary>
        public static Constellation ConstellationAtLongitude(Angle planetLongitude)
        {
            const double constellationSize = 360.0 / 27.0; //13°20'
            const double quarterSize = constellationSize / 4.0; //3°20'

            var normalized = planetLongitude.TotalDegrees % 360.0;
            if (normalized < 0) { normalized += 360.0; }

            var constellationIndex = (int)(normalized / constellationSize); //0-based
            var degreesIntoConstellation = normalized - (constellationIndex * constellationSize);
            var quarter = (int)(degreesIntoConstellation / quarterSize) + 1; //1-based, 1 to 4

            //constellation number is 1-based to match ConstellationName enum ordering
            return new Constellation(constellationIndex + 1, quarter, Angle.FromDegrees(degreesIntoConstellation));
        }
    }
}
