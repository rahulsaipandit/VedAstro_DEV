using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers API/FrontDesk/BirthTimeFinderAPI.cs's GET /api/FindBirthTime/EventsChart/PersonId/{personId}
    /// route - the RN/web equivalent of the Console app's "Find Birth Time - Life Predictor - Person"
    /// tool (Console/Program.cs's FindBirthTimeEventsChartPerson). Unlike /api/EventsChart, this
    /// endpoint is synchronous: it replies with the combined SVG directly, no job-poll cache.
    /// </summary>
    public class BirthTimeFinderEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public BirthTimeFinderEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<string> CreatePersonAsync(string ownerId)
        {
            var addUrl = $"/api/Calculate/AddPerson/OwnerId/{ownerId}/Location/1.3521,103.8198/Time/12:00/15/06/1990/+08:00/PersonName/ChartPerson/Gender/Male";
            var response = await _client.GetAsync(addUrl);
            response.EnsureSuccessStatusCode();
            return JObject.Parse(await response.Content.ReadAsStringAsync())["Payload"]!.ToString();
        }

        [Fact]
        public async Task BirthTimeFinder_NarrowHourRange_ReturnsCombinedSvg()
        {
            var ownerId = "btf-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var personId = await CreatePersonAsync(ownerId);

            // narrow inputs so the scan (3 candidate times) and each chart's time range (1 year) stay fast
            var url = $"/api/FindBirthTime/EventsChart/PersonId/{personId}" +
                       "?maxWidth=400&precisionInHours=1&startHour=11:00&endHour=13:00&endDate=15/06/1991";

            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            Assert.Contains("svg", response.Content.Headers.ContentType?.MediaType ?? "", StringComparison.OrdinalIgnoreCase);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("<svg", body, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("MultipleDasa", body);
        }

        [Fact]
        public async Task BirthTimeFinder_UnknownPersonId_ReturnsFailEnvelope()
        {
            var response = await _client.GetAsync("/api/FindBirthTime/EventsChart/PersonId/does-not-exist");
            response.EnsureSuccessStatusCode(); // app-level failure, not a transport-level error

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Fail", json["Status"]!.ToString());
        }
    }
}
