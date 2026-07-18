using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class AnonymousIpCallRecordRepositoryTests : KeyedRepositoryTestsBase<AnonymousIpCallRecordEntity, IAnonymousIpCallRecordRepository>
    {
        public AnonymousIpCallRecordRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IAnonymousIpCallRecordRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new AnonymousIpCallRecordRepository(factory);

        protected override void FillCustomData(AnonymousIpCallRecordEntity entity, string marker)
        {
            // Timestamp is non-nullable on this entity (unlike the other ported tables) - must set explicitly.
            entity.Timestamp = DateTimeOffset.UtcNow;
        }
    }
}
