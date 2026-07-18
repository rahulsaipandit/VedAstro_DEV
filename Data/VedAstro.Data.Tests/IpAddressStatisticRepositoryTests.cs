using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class IpAddressStatisticRepositoryTests : KeyedRepositoryTestsBase<IpAddressStatisticEntity, IIpAddressStatisticRepository>
    {
        public IpAddressStatisticRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IIpAddressStatisticRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new IpAddressStatisticRepository(factory);

        protected override void FillCustomData(IpAddressStatisticEntity entity, string marker)
        {
            entity.CallsPerSecond = 1;
            entity.CallsPerMinute = 2;
            entity.CallsPerHour = 3;
            entity.CallsPerDay = 4;
            entity.CallsPerMonth = 5;
        }
    }
}
