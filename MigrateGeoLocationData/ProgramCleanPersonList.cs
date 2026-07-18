using System;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Azure;
using CsvHelper;
using CsvHelper.Configuration;
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

            // Start and end timestamps for the target date in UTC
            DateTimeOffset startTimestamp = targetDate.Date;
            DateTimeOffset endTimestamp = startTimestamp.AddDays(1);


            // Build the filter expression
            string partitionKeyFilter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey}");

            // Build the Timestamp filter
            string timestampFilter = TableClient.CreateQueryFilter(
                $"Timestamp ge {startTimestamp} and Timestamp lt {endTimestamp}"
            );

            // Combine filters
            string combinedFilter = $"{partitionKeyFilter} and {timestampFilter}";

            // Query the entities
            // NOTE: AzureTable.PersonList (Library) was removed by the Postgres migration -
            // this one-off cleanup tool is out of scope for that migration (per migration.md),
            // so it now builds its own direct Azure Table client instead.
            var personListTableClient = new TableServiceClient(Secrets.VedAstroCentralStorageConnStr).GetTableClient("PersonList");
            AsyncPageable<TableEntity> queryResults = personListTableClient.QueryAsync<TableEntity>(filter: combinedFilter);

            int deleteCount = 0;

            // Iterate through the entities and delete them
            await foreach (TableEntity entity in queryResults)
            {
                try
                {
                    await personListTableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey, ETag.All);
                    deleteCount++;
                    Console.WriteLine($"Deleted entity with RowKey: {entity.RowKey}");
                }
                catch (RequestFailedException ex)
                {
                    Console.WriteLine($"Failed to delete entity with RowKey: {entity.RowKey}. Error: {ex.Message}");
                }
            }

            Console.WriteLine($"Total entities deleted: {deleteCount}");
            Console.WriteLine("Operation completed.");
        }
    }

}
