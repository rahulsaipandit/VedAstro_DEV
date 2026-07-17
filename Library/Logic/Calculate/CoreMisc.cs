using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VedAstro.Library
{
    /// <summary>
    /// Geo/timezone lookup, Ashtakavarga/Gochara wiring, Pancha Pakshi wiring & assorted
    /// misc facade methods. Reconstructed from scratch - see CoreTime.cs header note.
    /// </summary>
    public partial class Calculate
    {
        /// <summary>
        /// Length of the solar year (in days) used for Dasa/timing calculations.
        /// Mutable - some ayanamsa systems (e.g. Krishnamurti) use a slightly different value.
        /// Default matches the sidereal year, consistent with the default Lahiri ayanamsa.
        /// </summary>
        public static double SolarYearTimeSpan = 365.2564;


        //█░█░█ █▀▀ █▄▄   ▄▀█ █▀▀ █▀▀ █▀▀ █▀ █▀
        //▀▄▀▄▀ ██▄ █▄█   █▀█ █▄▄ █▄▄ ██▄ ▄█ ▄█

        /// <summary>
        /// Given an address, converts to its geo location equivalent.
        /// NOTE: uses the free Nominatim (OpenStreetMap) API - no key required, suitable for local dev.
        /// EXP: ../Calculate/AddressToGeoLocation/Address/Gaithersburg
        /// </summary>
        public static async Task<GeoLocation> AddressToGeoLocation(string address)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "VedAstro/1.0");
                client.Timeout = TimeSpan.FromSeconds(10);

                var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";
                var raw = await client.GetStringAsync(url);
                var results = Newtonsoft.Json.Linq.JArray.Parse(raw);

                if (results.Count == 0) { return GeoLocation.Empty; }

                var lat = (double)results[0]["lat"];
                var lon = (double)results[0]["lon"];
                var name = (string)results[0]["display_name"] ?? address;

                return new GeoLocation(name, lon, lat);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddressToGeoLocation] failed for '{address}': {ex.Message}");
                return GeoLocation.Empty;
            }
        }

        /// <summary>
        /// Gets the timezone offset for a given location & time, accounting for historical/DST changes
        /// where possible. NOTE: falls back to a longitude-based (non-DST-aware) offset if the
        /// free timeapi.io lookup fails or is unreachable (e.g. fully offline local dev).
        /// </summary>
        public static async Task<string> GeoLocationToTimezone(GeoLocation geoLocation, DateTimeOffset timeAtLocation)
        {
            var offset = await GeoLocationToTimezoneOffset(geoLocation, timeAtLocation);

            //format matching Time.DateTimeFormatTimezone ("zzz" -> "+05:30")
            var sign = offset < TimeSpan.Zero ? "-" : "+";
            var absOffset = offset.Duration();
            return $"{sign}{absOffset.Hours:D2}:{absOffset.Minutes:D2}";
        }

        /// <summary>Underlying TimeSpan-returning implementation of GeoLocationToTimezone.</summary>
        public static async Task<TimeSpan> GeoLocationToTimezoneOffset(GeoLocation geoLocation, DateTimeOffset timeAtLocation)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var lat = geoLocation.Latitude().ToString(System.Globalization.CultureInfo.InvariantCulture);
                var lon = geoLocation.Longitude().ToString(System.Globalization.CultureInfo.InvariantCulture);
                var url = $"https://timeapi.io/api/TimeZone/coordinate?latitude={lat}&longitude={lon}";

                var raw = await client.GetStringAsync(url);
                var json = Newtonsoft.Json.Linq.JObject.Parse(raw);
                var offsetSeconds = (int?)json["standardUtcOffset"]?["seconds"];

                if (offsetSeconds.HasValue) { return TimeSpan.FromSeconds(offsetSeconds.Value); }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GeoLocationToTimezone] lookup failed, falling back to longitude estimate: {ex.Message}");
            }

            //offline fallback: crude longitude-based estimate, not DST-aware
            return LongitudeToLMTOffset(geoLocation.Longitude());
        }

        /// <summary>
        /// Given a birth time and a human time-range preset, generates a start & end time.
        /// Supports presets like "age1to10", "3weeks", "3months", "3years", "fulllife", or a literal "1990-2000" year range.
        /// outputTimezone: output timezone can be different from birth timezone (e.g. "+08:00").
        /// </summary>
        public static (Time start, Time end) AutoCalculateTimeRange(Time inputBirthTime, string timePreset, TimeSpan outputTimezone)
        {
            var geoLocation = inputBirthTime.GetGeoLocation();
            var birthStd = inputBirthTime.GetStdDateTimeOffset();

            Time AtOffset(DateTimeOffset instant) => new Time(instant.ToOffset(outputTimezone), geoLocation);

            var preset = timePreset?.ToLowerInvariant() ?? "";

            //literal year range, e.g. "1990-2000"
            var yearRangeParts = preset.Split('-');
            if (yearRangeParts.Length == 2 && int.TryParse(yearRangeParts[0], out var fromYear) && int.TryParse(yearRangeParts[1], out var toYear))
            {
                var start = new DateTimeOffset(fromYear, 1, 1, 0, 0, 0, outputTimezone);
                var end = new DateTimeOffset(toYear, 12, 31, 23, 59, 59, outputTimezone);
                return (AtOffset(start), AtOffset(end));
            }

            //"ageXtoY", e.g. "age1to10"
            if (preset.StartsWith("age"))
            {
                var ageParts = preset.Substring(3).Split("to");
                if (ageParts.Length == 2 && int.TryParse(ageParts[0], out var fromAge) && int.TryParse(ageParts[1], out var toAge))
                {
                    var start = birthStd.AddYears(fromAge);
                    var end = birthStd.AddYears(toAge);
                    return (AtOffset(start), AtOffset(end));
                }
            }

            //"Nweeks" / "Nmonths" / "Nyears" centered on birth time going forward
            var numberPart = new string(preset.TakeWhile(char.IsDigit).ToArray());
            if (int.TryParse(numberPart, out var n))
            {
                if (preset.EndsWith("weeks")) { return (AtOffset(birthStd), AtOffset(birthStd.AddDays(n * 7))); }
                if (preset.EndsWith("months")) { return (AtOffset(birthStd), AtOffset(birthStd.AddMonths(n))); }
                if (preset.EndsWith("years")) { return (AtOffset(birthStd), AtOffset(birthStd.AddYears(n))); }
            }

            //"fulllife" or unrecognized preset: default to a wide 100 year span from birth
            return (AtOffset(birthStd), AtOffset(birthStd.AddYears(100)));
        }


        //█▀█ █▀▀ █░░ ▄▀█ ▀█▀ █▀▀ █▀▄   █▀▀ █░█ █▀▀ █▀▀ █▄▀ █▀
        //█▀▄ ██▄ █▄▄ █▀█ ░█░ ██▄ █▄▀   █▄▄ █▀█ ██▄ █▄▄ █░█ ▄█

        /// <summary>
        /// Whenever an affliction by way of a malefic occupying the same sign as Mercury, or aspecting it,
        /// is present. Simplified/best-effort per the original doc's own noted uncertainty.
        /// </summary>
        public static bool IsMercuryAfflicted(Time time)
        {
            var malefics = MaleficPlanetList(time).Where(p => p != PlanetName.Mercury);

            foreach (var malefic in malefics)
            {
                if (IsPlanetConjunctWithPlanet(PlanetName.Mercury, malefic, time)) { return true; }
                if (IsPlanetAspectedByPlanet(PlanetName.Mercury, malefic, time)) { return true; }
            }

            return false;
        }

        /// <summary>
        /// Used for judging dasa good/bad. Simplified proxy for classical Ishta/Kashta Phala:
        /// based on angular distance from the planet's debilitation point (further = better).
        /// Output range -5 (worst) to +5 (best). Bala book pg. 110.
        /// </summary>
        public static double PlanetIshtaKashtaScoreDegree(PlanetName planet, Time birthTime)
        {
            var longitude = PlanetNirayanaLongitude(planet, birthTime).TotalDegrees;
            var debilitation = PlanetDebilitationPointAngle(planet).TotalDegrees;

            //0 at debilitation, 180 at exaltation (opposite point)
            var distanceFromDebilitation = Math.Abs(((longitude - debilitation + 540.0) % 360.0) - 180.0);

            return distanceFromDebilitation.Remap(0, 180, -5, 5);
        }

        /// <summary>
        /// Also known as Chandramana or Hindu Month. Named after the constellation the Moon is in
        /// on the nearest full Moon day. NOTE: approximated via a single linear step to the nearest
        /// full moon (not iterated to full numeric convergence) - accurate to about a day, which is
        /// well within a single lunar month's constellation span.
        /// </summary>
        public static LunarMonth LunarMonth(Time time)
        {
            var moonLong = PlanetNirayanaLongitude(PlanetName.Moon, time).TotalDegrees;
            var sunLong = PlanetNirayanaLongitude(PlanetName.Sun, time).TotalDegrees;
            var elongation = (moonLong - sunLong + 360.0) % 360.0; //0 = new moon, 180 = full moon

            //Moon gains on Sun at roughly 360/29.53 - 360/365.25 ≈ 12.19 degrees/day
            var degreesToFullMoon = 180.0 - elongation;
            var daysToFullMoon = degreesToFullMoon / 12.19;

            var fullMoonInstant = time.GetStdDateTimeOffset().AddDays(daysToFullMoon);
            var fullMoonTime = new Time(fullMoonInstant, time.GetGeoLocation());

            var moonConstellationAtFullMoon = ConstellationAtLongitude(PlanetNirayanaLongitude(PlanetName.Moon, fullMoonTime)).GetConstellationName();

            return ConstellationToLunarMonth(moonConstellationAtFullMoon);
        }

        private static LunarMonth ConstellationToLunarMonth(ConstellationName constellation)
        {
            var months = new[]
            {
                global::VedAstro.Library.LunarMonth.Chaitra, global::VedAstro.Library.LunarMonth.Vaisaakha,
                global::VedAstro.Library.LunarMonth.Jyeshtha, global::VedAstro.Library.LunarMonth.Aashaadha,
                global::VedAstro.Library.LunarMonth.Sraavana, global::VedAstro.Library.LunarMonth.Bhaadrapada,
                global::VedAstro.Library.LunarMonth.Aaswayuja, global::VedAstro.Library.LunarMonth.Kaarteeka,
                global::VedAstro.Library.LunarMonth.Maargasira, global::VedAstro.Library.LunarMonth.Pushya,
                global::VedAstro.Library.LunarMonth.Maagha, global::VedAstro.Library.LunarMonth.Phaalguna
            };

            var constellationIndex = (int)constellation; //1 to 27
            var monthIndex = (int)Math.Round((constellationIndex - 1) / (27.0 / 12.0)) % 12;

            return months[monthIndex];
        }


        //█▀█ █▀ █░█ ▀█▀ ▄▀█ █▄▀ ▄▀█ █░█ ▄▀█ █▀█ █▀▀ ▄▀█   █░░ █▀▀ █▀█ █▀▀ █░█ ▄▀█ █▀█ ▄▀█
        //█▀▄ ▄█ █▀█ ░█░ █▀█ █░█ █▀█ ▀▄▀ █▀█ █▀▄ █▄█ █▀█   █▄▄ █▄█ █▄█ █▄▄ █▀█ █▀█ █▀▄ █▀█

        private static readonly List<PlanetName> SevenAshtakavargaPlanets = new()
        {
            PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
            PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn
        };

        /// <summary>
        /// Seven different Bhinnashtakavarga charts, one per planet (Rahu/Ketu excluded per classical convention).
        /// </summary>
        public static Dictionary<PlanetName, Dictionary<ZodiacName, int>> BhinnashtakavargaChart(Time birthTime)
        {
            var result = new Dictionary<PlanetName, Dictionary<ZodiacName, int>>();
            foreach (var planet in SevenAshtakavargaPlanets)
            {
                result[planet] = Ashtakavarga.BhinnashtakavargaChartForPlanet(planet, birthTime);
            }
            return result;
        }

        /// <summary>
        /// Gets the Ashtakavarga bindu (0-8) for a given planet at a given sign, from that planet's own
        /// Bhinnashtakavarga chart.
        /// </summary>
        public static int PlanetAshtakvargaBindu(PlanetName planet, ZodiacName signToCheck, Time time)
        {
            var chart = Ashtakavarga.BhinnashtakavargaChartForPlanet(planet, time);
            return chart.TryGetValue(signToCheck, out var bindu) ? bindu : 0;
        }

        /// <summary>
        /// Checks if a Gochara (transit) is occurring for a planet in a given house (counted from natal Moon),
        /// without checking bindu strength. Wrapper method for Gochara event calculations.
        /// </summary>
        public static bool IsGocharaOccurring(Time birthTime, Time time, PlanetName planet, int gocharaHouse)
        {
            var targetSign = SignCountedFromPlanetSign(gocharaHouse, PlanetName.Moon, birthTime);
            var currentSign = PlanetRasiD1Sign(planet, time).GetSignName();

            return currentSign == targetSign;
        }

        /// <summary>
        /// Checks if a given planet, with the given number of Ashtakavarga bindu (at its current transit sign),
        /// is transiting now.
        /// </summary>
        public static bool IsPlanetGocharaBindu(Time birthTime, Time nowTime, PlanetName planet, int bindu)
        {
            //no ashtakavarga bindu defined for rahu/ketu
            if (planet == PlanetName.Rahu || planet == PlanetName.Ketu) { return false; }

            var currentSign = PlanetRasiD1Sign(planet, nowTime).GetSignName();
            var actualBindu = PlanetAshtakvargaBindu(planet, currentSign, birthTime);

            return actualBindu == bindu;
        }


        //█▀█ ▄▀█ █▄░█ █▀▀ █░█ ▄▀█   █▀█ ▄▀█ █▄▀ █▀ █░█ █
        //█▀▀ █▀█ █░▀█ █▄▄ █▀█ █▀█   █▀▀ █▀█ █░█ ▄█ █▀█ █

        /// <summary>
        /// These 5 elemental vibrations act in 5 gradations of faculties for stipulated time intervals
        /// called Yamas, consisting of 2 hrs 24 mins each (6 Ghatikas), 5 Yamas in the day and 5 in the night.
        /// </summary>
        public static BirthYama BirthYama(Time inputTime)
        {
            var span = DayOrNightSpan(inputTime);
            var yamaLength = TimeSpan.FromTicks((span.end - span.start).Ticks / 5);

            var elapsed = inputTime.GetStdDateTimeOffset() - span.start;
            var yamaIndex = (int)(elapsed.Ticks / yamaLength.Ticks);
            yamaIndex = Math.Clamp(yamaIndex, 0, 4);

            var yamaStart = span.start + (yamaLength * yamaIndex);
            var yamaEnd = span.start + (yamaLength * (yamaIndex + 1));

            return new BirthYama(yamaIndex + 1, new Time(yamaStart, inputTime.GetGeoLocation()), new Time(yamaEnd, inputTime.GetGeoLocation()));
        }

        /// <summary>Gets the day (sunrise-sunset) or night (sunset-next sunrise) span containing the given time.</summary>
        private static (DateTimeOffset start, DateTimeOffset end) DayOrNightSpan(Time time)
        {
            var sunrise = SunriseTime(time).GetStdDateTimeOffset();
            var sunset = SunsetTime(time).GetStdDateTimeOffset();
            var t = time.GetStdDateTimeOffset();

            if (t >= sunrise && t < sunset) { return (sunrise, sunset); }

            if (t >= sunset)
            {
                var nextDayTime = new Time(t.AddDays(1), time.GetGeoLocation());
                var nextSunrise = SunriseTime(nextDayTime).GetStdDateTimeOffset();
                return (sunset, nextSunrise);
            }

            //t < sunrise: still in the previous night
            var prevDayTime = new Time(t.AddDays(-1), time.GetGeoLocation());
            var prevSunset = SunsetTime(prevDayTime).GetStdDateTimeOffset();
            return (prevSunset, sunrise);
        }

        /// <summary>
        /// Gets the birth (Pancha Pakshi) bird's main activity at the given check time.
        /// NOTE: birth-bird derivation uses a simplified constellation-index-mod-5 rule as a
        /// best-effort reconstruction - review against Pulippani's classical tables before
        /// relying on this for precise predictive work.
        /// </summary>
        public static BirdActivity MainActivity(Time birthTime, Time checkTime)
        {
            var bird = BirthBird(birthTime);

            //determine day/night & yama number at checkTime using the same 5-Yama division as BirthYama
            var sunrise = SunriseTime(checkTime).GetStdDateTimeOffset();
            var sunset = SunsetTime(checkTime).GetStdDateTimeOffset();
            var t = checkTime.GetStdDateTimeOffset();
            var timeOfDay = (t >= sunrise && t < sunset) ? PanchaPakshi.TimeOfDay.Day : PanchaPakshi.TimeOfDay.Night;

            var yama = BirthYama(checkTime).YamaCount;
            var dayOfWeek = DayOfWeek(checkTime);

            try
            {
                return PanchaPakshi.TableData[timeOfDay][dayOfWeek][yama][bird];
            }
            catch (KeyNotFoundException)
            {
                return BirdActivity.Ruling; //safe neutral default if the classical table doesn't cover this exact slot
            }
        }

        /// <summary>
        /// Gets the birth Pancha Pakshi bird for a birth time. Best-effort: uses birth constellation
        /// index mod 5, cycling through Vulture/Owl/Crow/Cock/Peacock. Review against a classical
        /// reference before relying on this for precise predictive work.
        /// </summary>
        public static BirdName BirthBird(Time birthTime)
        {
            var constellation = ConstellationAtLongitude(PlanetNirayanaLongitude(PlanetName.Moon, birthTime)).GetConstellationName();
            var birds = new[] { BirdName.Vulture, BirdName.Owl, BirdName.Crow, BirdName.Cock, BirdName.Peacock };

            var index = ((int)constellation - 1) % birds.Length;
            return birds[index];
        }


        //█░░ █▀▀ █▀▀ ▄▀█ █▀▀ █▄█   █▀▀ █ █░░ █▀▀   █ █▀▄▀█ █▀█ █▀█ █▀█ ▀█▀
        //█▄▄ ██▄ █▄█ █▀█ █▄▄ ░█░   █▄▄ █ █▄▄ ██▄   █ █░▀░█ █▀▀ █▄█ █▀▄ ░█░

        /// <summary>
        /// Best-effort parser for legacy Jagannatha Hora (.jhd) birth data files: scans for
        /// date/time/coordinate-shaped tokens rather than assuming an exact field layout, since
        /// the precise JHD spec wasn't available for this reconstruction (see CoreTime.cs header
        /// note). Falls back to Person.Empty (renamed) fields on anything it can't confidently parse -
        /// review imported data before relying on it.
        /// </summary>
        public static Person ParseJHDFiles(string personName, string rawJhdFile)
        {
            try
            {
                var lines = rawJhdFile.Split('\n', '\r').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

                //date looks like d,m,y or d/m/y ; time looks like h,m,s or h:m:s
                var dateMatch = System.Text.RegularExpressions.Regex.Match(rawJhdFile, @"(\d{1,2})[,/](\d{1,2})[,/](\d{4})");
                var timeMatch = System.Text.RegularExpressions.Regex.Match(rawJhdFile, @"(\d{1,2})[,:](\d{1,2})[,:](\d{1,2})");
                var coordMatch = System.Text.RegularExpressions.Regex.Match(rawJhdFile, @"(-?\d{1,3}\.\d+)[,\s]+(-?\d{1,3}\.\d+)");

                var geoLocation = coordMatch.Success
                    ? new GeoLocation(personName, double.Parse(coordMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture), double.Parse(coordMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
                    : GeoLocation.Empty;

                if (dateMatch.Success && timeMatch.Success)
                {
                    var day = int.Parse(dateMatch.Groups[1].Value);
                    var month = int.Parse(dateMatch.Groups[2].Value);
                    var year = int.Parse(dateMatch.Groups[3].Value);
                    var hour = int.Parse(timeMatch.Groups[1].Value);
                    var minute = int.Parse(timeMatch.Groups[2].Value);
                    var second = int.Parse(timeMatch.Groups[3].Value);

                    var lmtDateTime = new System.DateTime(year, month, day, hour, minute, second);
                    var birthTime = new Time(lmtDateTime, GetLocalTimeOffsetSafe(geoLocation), geoLocation);

                    return new Person(personName, birthTime, Gender.Male);
                }

                Console.WriteLine($"[ParseJHDFiles] could not confidently parse '{personName}', returning with empty birth time - review manually.");
                return new Person(personName, Time.Empty, Gender.Male);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ParseJHDFiles] failed for '{personName}': {ex.Message}");
                return new Person(personName, Time.Empty, Gender.Male);
            }
        }

        private static TimeSpan GetLocalTimeOffsetSafe(GeoLocation geoLocation) =>
            geoLocation.Equals(GeoLocation.Empty) ? TimeSpan.Zero : LongitudeToLMTOffset(geoLocation.Longitude());
    }
}
