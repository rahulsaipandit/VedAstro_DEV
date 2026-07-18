using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class RawRequestStatisticRepositoryTests : KeyedRepositoryTestsBase<RawRequestStatisticEntity, IRawRequestStatisticRepository>
    {
        public RawRequestStatisticRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IRawRequestStatisticRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new RawRequestStatisticRepository(factory);

        protected override void FillCustomData(RawRequestStatisticEntity entity, string marker)
        {
            // Every header-mirroring string property on this entity is a NOT NULL column
            // (see Data/Migrations/20260718140843_InitialCreate.cs) - default them all to ""
            // via reflection rather than hand-listing 50+ properties, then override a couple.
            foreach (var prop in typeof(RawRequestStatisticEntity).GetProperties())
            {
                // Skip the key columns - MakeEntity already set these and this loop must not clobber them.
                if (prop.Name is nameof(entity.PartitionKey) or nameof(entity.RowKey)) { continue; }

                if (prop.PropertyType == typeof(string) && prop.CanWrite)
                {
                    prop.SetValue(entity, "");
                }
            }

            entity.UserAgent = "TestAgent/" + marker;
            entity.Host = "example.com";
        }
    }
}
