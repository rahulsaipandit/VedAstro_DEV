using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class WebPageStatisticRepositoryTests : KeyedRepositoryTestsBase<WebPageStatisticEntity, IWebPageStatisticRepository>
    {
        public WebPageStatisticRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IWebPageStatisticRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new WebPageStatisticRepository(factory);

        protected override void FillCustomData(WebPageStatisticEntity entity, string marker)
        {
            entity.CallCount = marker.Length;
        }
    }
}
