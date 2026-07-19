using System;
using System.Net.Http;
using System.Net.Http.Json;
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

        /// <summary>Covers the live-computed one-on-one report endpoint used by WebsiteNative's Match/Report screen.</summary>
        [Fact]
        public async Task GetMatchReport_GoldenPath_ReturnsPassWithKutaScore()
        {
            var ownerId = "match-report-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var maleId = await AddPersonAsync(ownerId, "ReportMale", "Male", "Location/1.3521,103.8198/Time/12:00/15/06/1988/+08:00");
            var femaleId = await AddPersonAsync(ownerId, "ReportFemale", "Female", "Location/1.3521,103.8198/Time/09:30/22/09/1990/+08:00");

            var response = await _client.GetAsync($"/api/GetMatchReport/MaleId/{maleId}/FemaleId/{femaleId}");
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            var payload = (JObject)json["Payload"]!;
            Assert.Equal("ReportMale", payload["Male"]!["Name"]!.ToString());
            Assert.Equal("ReportFemale", payload["Female"]!["Name"]!.ToString());
            Assert.NotNull(payload["KutaScore"]);
            Assert.IsType<JArray>(payload["PredictionList"]);
        }

        /// <summary>
        /// Covers the new saved-match-reports persistence (SavedMatchReportEntity/POST
        /// /api/SaveMatchReport/GET /api/GetMatchReportList/OwnerId/{ownerId}) backing
        /// WebsiteNative's Match/Saved screen - a genuinely new feature, not a straight port.
        /// </summary>
        [Fact]
        public async Task SaveMatchReport_ThenGetMatchReportList_ReturnsSavedReportWithNotes()
        {
            var ownerId = "saved-match-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var maleId = await AddPersonAsync(ownerId, "SavedMale", "Male", "Location/1.3521,103.8198/Time/12:00/15/06/1988/+08:00");
            var femaleId = await AddPersonAsync(ownerId, "SavedFemale", "Female", "Location/1.3521,103.8198/Time/09:30/22/09/1990/+08:00");

            var saveResponse = await _client.PostAsJsonAsync("/api/SaveMatchReport", new
            {
                OwnerId = ownerId,
                MaleId = maleId,
                FemaleId = femaleId,
                Notes = "Met at a wedding"
            });
            saveResponse.EnsureSuccessStatusCode();
            var saveJson = JObject.Parse(await saveResponse.Content.ReadAsStringAsync());
            Assert.Equal("Pass", saveJson["Status"]!.ToString());

            var listResponse = await _client.GetAsync($"/api/GetMatchReportList/OwnerId/{ownerId}");
            listResponse.EnsureSuccessStatusCode();
            var listJson = JObject.Parse(await listResponse.Content.ReadAsStringAsync());
            Assert.Equal("Pass", listJson["Status"]!.ToString());

            var payload = (JArray)listJson["Payload"]!;
            Assert.Single(payload);
            var report = (JObject)payload[0]!;
            Assert.Equal("SavedMale", report["Male"]!["Name"]!.ToString());
            Assert.Equal("SavedFemale", report["Female"]!["Name"]!.ToString());
            Assert.Equal("Met at a wedding", report["Notes"]!.ToString());

            // Re-saving the same couple updates Notes rather than creating a second row.
            var resaveResponse = await _client.PostAsJsonAsync("/api/SaveMatchReport", new
            {
                OwnerId = ownerId,
                MaleId = maleId,
                FemaleId = femaleId,
                Notes = "Updated note"
            });
            resaveResponse.EnsureSuccessStatusCode();

            var listResponse2 = await _client.GetAsync($"/api/GetMatchReportList/OwnerId/{ownerId}");
            var listJson2 = JObject.Parse(await listResponse2.Content.ReadAsStringAsync());
            var payload2 = (JArray)listJson2["Payload"]!;
            Assert.Single(payload2);
            Assert.Equal("Updated note", payload2[0]!["Notes"]!.ToString());
        }
    }
}
