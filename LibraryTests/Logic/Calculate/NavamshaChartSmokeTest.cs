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
    }
}
