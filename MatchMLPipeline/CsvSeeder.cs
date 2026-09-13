using System.Globalization;
using CsvHelper;
using VedAstro.Data;
using VedAstro.Library;

namespace MatchMLPipeline;

/// <summary>
/// One-time local-dev seeding tool: loads the HuggingFace CSVs (HuggingFace/PersonList-15k.csv,
/// HuggingFace/MarriageInfoDataset.csv) into the local Postgres tables (person_list,
/// marriage_info_dataset) that DashaValidationPipeline/KutaValidationPipeline read from.
///
/// Idempotent - re-running skips rows already present (matched by primary key), so it never
/// duplicates data and is safe to re-run after a partial run or once the CSVs are updated.
/// Uses bulk AddRange + batched SaveChanges directly against AppDbContext instead of the
/// per-row UpsertAsync repository method, since that would mean ~33k individual round trips
/// for this one-time load.
/// </summary>
public static class CsvSeeder
{
    private const int BatchSize = 500;

    public static void SeedAll(string huggingFaceDir)
    {
        SeedPersonList(Path.Combine(huggingFaceDir, "PersonList-15k.csv"));
        SeedMarriageInfoDataset(Path.Combine(huggingFaceDir, "MarriageInfoDataset.csv"));
    }

    // Matches HuggingFace/PersonList-15k.csv header: RowKey,BirthTime,Gender,Name,Notes
    // (no PartitionKey column - see PartitionKey assignment below)
    private class PersonCsvRow
    {
        public string RowKey { get; set; } = "";
        public string BirthTime { get; set; } = "";
        public string Gender { get; set; } = "";
        public string Name { get; set; } = "";
        public string Notes { get; set; } = "";
    }

    // Matches HuggingFace/MarriageInfoDataset.csv header: PartitionKey,RowKey,Info
    private class MarriageCsvRow
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public string Info { get; set; } = "";
    }

    public static int SeedPersonList(string csvPath)
    {
        using var streamReader = new StreamReader(csvPath);
        using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<PersonCsvRow>().ToList();

        using var db = DatasetFactory.dbContextFactory.CreateDbContext();
        var existingKeys = db.PersonList.Select(p => p.RowKey).ToHashSet();

        var newEntities = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.RowKey) && !existingKeys.Contains(r.RowKey))
            .Select(r => new PersonListEntity
            {
                // This dataset has no owner/user concept (see Person.cs's normal
                // PartitionKey=OwnerId convention) - self-partition on the unique slug instead,
                // matching how DatasetFactory already keys MarriageInfoDatasetEntity rows by
                // this same RowKey.
                PartitionKey = r.RowKey,
                RowKey = r.RowKey,
                Name = r.Name,
                BirthTime = r.BirthTime,
                Gender = r.Gender,
                Notes = r.Notes,
            })
            .ToList();

        InsertInBatches(db, () => db.PersonList, newEntities, "person_list");

        return newEntities.Count;
    }

    public static int SeedMarriageInfoDataset(string csvPath)
    {
        using var streamReader = new StreamReader(csvPath);
        using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<MarriageCsvRow>().ToList();

        using var db = DatasetFactory.dbContextFactory.CreateDbContext();
        var existingKeys = db.MarriageInfoDataset
            .Select(m => new { m.PartitionKey, m.RowKey })
            .ToHashSet(a => (a.PartitionKey, a.RowKey));

        var newEntities = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.PartitionKey))
            .Where(r => !existingKeys.Contains((r.PartitionKey, r.RowKey ?? "")))
            .Select(r => new MarriageInfoDatasetEntity
            {
                PartitionKey = r.PartitionKey,
                RowKey = r.RowKey ?? "",
                Info = r.Info,
            })
            .ToList();

        InsertInBatches(db, () => db.MarriageInfoDataset, newEntities, "marriage_info_dataset");

        return newEntities.Count;
    }

    private static void InsertInBatches<T>(AppDbContext db, Func<Microsoft.EntityFrameworkCore.DbSet<T>> table, List<T> entities, string tableName) where T : class
    {
        for (var i = 0; i < entities.Count; i += BatchSize)
        {
            var count = Math.Min(BatchSize, entities.Count - i);
            var batch = entities.GetRange(i, count);
            table().AddRange(batch);
            db.SaveChanges();
            db.ChangeTracker.Clear();

            Console.WriteLine($"{tableName}: inserted {Math.Min(i + BatchSize, entities.Count)}/{entities.Count}");
        }
    }
}

// Small extension used only by SeedMarriageInfoDataset above to build a HashSet of tuples
// from an anonymous-type projection without pulling in a third-party helper for one call site.
file static class HashSetExtensions
{
    public static HashSet<TKey> ToHashSet<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        where TKey : notnull
        => new(source.Select(keySelector));
}
