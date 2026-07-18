using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using VedAstro.Data;
using Xunit;

namespace API.IntegrationTests
{
    /// <summary>
    /// Spins up the real minimal-API host (API/Program.cs) in-memory via
    /// Microsoft.AspNetCore.Mvc.Testing's WebApplicationFactory, backed by a real, disposable
    /// Postgres container (Testcontainers.PostgreSql) and an isolated temp folder for chart caching.
    /// Shared across all tests in a class via IClassFixture&lt;ApiWebApplicationFactory&gt;.
    /// </summary>
    public class ApiWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("vedastro_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        private readonly string _chartCacheDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        /// <summary>Temp folder backing this instance's IChartImageCache - tests can inspect it directly.</summary>
        public string ChartCacheDirectory => _chartCacheDir;

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            // force host creation now (rather than lazily on first CreateClient()) so the
            // AppDbContext.Database.MigrateAsync() below runs before any test issues a request
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Program.cs's ConfigureKestrel(ListenLocalhost(7071)) only configures
            // KestrelServerOptions - WebApplicationFactory replaces the IServer registration with
            // TestServer entirely (via UseTestServer(), triggered by CreateClient()/CreateDefaultClient()),
            // so that Kestrel binding is never actually used here and doesn't need overriding.
            builder.UseEnvironment("IntegrationTests");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                var overrides = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                    ["ChartCacheDirectory"] = _chartCacheDir,
                };
                config.AddInMemoryCollection(overrides);
            });
        }

        // Note: WebApplicationFactory<T> already declares its own `DisposeAsync()` returning
        // ValueTask (from IAsyncDisposable) - this `new` hides it for xunit's IAsyncLifetime
        // (which needs a Task-returning DisposeAsync), while still calling through to the base
        // implementation so the TestServer/HttpClients/DI container get torn down properly.
        public new async Task DisposeAsync()
        {
            await base.DisposeAsync();
            await _postgres.DisposeAsync();

            try
            {
                if (Directory.Exists(_chartCacheDir)) { Directory.Delete(_chartCacheDir, true); }
            }
            catch
            {
                // best-effort cleanup, don't fail teardown over a locked temp file
            }
        }
    }
}
