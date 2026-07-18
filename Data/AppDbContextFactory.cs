using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VedAstro.Data
{
    /// <summary>
    /// Design-time factory so `dotnet ef migrations add`/`dotnet ef database update` can
    /// construct AppDbContext without needing the full API host/DI container running.
    /// Reads the same appsettings.json/appsettings.Development.json as the real API host
    /// (API/Program.cs), so `dotnet ef` uses the actual local connection string instead of
    /// a hardcoded placeholder - falls back to the placeholder only if config/file is missing.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var apiDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.Exists(apiDirectory) ? apiDirectory : Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("Postgres")
                ?? "Host=localhost;Port=5432;Database=vedastro;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
