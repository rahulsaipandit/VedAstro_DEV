using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class SubscriberCallRecordRepositoryTests : KeyedRepositoryTestsBase<SubscriberCallRecordEntity, ISubscriberCallRecordRepository>
    {
        public SubscriberCallRecordRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override ISubscriberCallRecordRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new SubscriberCallRecordRepository(factory);

        protected override void FillCustomData(SubscriberCallRecordEntity entity, string marker)
        {
            entity.CallCount = marker.Length;
        }
    }
}
