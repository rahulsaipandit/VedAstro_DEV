using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using VedAstro.Library;
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
        public async Task Calculate_VedicBirthDate_ReturnsPassWithMatchingTithi()
        {
            var url = $"/api/Calculate/VedicBirthDate/{BirthTimeUrlSegment}/Year/1995";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());

            var payload = json["Payload"]!;
            // StdTime is "HH:mm dd/MM/yyyy zzz" - the matched date must fall in the requested year
            var datePart = payload["StdTime"]!.ToString().Split(' ')[1]; // "dd/MM/yyyy"
            Assert.Equal("1995", datePart.Split('/')[2]);
            Assert.False(string.IsNullOrWhiteSpace(payload["Location"]!["Name"]!.ToString()));
        }

        private const string BangaloreLocationUrlSegment = "Location/Bangalore/Coordinates/12.9716,77.5946";

        [Fact]
        public async Task Calculate_FestivalDate_Diwali2023_ReturnsPassWithMatchingYear()
        {
            var url = $"/api/Calculate/FestivalDate/FestivalName/Diwali/Year/2023/{BangaloreLocationUrlSegment}";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());

            var datePart = json["Payload"]!["StdTime"]!.ToString().Split(' ')[1]; // "dd/MM/yyyy"
            Assert.Equal("2023", datePart.Split('/')[2]);
        }

        [Fact]
        public async Task Calculate_FestivalCalendar_2024_ReturnsPassWithEveryFestival()
        {
            var url = $"/api/Calculate/FestivalCalendar/Year/2024/{BangaloreLocationUrlSegment}";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());

            var payload = (JObject)json["Payload"]!;
            Assert.Equal(Enum.GetValues(typeof(FestivalName)).Length, payload.Properties().Count());
            Assert.NotNull(payload["Diwali"]);
            Assert.NotNull(payload["Diwali"]!["StdTime"]);
        }

        [Fact]
        public async Task Calculate_ChoghadiyaPeriods_ReturnsPassWithSixteenPeriods()
        {
            var url = $"/api/Calculate/ChoghadiyaPeriods/{BirthTimeUrlSegment}";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());

            var payload = (JArray)json["Payload"]!;
            Assert.Equal(16, payload.Count);
            Assert.False(string.IsNullOrWhiteSpace(payload[0]["Name"]?.ToString()));
            Assert.False(string.IsNullOrWhiteSpace(payload[0]["Quality"]?.ToString()));
        }

        [Fact]
        public async Task Calculate_DailyPanchang_ReturnsPassWithCoreLimbs()
        {
            var url = $"/api/Calculate/DailyPanchang/{BirthTimeUrlSegment}";
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());

            var payload = json["Payload"]!;
            Assert.NotNull(payload["Tithi"]);
            Assert.NotNull(payload["LunarMonth"]);
            Assert.NotNull(payload["Vara"]);
            Assert.NotNull(payload["Nakshatra"]);
            Assert.NotNull(payload["Yoga"]);
            Assert.NotNull(payload["Karana"]);
            Assert.NotNull(payload["Sunrise"]);
            Assert.NotNull(payload["Sunset"]);
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
