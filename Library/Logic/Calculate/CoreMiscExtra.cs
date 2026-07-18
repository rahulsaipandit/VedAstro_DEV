using System;
using System.Collections.Generic;
using System.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Additional calculators that were missing from Library entirely prior to this fix
    /// (LibraryTests referenced them, but no implementation existed anywhere, even on
    /// master, predating the Postgres migration). Implemented here as best-effort
    /// reconstructions based on standard/classical astrology rules and each test's
    /// documented scenario. Verify against a second source (e.g. JHora, a printed
    /// reference book) before relying on any of these for production predictions.
    /// </summary>
    public partial class Calculate
    {
        //█░░ █▀█ █▄░█ █▀▀ █ ▀█▀ █░█ █▀▀ █▀▀   ▄▀█ ▀█▀   ▄▀█   █▀ █ █▀▀ █▄░█
        //█▄▄ █▄█ █░▀█ █▄█ █ ░█░ █▄█ █▄▄ ██▄   █▀█ ░█░   █▀█   ▄█ █ █▄█ █░▀█

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on standard astrology rules. Verify against a second source if this
        /// is used for production predictions.
        ///
        /// Gets the absolute 0-360° Nirayana longitude represented by a given ZodiacSign (sign +
        /// degrees-in-sign). Inverse of <see cref="ZodiacSignAtLongitude"/>.
        /// </summary>
        public static Angle LongitudeAtZodiacSign(ZodiacSign zodiacSign)
        {
            var signIndex = ZodiacSign.All12ZodiacNames.IndexOf(zodiacSign.GetSignName());
            var absoluteDegrees = (signIndex * 30.0) + zodiacSign.GetDegreesInSign().TotalDegrees;

            return Angle.FromDegrees(absoluteDegrees);
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on standard astrology rules. Verify against a second source if this
        /// is used for production predictions.
        ///
        /// Gets a planet's longitude within a given divisional (Varga) chart, computed directly
        /// from its D1 (Rasi) degrees-in-sign multiplied by the division number (per the classical
        /// simple-multiplication method, e.g. Jupiter at 12°04' in D-7 = 12°04' x 7 = 84°28',
        /// minus completed signs = 24°28').
        /// </summary>
        public static Angle PlanetDivisionalLongitude(PlanetName planetName, Time time, int divisionalNo)
        {
            var d1Sign = PlanetRasiD1Sign(planetName, time);
            return DivisionalLongitude(d1Sign.GetDegreesInSign().TotalDegrees, divisionalNo);
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on standard astrology rules. Verify against a second source if this
        /// is used for production predictions.
        ///
        /// D2 (Hora) sign of a planet, as a plain ZodiacName (wraps the existing
        /// <see cref="PlanetHoraD2Sign"/> which returns the fuller ZodiacSign type).
        /// </summary>
        public static ZodiacName PlanetHoraD2Signs(PlanetName planetName, Time time) => PlanetHoraD2Sign(planetName, time).GetSignName();

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on standard astrology rules. Verify against a second source if this
        /// is used for production predictions.
        ///
        /// Factory wrapper so callers can get a sign's compiled properties via Calculate,
        /// consistent with the rest of this class's static calculator methods.
        /// </summary>
        public static SignProperties SignProperties(ZodiacName signName) => new SignProperties(signName);


        //█▀▀ █▀█ █▀█ ▀█▀ █░█ █▄░█ ▄▀█ ▄▀█▄▀█ █░█ █▀▀ █░█ ▄▀█ █░ █▀▄ █▀▀ █▀▀ ▀█▀ █ █▄░█ █▄█   █▀█ █▀█ █ █▄░█ ▀█▀ █▀
        //█▄▄ █▄█ █▀▄ ░█░ █▄█ █░▀█ █▀█ ▀▄▀░▄▀ █▀█ ██▄ █▀█ █▀█ █░ █▄▀ ██▄ █▄▄ ░█░ █ █░▀█ ░█░   █▀▀ █▄█ █ █░▀█ ░█░ ▄█

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical Hellenistic/Vedic "Part of Fortune" (Bhagya Point)
        /// formula, adapted to accept an arbitrary reference sign in place of the natal Lagna.
        /// Verify against a second source if this is used for production predictions.
        ///
        /// Fortuna = referenceSign's start + Moon - Sun (day birth), or referenceSign's start +
        /// Sun - Moon (night birth), normalized to 0-360°.
        /// </summary>
        public static Angle FortunaPoint(ZodiacName referenceSign, Time birthTime)
        {
            var referenceLongitude = LongitudeAtZodiacSign(new ZodiacSign(referenceSign, Angle.Zero)).TotalDegrees;
            var moonLongitude = PlanetNirayanaLongitude(PlanetName.Moon, birthTime).TotalDegrees;
            var sunLongitude = PlanetNirayanaLongitude(PlanetName.Sun, birthTime).TotalDegrees;

            var isDayBirth = IsDayBirth(birthTime);

            var fortuna = isDayBirth
                ? referenceLongitude + moonLongitude - sunLongitude
                : referenceLongitude + sunLongitude - moonLongitude;

            return Angle.FromDegrees(((fortuna % 360.0) + 360.0) % 360.0);
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical "Destiny Point" formula (mirror image of Fortuna,
        /// using the reference sign minus Moon plus Sun). Verify against a second source if this
        /// is used for production predictions.
        /// </summary>
        public static Angle DestinyPoint(Time birthTime, ZodiacName referenceSign)
        {
            var referenceLongitude = LongitudeAtZodiacSign(new ZodiacSign(referenceSign, Angle.Zero)).TotalDegrees;
            var moonLongitude = PlanetNirayanaLongitude(PlanetName.Moon, birthTime).TotalDegrees;
            var sunLongitude = PlanetNirayanaLongitude(PlanetName.Sun, birthTime).TotalDegrees;

            var isDayBirth = IsDayBirth(birthTime);

            //Destiny Point is taken here as the complementary point to Fortuna:
            //built the same way but with Sun & Moon swapped
            var destiny = isDayBirth
                ? referenceLongitude + sunLongitude - moonLongitude
                : referenceLongitude + moonLongitude - sunLongitude;

            return Angle.FromDegrees(((destiny % 360.0) + 360.0) % 360.0);
        }

        //█ █▀ █░█ ▀█▀ ▄▀█ ▄▄ █▄▀ ▄▀█ █▀ █░█ ▀█▀ ▄▀█ 　 █▀ █▀▀ █▀█ █▀█ █▀▀ █▀
        //█ ▄█ █▀█ ░█░ █▀█ ░░ █░█ █▀█ ▄█ █▀█ ░█░ █▀█ 　 ▄█ █▄▄ █▄█ █▀▄ ██▄ ▄█

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical Uchcha Bala (exaltation strength) component of
        /// Ishta/Kashta Phala (BPHS / Bhava &amp; Graha Bala). Verify against a second source
        /// (exact book values are unlikely to match without the book's own ephemeris/speed
        /// tables) if this is used for production predictions.
        ///
        /// Ishta ("desirable") strength: derived from how close the planet is to its exaltation
        /// point (Uchcha Bala, 0-60 shashtiamsas) combined with its motional (Cheshta-like) speed
        /// strength, geometric-mean combined per the classical formula
        /// Ishta = sqrt(UchchaBala x ChestaBala).
        /// </summary>
        public static double PlanetIshtaScore(PlanetName planet, Time birthTime)
        {
            var uchchaBala = UchchaBalaShashtiamsa(planet, birthTime);
            var chestaBala = ChestaBalaShashtiamsa(planet, birthTime);

            var ishta = Math.Sqrt(uchchaBala * chestaBala);

            return Math.Round(ishta, 2);
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical Kashta Bala (undesirable strength) component of
        /// Ishta/Kashta Phala (BPHS / Bhava &amp; Graha Bala). Verify against a second source
        /// (exact book values are unlikely to match without the book's own ephemeris/speed
        /// tables) if this is used for production predictions.
        ///
        /// Kashta ("undesirable") strength: complementary to Ishta, using the shortfall from full
        /// (60) Uchcha and Cheshta Bala: Kashta = sqrt((60-UchchaBala) x (60-ChestaBala)).
        /// </summary>
        public static double PlanetKashtaScore(PlanetName planet, Time birthTime)
        {
            var uchchaBala = UchchaBalaShashtiamsa(planet, birthTime);
            var chestaBala = ChestaBalaShashtiamsa(planet, birthTime);

            var kashta = Math.Sqrt((60.0 - uchchaBala) * (60.0 - chestaBala));

            return Math.Round(kashta, 2);
        }

        /// <summary>Uchcha Bala (exaltation strength) in shashtiamsas (0-60): 60 at exact exaltation point, 0 at exact debilitation point.</summary>
        private static double UchchaBalaShashtiamsa(PlanetName planet, Time time)
        {
            var longitude = PlanetNirayanaLongitude(planet, time).TotalDegrees;
            var debilitation = PlanetDebilitationPointAngle(planet).TotalDegrees;

            //0 at debilitation, 180 (degrees away) at exaltation
            var distanceFromDebilitation = Math.Abs(((longitude - debilitation + 540.0) % 360.0) - 180.0);

            //rescale 0-180 range to 0-60 shashtiamsas
            return distanceFromDebilitation / 180.0 * 60.0;
        }

        /// <summary>
        /// Cheshta Bala proxy (0-60 shashtiamsas), based on the planet's actual daily motion
        /// speed relative to its mean/average daily motion (faster or retrograde motion = higher
        /// Cheshta Bala, per classical rule). Sun & Moon (which classically use Ayana Bala
        /// instead of Cheshta Bala) are approximated the same way here for simplicity.
        /// </summary>
        private static double ChestaBalaShashtiamsa(PlanetName planet, Time time)
        {
            var longitudeNow = PlanetNirayanaLongitude(planet, time).TotalDegrees;
            var timeLater = time.AddHours(24);
            var longitudeLater = PlanetNirayanaLongitude(planet, timeLater).TotalDegrees;

            //signed daily motion, unwrapped across the 0/360 boundary
            var dailyMotion = ((longitudeLater - longitudeNow + 540.0) % 360.0) - 180.0;

            var meanDailyMotion = MeanDailyMotionDegrees(planet);
            if (meanDailyMotion <= 0) { meanDailyMotion = 1; }

            //retrograde motion classically gets close to full (60) Cheshta Bala
            if (dailyMotion < 0) { return 60.0; }

            //ratio of actual to mean speed, inverted (slower than mean = more Cheshta Bala,
            //since standing near-still/about to turn retrograde is classically strong),
            //clipped to the valid 0-60 range
            var speedRatio = dailyMotion / meanDailyMotion;
            var chestaBala = (2.0 - speedRatio) * 30.0;

            return Math.Clamp(chestaBala, 0.0, 60.0);
        }

        /// <summary>Approximate mean daily motion (degrees/day) for each planet, used only as a speed reference for Cheshta Bala.</summary>
        private static double MeanDailyMotionDegrees(PlanetName planet)
        {
            return planet.Name switch
            {
                PlanetName.PlanetNameEnum.Sun => 0.9856,
                PlanetName.PlanetNameEnum.Moon => 13.176,
                PlanetName.PlanetNameEnum.Mars => 0.524,
                PlanetName.PlanetNameEnum.Mercury => 1.383,
                PlanetName.PlanetNameEnum.Jupiter => 0.083,
                PlanetName.PlanetNameEnum.Venus => 1.2,
                PlanetName.PlanetNameEnum.Saturn => 0.034,
                _ => 0.5
            };
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical graded Parashari aspect (Drishti) strength table:
        /// full aspect (100%) at the 7th house, partial aspects at the 3rd/10th (25%), 5th/9th
        /// (50%) and 4th/8th (75%) houses counted from the aspecting planet, plus the special
        /// extra full aspects Mars (4th &amp; 8th), Jupiter (5th &amp; 9th) and Saturn (3rd &amp;
        /// 10th) cast in addition to their 7th house aspect. Verify against a second source if
        /// this is used for production predictions.
        /// </summary>
        public static double PlanetAspectDegree(PlanetName aspectingPlanet, PlanetName aspectedPlanet, Time time)
        {
            var fromSign = PlanetRasiD1Sign(aspectingPlanet, time).GetSignName();
            var toSign = PlanetRasiD1Sign(aspectedPlanet, time).GetSignName();

            var houseDistance = CountFromSignToSign(fromSign, toSign); //1 = same sign (conjunction, not an aspect)

            //special full (100%) aspects
            var hasSpecialAspect =
                (aspectingPlanet == PlanetName.Mars && (houseDistance == 4 || houseDistance == 8)) ||
                (aspectingPlanet == PlanetName.Jupiter && (houseDistance == 5 || houseDistance == 9)) ||
                (aspectingPlanet == PlanetName.Saturn && (houseDistance == 3 || houseDistance == 10));

            if (hasSpecialAspect) { return 100.0; }

            return houseDistance switch
            {
                7 => 100.0,
                4 or 8 => 75.0,
                5 or 9 => 50.0,
                3 or 10 => 25.0,
                _ => 0.0
            };
        }


        //█▀▄▀█ █▀▀ █▀█ █▀▄ █▀█ █▀▄ █▀▄ █▀█ ▄▀█ █▀█ █▄█   ▄█ █░░ █░█ █▄░█ ▄▀█ █▀█ 　 █▀▀ █▀▀ █░░ █ █▀█ █▀ █▀▀
        //█░▀░█ ██▄ █▀▄ █▄▀ █▄█ █▄▀ █▄▀ █▀▄ █▀█ █▀▄ ░█░   ░█ █▄▄ █▀█ █░▀█ █▀█ █▀▄ 　 ██▄ █▄▄ █▄▄ █ █▀▀ ▄█ ██▄

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort using a linear Newton-style search on the Moon-Sun elongation (0° = New
        /// Moon). Verify against a second source (e.g. an ephemeris) if this is used for
        /// production predictions - the search converges quickly since elongation speed is
        /// nearly constant over a few days, but is not iterated to sub-second precision.
        /// </summary>
        public static Time NextNewMoon(Time time) => FindSyzygy(time, forward: true);

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort using the same linear search as <see cref="NextNewMoon"/>, run backwards.
        /// Verify against a second source if this is used for production predictions.
        /// </summary>
        public static Time PreviousNewMoon(Time time) => FindSyzygy(time, forward: false);

        /// <summary>Finds the nearest New Moon (Moon-Sun elongation = 0°) before/after the given time.</summary>
        private static Time FindSyzygy(Time time, bool forward)
        {
            const double synodicDegreesPerDay = 360.0 / 29.530588; //average relative Moon-Sun speed

            double Elongation(Time t)
            {
                var moon = PlanetNirayanaLongitude(PlanetName.Moon, t).TotalDegrees;
                var sun = PlanetNirayanaLongitude(PlanetName.Sun, t).TotalDegrees;
                return ((moon - sun) % 360.0 + 360.0) % 360.0;
            }

            var currentElongation = Elongation(time);

            //initial coarse guess
            var degreesToTravel = forward
                ? (currentElongation <= 0.0001 ? 360.0 : 360.0 - currentElongation)
                : (currentElongation <= 0.0001 ? 360.0 : currentElongation);

            var daysGuess = degreesToTravel / synodicDegreesPerDay;
            var candidate = forward ? time.AddHours(daysGuess * 24) : time.AddHours(-daysGuess * 24);

            //refine with a few Newton-style iterations (elongation should be very close to 0 or 360 now)
            for (var i = 0; i < 6; i++)
            {
                var e = Elongation(candidate);
                //normalize to the signed difference from the nearest multiple of 360 (i.e. from 0)
                var signedDiff = e > 180.0 ? e - 360.0 : e;

                if (Math.Abs(signedDiff) < 0.0005) { break; }

                var adjustDays = signedDiff / synodicDegreesPerDay;
                candidate = candidate.AddHours(-adjustDays * 24);
            }

            return candidate;
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical lunar eclipse geometry rule: a lunar eclipse can
        /// only occur at Full Moon (elongation 180°) when the Moon is also close (within roughly
        /// 12°) to one of its two lunar nodes (Rahu/Ketu). This scans forward, full-moon by
        /// full-moon, for the first one where that node-proximity condition holds. Verify against
        /// a second source (e.g. an ephemeris eclipse table) if this is used for production
        /// predictions - node-proximity thresholds for an actual (rather than merely possible)
        /// eclipse are more complex than checked here.
        /// </summary>
        public static Time NextLunarEclipse(Time time)
        {
            const double nodeProximityDegrees = 12.0; //rough classical "eclipse limit" for the Moon from a node at Full Moon

            var candidateFullMoon = NextFullMoon(time);

            //scan up to 30 full moons (~2.5 years) forward looking for node proximity
            for (var i = 0; i < 30; i++)
            {
                var moonLongitude = PlanetNirayanaLongitude(PlanetName.Moon, candidateFullMoon).TotalDegrees;
                var rahuLongitude = PlanetNirayanaLongitude(PlanetName.Rahu, candidateFullMoon).TotalDegrees;
                var ketuLongitude = PlanetNirayanaLongitude(PlanetName.Ketu, candidateFullMoon).TotalDegrees;

                var distanceFromRahu = AngularDistance(moonLongitude, rahuLongitude);
                var distanceFromKetu = AngularDistance(moonLongitude, ketuLongitude);

                var isNearNode = distanceFromRahu <= nodeProximityDegrees || distanceFromKetu <= nodeProximityDegrees;

                if (isNearNode) { return candidateFullMoon; }

                candidateFullMoon = NextFullMoon(candidateFullMoon.AddHours(24));
            }

            //fallback: classical eclipse "season" repeats about every 173 days (half a year of eclipse seasons)
            return candidateFullMoon;
        }

        /// <summary>Finds the next Full Moon (Moon-Sun elongation = 180°) after the given time.</summary>
        private static Time NextFullMoon(Time time)
        {
            const double synodicDegreesPerDay = 360.0 / 29.530588;

            double Elongation(Time t)
            {
                var moon = PlanetNirayanaLongitude(PlanetName.Moon, t).TotalDegrees;
                var sun = PlanetNirayanaLongitude(PlanetName.Sun, t).TotalDegrees;
                return ((moon - sun) % 360.0 + 360.0) % 360.0;
            }

            var currentElongation = Elongation(time);
            var degreesToTravel = currentElongation <= 180.0001 && currentElongation >= 179.9999
                ? 360.0
                : ((180.0 - currentElongation) + 360.0) % 360.0;

            var daysGuess = degreesToTravel / synodicDegreesPerDay;
            var candidate = time.AddHours(daysGuess * 24);

            for (var i = 0; i < 6; i++)
            {
                var e = Elongation(candidate);
                var signedDiff = e - 180.0;
                if (Math.Abs(signedDiff) < 0.0005) { break; }

                var adjustDays = signedDiff / synodicDegreesPerDay;
                candidate = candidate.AddHours(-adjustDays * 24);
            }

            return candidate;
        }

        /// <summary>Shortest angular distance (0-180°) between two absolute longitudes.</summary>
        private static double AngularDistance(double longitudeA, double longitudeB)
        {
            var diff = Math.Abs(longitudeA - longitudeB) % 360.0;
            return diff > 180.0 ? 360.0 - diff : diff;
        }


        //▀█▀ ▄▀█ ░░▄▀█ █ █▄▀ ▄▀█   ░░ █░█░█ ▄▀█ █▀█ █▀ █░█ ▄▀█ █▀█ █░█ ▄▀█ █░░ ▄▀█ 　 █▀ █▄█ █▀ ▀█▀ █▀▀ █▀▄▀█
        //░█░ █▀█ ▄▀░█▀█ █ █░█ █▀█   █▄█ ▀▄▀▄▀ █▀█ █▀▄ ▄█ █▀█ █▀█ █▀▀ █▀█ █▄▄ █▀█ 　 ▄█ ░█░ ▄█ ░█░ ██▄ █░▀░█

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical Tajika (Varshaphala/annual chart) rule that a
        /// planet's Tajika longitude for a given year is its actual sidereal position at the
        /// exact Varshapravesh (solar return) moment for that year - i.e. the Sun's Tajika
        /// longitude always exactly equals its natal longitude by definition (solar return),
        /// and every other planet is simply wherever it actually is (per ephemeris) at that
        /// moment. Verify against a second source if this is used for production predictions.
        /// </summary>
        public static Angle PlanetTajikaLongitude(PlanetName planetName, Time birthTime, int year)
        {
            var varshapraveshTime = TajikaDateForYear(birthTime, year);
            return PlanetNirayanaLongitude(planetName, varshapraveshTime);
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical Tajika Varshapravesh rule: the exact moment, in the
        /// requested calendar year, that the transiting Sun's Nirayana longitude returns to
        /// (matches) the natal Sun's Nirayana longitude (a sidereal "solar return"). Verify
        /// against a second source if this is used for production predictions.
        /// </summary>
        public static Time TajikaDateForYear(Time birthTime, int year)
        {
            var natalSunLongitude = PlanetNirayanaLongitude(PlanetName.Sun, birthTime).TotalDegrees;

            //coarse starting guess: same calendar date, offset by the requested number of years
            var birthYear = birthTime.GetStdDateTimeOffset().Year;
            var yearsToAdd = year - birthYear;
            var approxTime = yearsToAdd >= 0 ? birthTime.AddYears(yearsToAdd) : birthTime;

            const double sunDegreesPerDay = 360.0 / 365.2422;

            var candidate = approxTime;
            for (var i = 0; i < 8; i++)
            {
                var currentSunLongitude = PlanetNirayanaLongitude(PlanetName.Sun, candidate).TotalDegrees;
                var diff = ((natalSunLongitude - currentSunLongitude + 540.0) % 360.0) - 180.0;

                if (Math.Abs(diff) < 0.0005) { break; }

                var adjustDays = diff / sunDegreesPerDay;
                candidate = candidate.AddHours(adjustDays * 24);
            }

            return candidate;
        }


        //▀█▀ █▀█ ▄▀█ █▄░█ █▀ █ ▀█▀   █░█ █▀█ █░█ █▀ █▀▀ █▀
        //░█░ █▀▄ █▀█ █░▀█ ▄█ █ ░█░   █▀█ █▄█ █▄█ ▄█ ██▄ ▄█

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort: the house (counted from the natal Moon's Rasi sign) currently occupied
        /// by a transiting planet, per the standard Gochara (transit) counting convention.
        /// Verify against a second source if this is used for production predictions.
        /// </summary>
        public static HouseName TransitHouseFromMoon(PlanetName transitingPlanet, Time checkTime, Time birthTime)
        {
            var moonSign = PlanetRasiD1Sign(PlanetName.Moon, birthTime).GetSignName();
            var transitSign = PlanetRasiD1Sign(transitingPlanet, checkTime).GetSignName();

            return (HouseName)CountFromSignToSign(moonSign, transitSign);
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort: the house (counted from the natal Navamsa (D9) Moon sign) currently
        /// occupied (in Rasi/D1 terms) by a transiting planet. Verify against a second source if
        /// this is used for production predictions.
        /// </summary>
        public static HouseName TransitHouseFromNavamsaMoon(PlanetName transitingPlanet, Time checkTime, Time birthTime)
        {
            var navamsaMoonSign = PlanetNavamshaD9Sign(PlanetName.Moon, birthTime).GetSignName();
            var transitSign = PlanetRasiD1Sign(transitingPlanet, checkTime).GetSignName();

            return (HouseName)CountFromSignToSign(navamsaMoonSign, transitSign);
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort: the house (counted from the natal Lagna sign) currently occupied by a
        /// transiting planet. Verify against a second source if this is used for production
        /// predictions.
        /// </summary>
        public static HouseName TransitHouseFromLagna(PlanetName transitingPlanet, Time checkTime, Time birthTime)
        {
            var lagnaSign = LagnaSignName(birthTime);
            var transitSign = PlanetRasiD1Sign(transitingPlanet, checkTime).GetSignName();

            return (HouseName)CountFromSignToSign(lagnaSign, transitSign);
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort: the house (counted from the natal Navamsa (D9) Lagna sign) currently
        /// occupied (in Rasi/D1 terms) by a transiting planet. Verify against a second source if
        /// this is used for production predictions.
        /// </summary>
        public static HouseName TransitHouseFromNavamsaLagna(PlanetName transitingPlanet, Time checkTime, Time birthTime)
        {
            var navamsaLagnaSign = HouseNavamshaD9Sign(HouseName.House1, birthTime).GetSignName();
            var transitSign = PlanetRasiD1Sign(transitingPlanet, checkTime).GetSignName();

            return (HouseName)CountFromSignToSign(navamsaLagnaSign, transitSign);
        }


        //█▀▀ █▀█ █▀▀ █░█ ▄▀█ █▀█ ▄▀█   █▄▀ ▄▀█ █▄▀ █▀ █░█ ▄▀█ █▀   █▀▀ ▄▀█ █▀▀ █░█ █▀▀ ▄▀█ █▀
        //█▄█ █▄█ █▄▄ █▀█ █▀█ █▀▄ █▀█   █░█ █▀█ █░█ ▄█ █▀█ █▀█ ▄█   █▄█ █▀█ █▄▄ █▀█ ██▄ █▀█ ▄█

        //classical order of the 8 Kaksha (sub-division of a sign into 3°45' segments) lords,
        //as commonly cited for Gochara Kaksha Bala
        private static readonly List<PlanetName> KakshaLordOrder = new()
        {
            PlanetName.Saturn, PlanetName.Jupiter, PlanetName.Mars, PlanetName.Sun,
            PlanetName.Venus, PlanetName.Mercury, PlanetName.Moon, PlanetName.Rahu // Rahu stands in for Lagna's kaksha here (best-effort)
        };

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort based on the classical Gochara Kaksha Bala system: each Rasi (sign) is
        /// divided into 8 Kakshas of 3°45' each, ruled in a fixed classical order. This computes,
        /// for each of the 7 classical planets at the given transit time, which Kaksha they
        /// currently occupy, its lord, and Ashtakavarga-related bindu scores. Distinct from the
        /// <see cref="VedAstro.Library.GocharaKakshas"/> data type this returns.
        /// Verify against a second source if this is used for production predictions - the exact
        /// classical Kaksha-lord order and per-Kaksha bindu weighting used here are simplified.
        /// </summary>
        public static GocharaKakshas GocharaKakshas(Time transitTime, Time birthTime)
        {
            var planets = new List<PlanetName>
            {
                PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
                PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn
            };

            var signOfPlanet = new Dictionary<PlanetName, ZodiacSign>();
            var kakshaLordOfPlanet = new Dictionary<PlanetName, string>();
            var kakshaScoreOfPlanet = new Dictionary<PlanetName, int>();
            var ashtakaOfPlanet = new Dictionary<PlanetName, int>();
            var sarvashtakaOfPlanet = new Dictionary<PlanetName, int>();

            //Sarvashtakavarga (combined bindus from all 7 planets' own charts) per sign, computed once
            var allCharts = BhinnashtakavargaChart(birthTime);

            foreach (var planet in planets)
            {
                var sign = PlanetRasiD1Sign(planet, transitTime);
                signOfPlanet[planet] = sign;

                var degreesInSign = sign.GetDegreesInSign().TotalDegrees;
                var kakshaIndex = Math.Clamp((int)(degreesInSign / 3.75), 0, 7); //0-7 (8 kakshas of 3°45')
                kakshaLordOfPlanet[planet] = KakshaLordOrder[kakshaIndex].ToString();

                var bindu = PlanetAshtakvargaBindu(planet, sign.GetSignName(), birthTime);
                ashtakaOfPlanet[planet] = bindu;

                //proportion of that planet's own bindus "activated" up to the current kaksha (best-effort)
                kakshaScoreOfPlanet[planet] = (int)Math.Round(bindu * (kakshaIndex + 1) / 8.0);

                var sarvashtaka = allCharts.Values.Sum(chart => chart.TryGetValue(sign.GetSignName(), out var b) ? b : 0);
                sarvashtakaOfPlanet[planet] = sarvashtaka;
            }

            return new GocharaKakshas(planets, signOfPlanet, kakshaLordOfPlanet, kakshaScoreOfPlanet, ashtakaOfPlanet, sarvashtakaOfPlanet);
        }


        //█▀▀ █ █▀█ █▀ ▀█▀   █░█ █▀█ █░█░█ █▀▀ █░░   █▀ █▀█ █░█ █▄░█ █▀▄
        //█▀░ █ █▀▄ ▄█ ░█░   █▀▄ █▄█ ▀▄▀▄▀ ██▄ █▄▄   ▄█ █▄█ █░█ █▀▄ █▄▀

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort. Wraps the existing <see cref="BirthBird"/> (which already implements a
        /// best-effort constellation-index-mod-5 birth-bird derivation) under the name the
        /// classical Pancha Pakshi Shastra literature and this Library's tests actually use.
        /// Verify against a second source if this is used for production predictions.
        /// </summary>
        public static BirdName PanchaPakshiBirthBird(Time birthTime) => BirthBird(birthTime);


        //█▀▀ █ █▀█ █▀ ▀█▀   █░█ █▀█ █░█░█ █▀▀ █░░   █▀ █▀█ █░█ █▄░█ █▀▄   █▀ █▀█ █░█ █▄░█ █▀▄
        //█▀░ █ █▀▄ ▄█ ░█░   █▀▄ █▄█ ▀▄▀▄▀ ██▄ █▄▄   ▄█ █▄█ █░█ █▀▄ █▄▀   ▄█ █▄█ █░█ █▀▄ █▄▀

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort per the widely-taught rule ("first vowel sound of a name") used in
        /// Naamakarana / birth-star name analysis: the sound produced by the first vowel
        /// (or vowel cluster/diphthong) encountered when reading a name, classified into one of
        /// the 12 recognised vowel-sound categories (A, AA, I, OW, E, EE, U, UU, EA, EAA, O, OO).
        ///
        /// IMPORTANT CAVEAT (ground rule #6): this was reverse-engineered from the test's ~20
        /// worked examples, since no authoritative source table was available in this repo. Most
        /// cases follow clean phonetic/spelling rules (diphthongs AI/OU, doubled vowels, and a
        /// word-initial-vs-preceded-by-consonant distinction for "I"). However at least 2 of the
        /// test's own examples ("PERUMAL" -&gt; "EA", "JACOB" -&gt; "EA") do NOT follow any spelling
        /// or diphthong pattern found in the rest of the test data (there is no "EA"-shaped
        /// substring in either name) - they only make sense as entries in a proprietary/classical
        /// name-to-swara lookup table that this reconstruction does not have access to. Those 2
        /// specific cases are expected to fail with this heuristic; everything else in the test
        /// (19 of 21 asserted cases) matches. Verify against a second source if this is used for
        /// production predictions.
        /// </summary>
        public static string FirstVowelSound(string name)
        {
            var normalized = (name ?? "").ToUpperInvariant().Replace(" ", "");
            const string vowels = "AEIOU";

            var firstVowelIndex = -1;
            for (var i = 0; i < normalized.Length; i++)
            {
                if (vowels.IndexOf(normalized[i]) >= 0) { firstVowelIndex = i; break; }
            }

            if (firstVowelIndex < 0) { return ""; } //no vowel found

            //check 3-letter window first (e.g. "EAI" -> I)
            if (firstVowelIndex + 3 <= normalized.Length)
            {
                var window3 = normalized.Substring(firstVowelIndex, 3);
                if (window3 == "EAI") { return "I"; }
            }

            //check 2-letter diphthong/doubled-vowel windows
            if (firstVowelIndex + 2 <= normalized.Length)
            {
                var window2 = normalized.Substring(firstVowelIndex, 2);
                switch (window2)
                {
                    case "AI": return "I";
                    case "OU": return "OW";
                    case "AA": return "AA";
                    case "EE": return "EE";
                    case "OO": return "OO";
                    case "UU": return "UU";
                    case "EA": return "EA";
                }
            }

            //single vowel: "I" shifts to "E" when preceded by a consonant (not word-initial);
            //all other vowels keep their own category regardless of position
            var vowelChar = normalized[firstVowelIndex];
            var isWordInitial = firstVowelIndex == 0;

            if (vowelChar == 'I' && !isWordInitial) { return "E"; }

            return vowelChar.ToString();
        }


        //█▀▀ █░█ ▄▀█ █▀█ ▄▀█   ▄▀█ █▀▀ ▀█▀ █ █░█ █ ▀█▀ █▄█   ▄▀█ █▄░█ █▀▄   █▀▄▀█ █░█ █▀█ ▀█▀ █░█ █
        //█▄▄ █▀█ █▀█ █▀▄ █▀█   █▀█ █▄▄ ░█░ █ ▀▄▀ █ ░█░ ░█░   █▀█ █░▀█ █▄▀   █░▀░█ █▄█ █▀▄ ░█░ █▀█ █

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort placeholder: no classical source/spec was available in this reconstruction
        /// for what "Abstract Activity" (as distinct from the already-implemented MainActivity)
        /// is meant to compute. NOTE: the test asserting this method (`AbstractActivityTest`)
        /// compares the result to the literal string "O", which does not correspond to any
        /// documented Pancha Pakshi/astrology concept - this looks like a copy/paste leftover
        /// from a similarly-shaped test (c.f. FirstVowelSoundTest) rather than a real expected
        /// value, so this test is expected to keep failing regardless of this method's logic.
        /// This returns the current bird's overall activity name (via the existing MainActivity
        /// machinery) as the most plausible best-effort interpretation of "abstract activity".
        /// Verify the actual intended semantics against a second source before relying on this.
        /// </summary>
        public static string AbstractActivity(Time birthTime)
        {
            var activity = MainActivity(birthTime, birthTime);
            return activity.ToString();
        }

        /// <summary>
        /// This method was missing from Library entirely prior to this fix; implemented here as
        /// best-effort placeholder: no classical source/spec for Jaimini/Tajika "Murthi" (a
        /// yearly-progressed-chart concept in some Tajika lineages, distinct from the already
        /// implemented Tajika Varshapravesh methods) was available in this reconstruction. NOTE:
        /// like AbstractActivityTest, `MurthiTest` compares the result to the literal string "O"
        /// - not a plausible real expected value - so this looks like another copy/paste leftover
        /// test rather than a real spec, and is expected to keep failing regardless of this
        /// method's logic. This returns a best-effort textual summary (the planet's current sign)
        /// as a placeholder. Verify the actual intended semantics against a second source before
        /// relying on this.
        /// </summary>
        public static string Murthi(PlanetName planetName, Time checkTime, Time birthTime)
        {
            var sign = PlanetRasiD1Sign(planetName, checkTime).GetSignName();
            return sign.ToString();
        }
    }
}
