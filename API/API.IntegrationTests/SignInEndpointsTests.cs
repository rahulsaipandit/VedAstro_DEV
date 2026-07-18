using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers API/FrontDesk/SignInAPI.cs. Real OAuth can't be exercised here (no real Google/
    /// Facebook token), so this just proves the routes exist, accept a call, and fail gracefully
    /// (well-formed "Fail" envelope) rather than 404ing or leaking an unhandled exception.
    /// </summary>
    public class SignInEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public SignInEndpointsTests(ApiWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SignInGoogle_WithFakeToken_FailsGracefully()
        {
            var response = await _client.GetAsync("/api/SignInGoogle/Token/this-is-not-a-real-google-jwt");

            response.EnsureSuccessStatusCode(); // never a raw 404/500 - SignInAPI.cs catches and replies Fail
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Fail", json["Status"]!.ToString());
            Assert.Equal("Login Failed", json["Payload"]!.ToString());
        }

        [Fact]
        public async Task SignInFacebook_WithFakeToken_FailsGracefully()
        {
            var response = await _client.GetAsync("/api/SignInFacebook/Token/this-is-not-a-real-facebook-token");

            response.EnsureSuccessStatusCode();
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Fail", json["Status"]!.ToString());
        }

        [Fact]
        public async Task FacebookDeauthorize_ReturnsPassEnvelope()
        {
            var response = await _client.GetAsync("/api/FacebookDeauthorize");

            response.EnsureSuccessStatusCode();
            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
        }
    }
}
