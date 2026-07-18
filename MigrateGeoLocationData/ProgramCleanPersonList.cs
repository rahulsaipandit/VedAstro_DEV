using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VedAstro.Data;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace MigrateGeoLocationData
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Partition key to filter
            string partitionKey = "xxxxx";

            // Date to filter (22 December 2024)
            DateTimeOffset targetDate = new DateTimeOffset(new DateTime(2024, 12, 22), TimeSpan.Zero);
            DateTimeOffset startTimestamp = targetDate.Date;
            DateTimeOffset endTimestamp = startTimestamp.AddDays(1);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException(
                    "Missing ConnectionStrings:Postgres - add an appsettings.json next to this " +
                    "project (see appsettings.Development.json under API/ for the local format).");

            var services = new ServiceCollection();
            services.AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(connectionString));
            await using var provider = services.BuildServiceProvider();

            var contextFactory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            var personRepository = new PersonRepository(contextFactory);

            // Same filter as the original Azure Table cleanup: rows in the given partition
            // created on the target date.
            var matchingRows = personRepository.Query()
                .Where(p => p.PartitionKey == partitionKey
                            && p.Timestamp >= startTimestamp
                            && p.Timestamp < endTimestamp)
                .ToList();

            int deleteCount = 0;

            foreach (var row in matchingRows)
            {
                try
                {
                    await personRepository.DeleteAsync(row.PartitionKey, row.RowKey);
                    deleteCount++;
                    Console.WriteLine($"Deleted entity with RowKey: {row.RowKey}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete entity with RowKey: {row.RowKey}. Error: {ex.Message}");
                }
            }

            Console.WriteLine($"Total entities deleted: {deleteCount}");
            Console.WriteLine("Operation completed.");
        }
    }
}
