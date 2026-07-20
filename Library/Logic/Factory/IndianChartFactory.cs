using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace VedAstro.Library
{
    /// <summary>
    /// Renders South/North Indian style Kundali (birth chart) SVGs: an orange picture-frame
    /// border around a 4x4 house grid (12 perimeter cells + one merged center title cell),
    /// each house showing a house-number badge, the planets occupying it, and its zodiac sign.
    /// Reconstructed from scratch - see Library/Logic/Calculate/CoreTime.cs header note (the
    /// original chart-drawing code was never committed to git, only its doc metadata survived).
    /// </summary>
    public partial class Calculate
    {
        private const int ChartSize = 480;
        private const int FrameThickness = 14;
        private const string FrameColor = "#f5992c";
        private const string FrameColorDark = "#c4741f";
        private const string HouseBadgeColor = "#2a5db0";

        //SkyChart is a wide horizontal ruler/timeline (angle ruler + zodiac band + house band +
        //planet icons hanging below), not a square grid like the Indian chart - 750x230 is the
        //original Azure-era aspect ratio it was designed around, so it must NOT reuse ChartSize
        private const int SkyChartWidth = 750;
        private const int SkyChartHeight = 230;

        /// <summary>Gets sky chart at a given time. SVG image, URL can be used like an image source link.</summary>
        public static async System.Threading.Tasks.Task<string> SkyChart(Time time) =>
            await SkyChartFactory.GenerateChart(time, SkyChartWidth, SkyChartHeight);

        /// <summary>Creates a Kundali chart (D1 to D20) in South Indian style. SVG image.</summary>
        public static string SouthIndianChart(Time time, ChartType chartType = ChartType.RasiD1) => GenerateIndianChartSvg(time, chartType, northIndianStyle: false);

        /// <summary>Creates a Kundali chart (D1 to D20) in North Indian style. SVG image.</summary>
        public static string NorthIndianChart(Time time, ChartType chartType = ChartType.RasiD1) => GenerateIndianChartSvg(time, chartType, northIndianStyle: true);

        private static Dictionary<PlanetName, ZodiacName> GetPlanetSignsForChartType(Time time, ChartType chartType)
        {
            var signGetters = new Dictionary<ChartType, System.Func<Time, Dictionary<PlanetName, ZodiacSign>>>
            {
                [ChartType.RasiD1] = AllPlanetRasiSigns,
                [ChartType.HoraD2] = AllPlanetHoraSign,
                [ChartType.DrekkanaD3] = AllPlanetDrekkanaSign,
                [ChartType.ChaturthamshaD4] = AllPlanetChaturthamsaSign,
                [ChartType.SaptamshaD7] = AllPlanetSaptamshaSign,
                [ChartType.NavamshaD9] = AllPlanetNavamshaSign,
                [ChartType.DashamamshaD10] = AllPlanetDashamamshaSign,
                [ChartType.DwadashamshaD12] = AllPlanetDwadashamshaSign,
                [ChartType.ShodashamshaD16] = AllPlanetShodashamshaSign,
                [ChartType.VimshamshaD20] = AllPlanetVimshamshaSign,
                [ChartType.ChaturvimshamshaD24] = AllPlanetChaturvimshamshaSign,
                [ChartType.BhamshaD27] = AllPlanetBhamshaSign,
                [ChartType.TrimshamshaD30] = AllPlanetTrimshamshaSign,
                [ChartType.KhavedamshaD40] = AllPlanetKhavedamshaSign,
                [ChartType.AkshavedamshaD45] = AllPlanetAkshavedamshaSign,
                [ChartType.ShashtyamshaD60] = AllPlanetShashtyamshaSign,
            };

            var getter = signGetters.TryGetValue(chartType, out var g) ? g : AllPlanetRasiSigns;

            return getter(time).ToDictionary(kv => kv.Key, kv => kv.Value.GetSignName());
        }

        /// <summary>Element (fire/earth/air/water) accent color per sign, used for the sign label text.</summary>
        private static string GetSignColor(ZodiacName sign) => sign switch
        {
            ZodiacName.Aries or ZodiacName.Leo or ZodiacName.Sagittarius => "#d9482f",
            ZodiacName.Taurus or ZodiacName.Virgo or ZodiacName.Capricorn => "#2e8b3d",
            ZodiacName.Gemini or ZodiacName.Libra or ZodiacName.Aquarius => "#2e6fd9",
            ZodiacName.Cancer or ZodiacName.Scorpio or ZodiacName.Pisces => "#a13fbf",
            _ => "#333333",
        };

        /// <summary>"RasiD1" -> "Rasi D1", "NavamshaD9" -> "Navamsha D9" (space before each interior capital).</summary>
        private static string ChartTypeDisplayName(ChartType chartType) =>
            Regex.Replace(chartType.ToString(), "(?<=[a-z])(?=[A-Z])", " ");

        /// <summary>Small house-pictogram glyph (roof + walls), used as the house-number badge icon.</summary>
        private static void AppendHouseIcon(StringBuilder svg, double x, double y, string color)
        {
            svg.Append($"<path d=\"M{x} {y + 5} L{x + 5} {y} L{x + 10} {y + 5} M{x + 1.5} {y + 4} V{y + 10} H{x + 8.5} V{y + 4}\" " +
                       $"fill=\"none\" stroke=\"{color}\" stroke-width=\"1.3\" stroke-linejoin=\"round\"/>");
        }

        /// <summary>Orange picture-frame border with 4 small decorative corner tabs, matching the reference design.</summary>
        private static void AppendFrame(StringBuilder svg)
        {
            var half = FrameThickness / 2.0;
            svg.Append($"<rect x=\"{half}\" y=\"{half}\" width=\"{ChartSize - FrameThickness}\" height=\"{ChartSize - FrameThickness}\" " +
                       $"fill=\"none\" stroke=\"{FrameColor}\" stroke-width=\"{FrameThickness}\" rx=\"4\"/>");

            //decorative corner tabs straddling each corner of the frame, like little hinge brackets
            const int tabLong = 26;
            const int tabShort = 10;
            var corners = new (double x, double y, bool horizontal)[]
            {
                (-2, -2, true), (ChartSize - tabLong + 2, -2, true),
                (-2, ChartSize - tabShort + 2, true), (ChartSize - tabLong + 2, ChartSize - tabShort + 2, true),
            };
            foreach (var (x, y, _) in corners)
            {
                svg.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{tabLong}\" height=\"{tabShort}\" fill=\"{FrameColorDark}\" rx=\"2\"/>");
            }
        }

        /// <summary>Renders one house cell: border, house-number badge, planets occupying it, and its sign name.</summary>
        private static void AppendHouseCell(StringBuilder svg, double x, double y, double cell, int houseNumber,
            ZodiacName sign, IEnumerable<PlanetName> planetsInSign)
        {
            svg.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{cell}\" height=\"{cell}\" fill=\"none\" stroke=\"{FrameColor}\" stroke-width=\"1.5\"/>");

            AppendHouseIcon(svg, x + 6, y + 6, HouseBadgeColor);
            svg.Append($"<text x=\"{x + 19}\" y=\"{y + 16}\" font-size=\"13\" font-weight=\"bold\" fill=\"{HouseBadgeColor}\">{houseNumber}</text>");

            var lineY = y + 38;
            foreach (var planet in planetsInSign)
            {
                svg.Append($"<text x=\"{x + cell / 2}\" y=\"{lineY}\" font-size=\"13\" text-anchor=\"middle\" fill=\"#222\">{planet.Name}</text>");
                lineY += 16;
            }

            svg.Append($"<text x=\"{x + 6}\" y=\"{y + cell - 8}\" font-size=\"11\" font-weight=\"bold\" fill=\"{GetSignColor(sign)}\">{sign.ToString().ToUpperInvariant()}</text>");
        }

        private static string GenerateIndianChartSvg(Time time, ChartType chartType, bool northIndianStyle)
        {
            var planetSigns = GetPlanetSignsForChartType(time, chartType);
            var lagnaSign = LagnaSignName(time);

            var svg = new StringBuilder();
            svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {ChartSize} {ChartSize}\" font-family=\"sans-serif\" font-size=\"11\">");
            svg.Append($"<rect x=\"0\" y=\"0\" width=\"{ChartSize}\" height=\"{ChartSize}\" fill=\"white\"/>");

            if (northIndianStyle)
            {
                AppendNorthIndianGrid(svg, planetSigns, lagnaSign);
            }
            else
            {
                AppendSouthIndianGrid(svg, planetSigns, lagnaSign);
            }

            AppendFrame(svg);

            //center 2x2 area is merged (no internal cross lines) and holds the chart title
            var cell = (ChartSize - 2.0 * FrameThickness) / 4;
            svg.Append($"<rect x=\"{FrameThickness + cell}\" y=\"{FrameThickness + cell}\" width=\"{2 * cell}\" height=\"{2 * cell}\" fill=\"none\" stroke=\"{FrameColor}\" stroke-width=\"1.5\"/>");
            svg.Append($"<text x=\"{ChartSize / 2}\" y=\"{ChartSize / 2}\" font-size=\"18\" font-weight=\"bold\" text-anchor=\"middle\" fill=\"#333\">{ChartTypeDisplayName(chartType)}</text>");

            svg.Append("</svg>");
            return svg.ToString();
        }

        /// <summary>Fixed 4x4 grid, signs never rotate (South Indian style) - only the house-number badge in each
        /// cell rotates with the Lagna, same as North Indian style. Starts at Pisces top-left, clockwise.
        /// The middle 2x2 area is left as one merged, unbordered cell for the chart title.</summary>
        private static void AppendSouthIndianGrid(StringBuilder svg, Dictionary<PlanetName, ZodiacName> planetSigns, ZodiacName lagnaSign)
        {
            var cell = (ChartSize - 2.0 * FrameThickness) / 4;
            var origin = FrameThickness;

            //fixed grid position (row,col) for each sign, clockwise from top-left starting Pisces
            var gridOrder = new[]
            {
                (ZodiacName.Pisces, 0, 0), (ZodiacName.Aries, 0, 1), (ZodiacName.Taurus, 0, 2), (ZodiacName.Gemini, 0, 3),
                (ZodiacName.Aquarius, 1, 0), (ZodiacName.Cancer, 1, 3),
                (ZodiacName.Capricorn, 2, 0), (ZodiacName.Leo, 2, 3),
                (ZodiacName.Sagittarius, 3, 0), (ZodiacName.Scorpio, 3, 1), (ZodiacName.Libra, 3, 2), (ZodiacName.Virgo, 3, 3),
            };

            foreach (var (sign, row, col) in gridOrder)
            {
                var x = origin + col * cell;
                var y = origin + row * cell;

                var houseNumber = HouseCountFrom(lagnaSign, sign);
                var planetsInSign = planetSigns.Where(kv => kv.Value == sign).Select(kv => kv.Key);
                AppendHouseCell(svg, x, y, cell, houseNumber, sign, planetsInSign);
            }
        }

        /// <summary>
        /// North Indian style: a 4x4 grid labelled by HOUSE number (rotates with Lagna), drawn as a plain
        /// square grid (not the traditional diamond geometry) - a deliberate visual simplification, see class note.
        /// </summary>
        private static void AppendNorthIndianGrid(StringBuilder svg, Dictionary<PlanetName, ZodiacName> planetSigns, ZodiacName lagnaSign)
        {
            var cell = (ChartSize - 2.0 * FrameThickness) / 4;
            var origin = FrameThickness;

            //same fixed screen positions as the South Indian grid, but labelled by house number counted from Lagna
            var gridPositions = new[] { (0, 0), (0, 1), (0, 2), (0, 3), (1, 0), (1, 3), (2, 0), (2, 3), (3, 0), (3, 1), (3, 2), (3, 3) };
            var signsInOrder = new[]
            {
                ZodiacName.Pisces, ZodiacName.Aries, ZodiacName.Taurus, ZodiacName.Gemini,
                ZodiacName.Aquarius, ZodiacName.Cancer, ZodiacName.Capricorn, ZodiacName.Leo,
                ZodiacName.Sagittarius, ZodiacName.Scorpio, ZodiacName.Libra, ZodiacName.Virgo
            };

            for (int i = 0; i < 12; i++)
            {
                var sign = signsInOrder[i];
                var (row, col) = gridPositions[i];
                var houseNumber = HouseCountFrom(lagnaSign, sign);

                var x = origin + col * cell;
                var y = origin + row * cell;
                var planetsInSign = planetSigns.Where(kv => kv.Value == sign).Select(kv => kv.Key);
                AppendHouseCell(svg, x, y, cell, houseNumber, sign, planetsInSign);
            }
        }

        /// <summary>
        /// Given a start time, end time and interval in hours, generates a CSV table (Name,Time,Location)
        /// suitable for feeding into the ML Table Generator to build datasets.
        /// </summary>
        public static string GenerateTimeListCSV(Time startTime, Time endTime, double hoursBetween)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Name,Time,Location");

            var current = startTime.GetStdDateTimeOffset();
            var end = endTime.GetStdDateTimeOffset();
            var geoLocation = startTime.GetGeoLocation();
            var step = System.TimeSpan.FromHours(hoursBetween <= 0 ? 1 : hoursBetween);

            int index = 0;
            while (current <= end)
            {
                var rowTime = new Time(current, geoLocation);
                csv.AppendLine($"Row{index},{rowTime.GetStdDateTimeOffsetText()},{geoLocation.Name()}");

                current += step;
                index++;
            }

            return csv.ToString();
        }
    }
}
