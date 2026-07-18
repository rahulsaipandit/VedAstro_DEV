using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>Covers API/FrontDesk/MatchAPI.cs's GET /api/FindMatch/PersonId/{personId} route.</summary>
    public class MatchEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public MatchEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<string> AddPersonAsync(string ownerId, string name, string gender, string birthTimeUrlSegment)
        {
            var addUrl = $"/api/Calculate/AddPerson/OwnerId/{ownerId}/{birthTimeUrlSegment}/PersonName/{name}/Gender/{gender}";
            var response = await _client.GetAsync(addUrl);
            response.EnsureSuccessStatusCode();
            return JObject.Parse(await response.Content.ReadAsStringAsync())["Payload"]!.ToString();
        }

        [Fact]
        public async Task FindMatch_GoldenPath_ReturnsPassWithScoredArray()
        {
            var ownerId = "match-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            // MatchAPI cross-checks the input person against every other person in the DB (across
            // all owners), regardless of KutaScore result - a golden-path call just needs it to
            // execute end-to-end and reply with a well-shaped Pass envelope.
            var malePersonId = await AddPersonAsync(ownerId, "MatchMale", "Male", "Location/1.3521,103.8198/Time/12:00/15/06/1988/+08:00");
            await AddPersonAsync(ownerId, "MatchFemale", "Female", "Location/1.3521,103.8198/Time/09:30/22/09/1990/+08:00");

            var response = await _client.GetAsync($"/api/FindMatch/PersonId/{malePersonId}");
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            Assert.IsType<JArray>(json["Payload"]); // matches below the 70-point Kuta threshold are filtered out, an empty array is still a valid Pass
        }
    }
}
