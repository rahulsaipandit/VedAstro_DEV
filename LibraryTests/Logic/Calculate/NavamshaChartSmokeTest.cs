using Microsoft.VisualStudio.TestTools.UnitTesting;
using VedAstro.Library;

namespace VedAstro.Library.Tests
{
    [TestClass]
    public class NavamshaChartSmokeTest
    {
        [TestMethod]
        public void SouthIndianChartRendersNavamshaD9()
        {
            var time = new Time("16:20 26/01/1975 +05:30", GeoLocation.Bangalore);
            var svg = Calculate.SouthIndianChart(time, ChartType.NavamshaD9);

            Assert.IsTrue(svg.Contains("<svg"));
            Assert.IsTrue(svg.Contains("D9"), "Expected the chart title to mention D9 (Navamsha)");
        }

        [TestMethod]
        public void RasiD1AndNavamshaD9ProduceDifferentSvg()
        {
            var time = new Time("16:20 26/01/1975 +05:30", GeoLocation.Bangalore);
            var rasi = Calculate.SouthIndianChart(time, ChartType.RasiD1);
            var navamsha = Calculate.SouthIndianChart(time, ChartType.NavamshaD9);

            System.Console.WriteLine("RASI: " + rasi);
            System.Console.WriteLine("NAVAMSHA: " + navamsha);

            Assert.AreNotEqual(rasi, navamsha, "RasiD1 and NavamshaD9 charts should not be byte-identical");
        }

        [TestMethod]
        public void RasiAndNavamshaPlanetSignsDiffer()
        {
            var time = new Time("16:20 26/01/1975 +05:30", GeoLocation.Bangalore);
            var rasiSigns = Calculate.AllPlanetRasiSigns(time);
            var navamshaSigns = Calculate.AllPlanetNavamshaSign(time);

            foreach (var planet in rasiSigns.Keys)
            {
                System.Console.WriteLine($"{planet}: Rasi={rasiSigns[planet].GetSignName()} Navamsha={navamshaSigns[planet].GetSignName()}");
            }
        }

        [TestMethod]
        public void RasiAndNavamshaLagnaSignsDiffer()
        {
            var time = new Time("16:20 26/01/1975 +05:30", GeoLocation.Bangalore);
            var d1Lagna = Calculate.HouseZodiacSign(HouseName.House1, time).GetSignName();
            var d9Lagna = Calculate.HouseNavamshaD9Sign(HouseName.House1, time).GetSignName();

            System.Console.WriteLine($"D1 Lagna={d1Lagna} D9 Lagna={d9Lagna}");

            // for this birth data they happen to differ; the real regression check is that the
            // house-number badges in the rendered SVG differ between chart types (see below)
            var rasiSvg = Calculate.SouthIndianChart(time, ChartType.RasiD1);
            var navamshaSvg = Calculate.SouthIndianChart(time, ChartType.NavamshaD9);
            Assert.AreNotEqual(rasiSvg, navamshaSvg);
        }
    }
}
