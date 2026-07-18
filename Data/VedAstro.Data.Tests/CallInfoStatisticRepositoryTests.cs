using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class CallInfoStatisticRepositoryTests : KeyedRepositoryTestsBase<CallInfoStatisticEntity, ICallInfoStatisticRepository>
    {
        public CallInfoStatisticRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override ICallInfoStatisticRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new CallInfoStatisticRepository(factory);

        protected override void FillCustomData(CallInfoStatisticEntity entity, string marker)
        {
            entity.UserAgent = "TestAgent/" + marker;
            entity.RequestUrl = "https://example.com/" + marker;
        }
    }
}
