using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class CallTrackerRepositoryTests : KeyedRepositoryTestsBase<CallStatusEntity, ICallTrackerRepository>
    {
        public CallTrackerRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override ICallTrackerRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new CallTrackerRepository(factory);

        protected override void FillCustomData(CallStatusEntity entity, string marker)
        {
            entity.IsRunning = marker.Length % 2 == 0;
        }
    }
}
