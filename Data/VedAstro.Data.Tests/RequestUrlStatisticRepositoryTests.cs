using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class RequestUrlStatisticRepositoryTests : KeyedRepositoryTestsBase<RequestUrlStatisticEntity, IRequestUrlStatisticRepository>
    {
        public RequestUrlStatisticRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IRequestUrlStatisticRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new RequestUrlStatisticRepository(factory);

        protected override void FillCustomData(RequestUrlStatisticEntity entity, string marker)
        {
            entity.CallCount = marker.Length;
            entity.MetadataHash = "hash-" + marker;
        }
    }
}
