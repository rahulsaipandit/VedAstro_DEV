using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Diagnoses a report that the Horoscope page's two Birth Charts (RasiD1 and NavamshaD9)
    /// render identically - checks the real HTTP path end to end (URL parsing -> reflection
    /// dispatch -> ChartType enum parsing -> SVG generation), not just the C# method directly.
    /// </summary>
    public class SkyChartAndIndianChartEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public SkyChartAndIndianChartEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private const string BirthTimeUrlSegment = "Location/Bhopal,MadhyaPradesh,India/Time/16:20/26/01/1975/+05:30";

        [Fact]
        public async Task SouthIndianChart_RasiD1AndNavamshaD9_ReturnDifferentSvg()
        {
            var rasiUrl = $"/api/Calculate/SouthIndianChart/{BirthTimeUrlSegment}/ChartType/RasiD1/Ayanamsa/Raman";
            var navamshaUrl = $"/api/Calculate/SouthIndianChart/{BirthTimeUrlSegment}/ChartType/NavamshaD9/Ayanamsa/Raman";

            var rasiResponse = await _client.GetAsync(rasiUrl);
            var navamshaResponse = await _client.GetAsync(navamshaUrl);

            rasiResponse.EnsureSuccessStatusCode();
            navamshaResponse.EnsureSuccessStatusCode();

            var rasiSvg = await rasiResponse.Content.ReadAsStringAsync();
            var navamshaSvg = await navamshaResponse.Content.ReadAsStringAsync();

            Assert.NotEqual(rasiSvg, navamshaSvg);
        }
    }
}
