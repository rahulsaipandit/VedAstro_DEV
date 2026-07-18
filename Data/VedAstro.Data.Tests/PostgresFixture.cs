using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace VedAstro.Data.Tests
{
    /// <summary>
    /// Shared xUnit collection fixture: starts one disposable Testcontainers Postgres instance
    /// for the whole "Postgres" test collection, applies AppDbContext's EF Core migrations to it
    /// once, and hands out a fresh AppDbContext per test via <see cref="CreateContext"/>.
    /// No manual/permanent Postgres install is required - Docker must be running.
    /// </summary>
    public class PostgresFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _container = null!;

        public string ConnectionString => _container.GetConnectionString();

        public async Task InitializeAsync()
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("vedastro_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();

            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        /// <summary>Creates a fresh AppDbContext bound to the container - one per test to avoid state bleed.</summary>
        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            return new AppDbContext(options);
        }

        /// <summary>
        /// Factory bound to the same container, for constructing repositories the same way
        /// API/Program.cs does (each repository creates its own short-lived context per
        /// operation via IDbContextFactory&lt;AppDbContext&gt; rather than holding one shared,
        /// non-thread-safe instance - see EfKeyedRepository).
        /// </summary>
        public IDbContextFactory<AppDbContext> CreateContextFactory() => new TestDbContextFactory(ConnectionString);

        private class TestDbContextFactory : IDbContextFactory<AppDbContext>
        {
            private readonly string _connectionString;
            public TestDbContextFactory(string connectionString) => _connectionString = connectionString;

            public AppDbContext CreateDbContext()
            {
                var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_connectionString).Options;
                return new AppDbContext(options);
            }
        }
    }

    [CollectionDefinition("Postgres")]
    public class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
        // Marker class - xUnit collection fixture wiring only, no code needed.
    }
}
