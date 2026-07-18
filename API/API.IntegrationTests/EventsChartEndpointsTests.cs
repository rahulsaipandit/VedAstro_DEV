using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers API/FrontDesk/EventsChartAPI.cs's main GET /api/EventsChart/{*settingsUrl} route,
    /// including its cache mechanism (Library/Logic/AzureCache.cs's CacheExecute):
    /// - 1st call for a never-seen chart id: fires off background compute, replies immediately
    ///   with the "Call-Status: Running" header (no body yet).
    /// - poll same URL until the compute finishes and the chart is on disk -> "Call-Status: Pass"
    ///   with the SVG body, and the chart file now exists in the temp ChartCacheDirectory.
    /// - a further call for the exact same settings hits the cache directly ("Call-Status: Pass"
    ///   immediately, no "Running" step) - proving CacheExecute's cache-hit path.
    /// </summary>
    public class EventsChartEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly ApiWebApplicationFactory _factory;

        public EventsChartEndpointsTests(ApiWebApplicationFactory factory)
        {
            _factory = factory;
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
        public async Task EventsChart_FirstCallRuns_SecondCallServedFromCache()
        {
            var ownerId = "chart-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var personId = await CreatePersonAsync(ownerId);

            // small 1-month range with a coarse days-per-pixel so the background compute finishes quickly
            var settingsUrl = $"{personId}/Start/00:00/01/01/2020/End/00:00/01/02/2020/+08:00/1/Gochara/General";

            // 1st call: chart not cached yet -> "Running"
            var firstResponse = await _client.GetAsync($"/api/EventsChart/{settingsUrl}");
            firstResponse.EnsureSuccessStatusCode();
            Assert.Equal("Running", firstResponse.Headers.GetValues("Call-Status").First());

            // poll until the fire-and-forget background compute finishes and the SVG is cached
            string? callStatus = null;
            string? body = null;
            for (var i = 0; i < 50 && callStatus != "Pass"; i++)
            {
                await Task.Delay(200);
                var pollResponse = await _client.GetAsync($"/api/EventsChart/{settingsUrl}");
                pollResponse.EnsureSuccessStatusCode();
                callStatus = pollResponse.Headers.TryGetValues("Call-Status", out var values) ? values.First() : null;
                body = await pollResponse.Content.ReadAsStringAsync();
            }

            Assert.Equal("Pass", callStatus);
            Assert.False(string.IsNullOrWhiteSpace(body));
            Assert.Contains("<svg", body, StringComparison.OrdinalIgnoreCase);

            // the chart file should now be sitting in the isolated temp ChartCacheDirectory
            Assert.True(Directory.Exists(_factory.ChartCacheDirectory));
            var cachedFiles = Directory.EnumerateFiles(_factory.ChartCacheDirectory)
                .Where(f => !f.EndsWith(".meta.json"))
                .ToList();
            Assert.NotEmpty(cachedFiles);

            // a further call for the identical settings must be an immediate cache hit - no "Running" step
            var cachedResponse = await _client.GetAsync($"/api/EventsChart/{settingsUrl}");
            cachedResponse.EnsureSuccessStatusCode();
            Assert.Equal("Pass", cachedResponse.Headers.GetValues("Call-Status").First());
            var cachedBody = await cachedResponse.Content.ReadAsStringAsync();
            Assert.Contains("<svg", cachedBody, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SavedChartNameList_ReturnsPassEnvelope()
        {
            var ownerId = "chart-owner-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var personId = await CreatePersonAsync(ownerId);

            var response = await _client.GetAsync($"/api/SavedChartNameList/{personId}");
            response.EnsureSuccessStatusCode();

            var json = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Pass", json["Status"]!.ToString());
            Assert.IsType<JArray>(json["Payload"]); // no charts generated for this fresh person yet - still Pass
        }
    }
}
