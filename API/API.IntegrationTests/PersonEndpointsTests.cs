using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers API/FrontDesk/PersonAPI.cs: AddPerson/GetPersonList/GetPersonListHash (reached only
    /// through the Calculate/{calculatorName} reflection dispatcher) plus the 3 direct routes
    /// (GetPerson/UpdatePerson/DeletePerson) that ViewComponents/Code/API/PersonTools.cs calls.
    /// </summary>
    public class PersonEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public PersonEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        // coordinates form ("lat,long") skips the AddressToGeoLocation geocoding API call, and a
        // non-"+00:00" offset skips the GeoLocationToTimezone API call - both would otherwise hit
        // the network using placeholder API keys and fail in this offline test environment.
        private const string BirthTimeUrlSegment = "Location/1.3521,103.8198/Time/12:00/15/06/1990/+08:00";

        [Fact]
        public async Task AddPerson_ThenGetPersonList_GoldenPath()
        {
            var ownerId = "test-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var addUrl = $"/api/Calculate/AddPerson/OwnerId/{ownerId}/{BirthTimeUrlSegment}/PersonName/JohnDoe/Gender/Male";
            var addResponse = await _client.GetAsync(addUrl);
            addResponse.EnsureSuccessStatusCode();

            var addJson = JObject.Parse(await addResponse.Content.ReadAsStringAsync());
            Assert.True("Pass" == addJson["Status"]!.ToString(), addJson.ToString());
            var personId = addJson["Payload"]!.ToString();
            Assert.False(string.IsNullOrWhiteSpace(personId));

            var listUrl = $"/api/Calculate/GetPersonList/OwnerId/{ownerId}";
            var listResponse = await _client.GetAsync(listUrl);
            listResponse.EnsureSuccessStatusCode();

            var listJson = JObject.Parse(await listResponse.Content.ReadAsStringAsync());
            Assert.Equal("Pass", listJson["Status"]!.ToString());
            var payloadArray = (JArray)listJson["Payload"]!;
            Assert.Contains(payloadArray, p => p["PersonId"]!.ToString() == personId);
            Assert.Equal("JohnDoe", payloadArray.First(p => p["PersonId"]!.ToString() == personId)["Name"]!.ToString());
        }

        [Fact]
        public async Task GetPersonListHash_ChangesAfterAddingPerson()
        {
            var ownerId = "test-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var hashBeforeResponse = await _client.GetAsync($"/api/Calculate/GetPersonListHash/OwnerId/{ownerId}");
            hashBeforeResponse.EnsureSuccessStatusCode();
            var hashBefore = JObject.Parse(await hashBeforeResponse.Content.ReadAsStringAsync())["Payload"]!.ToString();

            var addUrl = $"/api/Calculate/AddPerson/OwnerId/{ownerId}/{BirthTimeUrlSegment}/PersonName/JaneDoe/Gender/Female";
            (await _client.GetAsync(addUrl)).EnsureSuccessStatusCode();

            var hashAfterResponse = await _client.GetAsync($"/api/Calculate/GetPersonListHash/OwnerId/{ownerId}");
            hashAfterResponse.EnsureSuccessStatusCode();
            var hashAfter = JObject.Parse(await hashAfterResponse.Content.ReadAsStringAsync())["Payload"]!.ToString();

            Assert.NotEqual(hashBefore, hashAfter);
        }

        [Fact]
        public async Task GetPerson_UpdatePerson_DeletePerson_GoldenPath()
        {
            var ownerId = "test-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var addUrl = $"/api/Calculate/AddPerson/OwnerId/{ownerId}/{BirthTimeUrlSegment}/PersonName/AliceSmith/Gender/Female";
            var addResponse = await _client.GetAsync(addUrl);
            addResponse.EnsureSuccessStatusCode();
            var personId = JObject.Parse(await addResponse.Content.ReadAsStringAsync())["Payload"]!.ToString();

            // GET /api/GetPerson/OwnerId/{ownerId}/PersonId/{personId}
            var getResponse = await _client.GetAsync($"/api/GetPerson/OwnerId/{ownerId}/PersonId/{personId}");
            getResponse.EnsureSuccessStatusCode();
            var getJson = JObject.Parse(await getResponse.Content.ReadAsStringAsync());
            Assert.Equal("Pass", getJson["Status"]!.ToString());
            var personPayload = (JObject)getJson["Payload"]!;
            Assert.Equal("AliceSmith", personPayload["Name"]!.ToString());

            // POST /api/UpdatePerson - reuse the Person.ToJson() shape we just got back, tweak the
            // name. NOTE: HttpClient.PostAsJsonAsync<JObject> would use System.Text.Json to
            // serialize the Newtonsoft JObject as a plain .NET object (wrong wire shape entirely,
            // not the JSON it represents) - build the request body with Newtonsoft explicitly instead.
            personPayload["Name"] = "AliceSmithUpdated";
            var updateContent = new StringContent(personPayload.ToString(), Encoding.UTF8, "application/json");
            var updateResponse = await _client.PostAsync("/api/UpdatePerson", updateContent);
            updateResponse.EnsureSuccessStatusCode();
            var updateJson = JObject.Parse(await updateResponse.Content.ReadAsStringAsync());
            Assert.Equal("Pass", updateJson["Status"]!.ToString());

            var getAfterUpdateResponse = await _client.GetAsync($"/api/GetPerson/OwnerId/{ownerId}/PersonId/{personId}");
            var afterUpdateJson = JObject.Parse(await getAfterUpdateResponse.Content.ReadAsStringAsync());
            Assert.Equal("AliceSmithUpdated", ((JObject)afterUpdateJson["Payload"]!)["Name"]!.ToString());

            // GET /api/DeletePerson/OwnerId/{ownerId}/PersonId/{personId}
            var deleteResponse = await _client.GetAsync($"/api/DeletePerson/OwnerId/{ownerId}/PersonId/{personId}");
            deleteResponse.EnsureSuccessStatusCode();
            var deleteJson = JObject.Parse(await deleteResponse.Content.ReadAsStringAsync());
            Assert.Equal("Pass", deleteJson["Status"]!.ToString());

            var listAfterDeleteResponse = await _client.GetAsync($"/api/Calculate/GetPersonList/OwnerId/{ownerId}");
            var listAfterDeleteJson = JObject.Parse(await listAfterDeleteResponse.Content.ReadAsStringAsync());
            var payloadArray = (JArray)listAfterDeleteJson["Payload"]!;
            Assert.DoesNotContain(payloadArray, p => p["PersonId"]!.ToString() == personId);
        }
    }
}
