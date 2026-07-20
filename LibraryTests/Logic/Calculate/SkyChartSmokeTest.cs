using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VedAstro.Library;

namespace VedAstro.Library.Tests
{
    [TestClass]
    public class SkyChartSmokeTest
    {
        [TestMethod]
        public async Task SkyChartHasNoNestedSvgOrClassAttributes()
        {
            var time = new Time("16:20 26/01/1975 +05:30", GeoLocation.Bangalore);
            var svg = await Calculate.SkyChart(time);

            var svgTagCount = System.Text.RegularExpressions.Regex.Matches(svg, "<svg\\b").Count;
            var hasClassAttr = svg.Contains("class=");

            Assert.AreEqual(1, svgTagCount, $"Expected exactly 1 <svg> tag (no nested svgs), got {svgTagCount}");
            Assert.IsFalse(hasClassAttr, "Unexpected class= attribute in output SVG");
        }
    }
}
