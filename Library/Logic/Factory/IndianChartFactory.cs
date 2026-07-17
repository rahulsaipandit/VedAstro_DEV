using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VedAstro.Library
{
    /// <summary>
    /// Renders simple South/North Indian style Kundali (birth chart) SVGs.
    /// Reconstructed from scratch - see Library/Logic/Calculate/CoreTime.cs header note.
    /// NOTE: these are functionally-correct-but-simplified renderings (basic grid/diamond with
    /// sign/house numbers and planet abbreviations) - not a pixel-faithful reproduction of a
    /// traditional hand-drawn chart. Good enough to verify data locally; revisit for production visuals.
    /// </summary>
    public partial class Calculate
    {
        private const int ChartSize = 400;

        /// <summary>Gets sky chart at a given time. SVG image, URL can be used like an image source link.</summary>
        public static async System.Threading.Tasks.Task<string> SkyChart(Time time) =>
            await SkyChartFactory.GenerateChart(time, ChartSize, ChartSize);

        /// <summary>Creates a Kundali chart (D1 to D20) in South Indian style. SVG image.</summary>
        public static string SouthIndianChart(Time time, ChartType chartType) => GenerateIndianChartSvg(time, chartType, northIndianStyle: false);

        /// <summary>Creates a Kundali chart (D1 to D20) in North Indian style. SVG image.</summary>
        public static string NorthIndianChart(Time time, ChartType chartType) => GenerateIndianChartSvg(time, chartType, northIndianStyle: true);

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

        private static string PlanetAbbreviation(PlanetName planet) => planet.Name switch
        {
            PlanetName.PlanetNameEnum.Sun => "Su",
            PlanetName.PlanetNameEnum.Moon => "Mo",
            PlanetName.PlanetNameEnum.Mars => "Ma",
            PlanetName.PlanetNameEnum.Mercury => "Me",
            PlanetName.PlanetNameEnum.Jupiter => "Ju",
            PlanetName.PlanetNameEnum.Venus => "Ve",
            PlanetName.PlanetNameEnum.Saturn => "Sa",
            PlanetName.PlanetNameEnum.Rahu => "Ra",
            PlanetName.PlanetNameEnum.Ketu => "Ke",
            _ => planet.Name.ToString().Substring(0, System.Math.Min(2, planet.Name.ToString().Length))
        };

        private static string GenerateIndianChartSvg(Time time, ChartType chartType, bool northIndianStyle)
        {
            var planetSigns = GetPlanetSignsForChartType(time, chartType);
            var lagnaSign = LagnaSignName(time);

            var svg = new StringBuilder();
            svg.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {ChartSize} {ChartSize}\" font-family=\"sans-serif\" font-size=\"11\">");
            svg.Append($"<rect x=\"0\" y=\"0\" width=\"{ChartSize}\" height=\"{ChartSize}\" fill=\"white\" stroke=\"black\" stroke-width=\"2\"/>");

            if (northIndianStyle)
            {
                AppendNorthIndianGrid(svg, planetSigns, lagnaSign);
            }
            else
            {
                AppendSouthIndianGrid(svg, planetSigns);
            }

            svg.Append("</svg>");
            return svg.ToString();
        }

        /// <summary>Fixed 4x4 grid, signs never rotate (South Indian style). Starts at Pisces top-left, clockwise.</summary>
        private static void AppendSouthIndianGrid(StringBuilder svg, Dictionary<PlanetName, ZodiacName> planetSigns)
        {
            const int cell = ChartSize / 4;

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
                var x = col * cell;
                var y = row * cell;
                svg.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{cell}\" height=\"{cell}\" fill=\"none\" stroke=\"black\"/>");
                svg.Append($"<text x=\"{x + 4}\" y=\"{y + 12}\" font-weight=\"bold\">{sign}</text>");

                var planetsInSign = planetSigns.Where(kv => kv.Value == sign).Select(kv => PlanetAbbreviation(kv.Key));
                svg.Append($"<text x=\"{x + 4}\" y=\"{y + 28}\">{string.Join(" ", planetsInSign)}</text>");
            }
        }

        /// <summary>
        /// Simplified North Indian style: a 4x4 grid labelled by HOUSE number (rotates with Lagna),
        /// rather than the traditional diamond geometry - a deliberate visual simplification, see class note.
        /// </summary>
        private static void AppendNorthIndianGrid(StringBuilder svg, Dictionary<PlanetName, ZodiacName> planetSigns, ZodiacName lagnaSign)
        {
            const int cell = ChartSize / 4;

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

                var x = col * cell;
                var y = row * cell;
                svg.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{cell}\" height=\"{cell}\" fill=\"none\" stroke=\"black\"/>");
                svg.Append($"<text x=\"{x + 4}\" y=\"{y + 12}\" font-weight=\"bold\">H{houseNumber}</text>");

                var planetsInSign = planetSigns.Where(kv => kv.Value == sign).Select(kv => PlanetAbbreviation(kv.Key));
                svg.Append($"<text x=\"{x + 4}\" y=\"{y + 28}\">{string.Join(" ", planetsInSign)}</text>");
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
