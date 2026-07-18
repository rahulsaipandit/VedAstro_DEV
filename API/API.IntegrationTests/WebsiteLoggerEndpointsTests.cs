using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VedAstro.Data.Repositories;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Covers API/FrontDesk/WebsiteLoggerAPI.cs's LogError/LogDebug routes. Neither writes a
    /// Pass/Fail envelope back (they just persist and implicitly 200), so the only meaningful
    /// assertion is that the row is actually queryable back out afterwards - done here via the
    /// repository resolved straight from the factory's DI container.
    /// </summary>
    public class WebsiteLoggerEndpointsTests : IClassFixture<ApiWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly ApiWebApplicationFactory _factory;

        public WebsiteLoggerEndpointsTests(ApiWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task LogError_PersistsRow_QueryableAfterwards()
        {
            var userId = "log-error-user-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var localTime = DateTime.UtcNow.ToString("O");

            var body = new
            {
                UserId = userId,
                LocalTime = localTime,
                Url = "https://vedastro.org/test-page",
                Message = "integration test error message",
                Stack = "at TestMethod()",
                UserAgent = "IntegrationTests/1.0"
            };

            var response = await _client.PostAsJsonAsync("/api/LogError", body);
            response.EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IWebsiteErrorLogRepository>();
            var rows = await repo.GetByPartitionKeyAsync(userId);

            Assert.Single(rows);
            Assert.Equal("integration test error message", rows.First().ErrorMessage);
        }

        [Fact]
        public async Task LogDebug_PersistsRow_QueryableAfterwards()
        {
            var userId = "log-debug-user-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var localTime = DateTime.UtcNow.ToString("O");

            var body = new
            {
                UserId = userId,
                LocalTime = localTime,
                Url = "https://vedastro.org/test-page",
                Message = "integration test debug message",
                UserAgent = "IntegrationTests/1.0"
            };

            var response = await _client.PostAsJsonAsync("/api/LogDebug", body);
            response.EnsureSuccessStatusCode();

            using var scope = _factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IWebsiteDebugLogRepository>();
            var rows = await repo.GetByPartitionKeyAsync(userId);

            Assert.Single(rows);
            Assert.Equal("integration test debug message", rows.First().Message);
        }
    }
}
