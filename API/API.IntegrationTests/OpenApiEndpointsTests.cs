using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers API/FrontDesk/OpenAPI.cs: the metadata endpoints and the generic
    /// `Calculate/{calculatorName}/{*fullParamString}` reflection dispatcher (for a non-chat
    /// calculator - HoroscopePredictionNames(Time) - plus person-list retrieval through the same
    /// dispatcher, which is exercised more thoroughly in PersonEndpointsTests).
    /// </summary>
    public class OpenApiEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public OpenApiEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private const string BirthTimeUrlSegment = "Location/1.3521,103.8198/Time/12:00/15/06/1990/+08:00";

        [Fact]
        public async Task ListAllCalls_ReturnsPassWithNonEmptyArray()
        {
            var response = await _client.GetAsync("/api/ListAllCalls");
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            Assert.True(((JArray)json["Payload"]!).Count > 0);
        }

        [Fact]
        public async Task AllCallsHash_ReturnsPassWithNonEmptyHash()
        {
            var response = await _client.GetAsync("/api/AllCallsHash");
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            Assert.False(string.IsNullOrWhiteSpace(json["Payload"]!.ToString()));
        }

        [Fact]
        public async Task Calculate_HoroscopePredictionNames_NonChatCalculator_ReturnsPassWithList()
        {
            var url = $"/api/Calculate/HoroscopePredictionNames/{BirthTimeUrlSegment}";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            Assert.IsType<JArray>(json["Payload"]);
        }

        [Fact]
        public async Task Calculate_GetPersonList_ThroughReflectionDispatcher_ReturnsPassWithArray()
        {
            var ownerId = "openapi-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var response = await _client.GetAsync($"/api/Calculate/GetPersonList/OwnerId/{ownerId}");
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            Assert.IsType<JArray>(json["Payload"]); // empty list for a fresh owner id, still a valid Pass
        }

        [Fact]
        public async Task Calculate_UnknownCalculatorName_ReturnsFailEnvelope_NotUnhandledException()
        {
            var response = await _client.GetAsync("/api/Calculate/ThisCalculatorDoesNotExist/Foo/Bar");

            // app-level failure, not a transport-level error - APITools.FailMessageJson always replies 200
            response.EnsureSuccessStatusCode();
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Fail", json["Status"]!.ToString());
        }
    }
}
