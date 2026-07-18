using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers API/FrontDesk/GeneralAPI.cs. Only the MapFallback route is exercised here -
    /// /api/Home, /api/GetVedAstroJSHash and /api/favicon.ico all reach out live to
    /// URL.WebStable (vedastro.org) with no offline/local fallback, so they aren't meaningfully
    /// testable in this sandboxed, Testcontainers-only environment (see final report TODO).
    /// </summary>
    public class GeneralEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public GeneralEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UnmatchedRoute_FallsBackToGracefulFailEnvelope()
        {
            var response = await _client.GetAsync("/api/ThisRouteDoesNotExistAnywhere");

            // MapFallback is registered last so it only catches truly unmatched routes - it
            // always replies 200 with a Fail envelope rather than a raw 404, per GeneralAPI.cs.
            response.EnsureSuccessStatusCode();
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Fail", json["Status"]!.ToString());
            Assert.Contains("APIBuilder", json["Payload"]!.ToString());
        }
    }
}
