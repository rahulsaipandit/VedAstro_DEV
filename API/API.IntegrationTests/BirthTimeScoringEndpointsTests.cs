using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers API/FrontDesk/BirthTimeScoringAPI.cs - the questionnaire-driven BTR scoring
    /// endpoints layered on top of the same candidate sweep BirthTimeFinderAPI uses (see
    /// docs/BirthTimeFinder.md).
    /// </summary>
    public class BirthTimeScoringEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public BirthTimeScoringEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<string> CreatePersonAsync(string ownerId)
        {
            var addUrl = $"/api/Calculate/AddPerson/OwnerId/{ownerId}/Location/1.3521,103.8198/Time/12:00/15/06/1990/+08:00/PersonName/ScoringPerson/Gender/Male";
            var response = await _client.GetAsync(addUrl);
            response.EnsureSuccessStatusCode();
            return JObject.Parse(await response.Content.ReadAsStringAsync())["Payload"]!.ToString();
        }

        [Fact]
        public async Task GetQuestions_ReturnsFullQuestionBank()
        {
            var response = await _client.GetAsync("/api/FindBirthTime/Questions");
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());

            var questions = (JArray)json["Payload"]!;
            Assert.Equal(95, questions.Count);
            Assert.All(questions, q =>
            {
                Assert.False(string.IsNullOrWhiteSpace(q["Text"]?.ToString()));
                Assert.False(string.IsNullOrWhiteSpace(q["Category"]?.ToString()));
            });
        }

        [Fact]
        public async Task ScoreCandidates_NarrowHourRange_ReturnsRankedCandidatesDescending()
        {
            var ownerId = "bts-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var personId = await CreatePersonAsync(ownerId);

            var requestBody = new JObject
            {
                ["answers"] = new JArray
                {
                    new JObject { ["questionId"] = 37, ["answer"] = "StronglyYes" }, // "Is your marriage happy?"
                    new JObject { ["questionId"] = 49, ["answer"] = "Yes" },          // "satisfied with physical health?"
                },
                ["precisionInHours"] = 1,
                ["startHour"] = "11:00",
                ["endHour"] = "13:00"
            };

            var content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync($"/api/FindBirthTime/Score/PersonId/{personId}", content);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());

            var ranked = (JArray)json["Payload"]!;
            Assert.Equal(3, ranked.Count); // 11:00, 12:00, 13:00 at 1hr precision

            for (var i = 1; i < ranked.Count; i++)
            {
                var prev = ranked[i - 1]["ConfidencePercent"]!.Value<double>();
                var curr = ranked[i]["ConfidencePercent"]!.Value<double>();
                Assert.True(prev >= curr, "Ranked candidates must be sorted by descending confidence");
            }
        }

        [Fact]
        public async Task ScoreCandidates_UnknownPersonId_ReturnsFailEnvelope()
        {
            var content = new StringContent(new JObject { ["answers"] = new JArray() }.ToString(), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/FindBirthTime/Score/PersonId/does-not-exist", content);
            response.EnsureSuccessStatusCode(); // app-level failure, not a transport-level error

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Fail", json["Status"]!.ToString());
        }
    }
}
