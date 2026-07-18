using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class SubscriberStatisticRepositoryTests : KeyedRepositoryTestsBase<SubscriberStatisticEntity, ISubscriberStatisticRepository>
    {
        public SubscriberStatisticRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override ISubscriberStatisticRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new SubscriberStatisticRepository(factory);

        protected override void FillCustomData(SubscriberStatisticEntity entity, string marker)
        {
            entity.CallCount = marker.Length;
            entity.MetadataHash = "hash-" + marker;
        }
    }
}
