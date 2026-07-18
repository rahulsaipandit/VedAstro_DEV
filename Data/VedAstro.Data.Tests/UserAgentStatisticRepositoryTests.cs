using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class UserAgentStatisticRepositoryTests : KeyedRepositoryTestsBase<UserAgentStatisticEntity, IUserAgentStatisticRepository>
    {
        public UserAgentStatisticRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IUserAgentStatisticRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new UserAgentStatisticRepository(factory);

        protected override void FillCustomData(UserAgentStatisticEntity entity, string marker)
        {
            entity.CallCount = marker.Length;
            entity.MetadataHash = "hash-" + marker;
        }
    }
}
