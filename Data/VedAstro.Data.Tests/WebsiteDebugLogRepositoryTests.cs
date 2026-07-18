using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class WebsiteDebugLogRepositoryTests : KeyedRepositoryTestsBase<WebsiteDebugLogEntity, IWebsiteDebugLogRepository>
    {
        public WebsiteDebugLogRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IWebsiteDebugLogRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new WebsiteDebugLogRepository(factory);

        protected override void FillCustomData(WebsiteDebugLogEntity entity, string marker)
        {
            entity.Url = "https://example.com/" + marker;
            entity.UserAgent = "TestAgent/" + marker;
            entity.Message = "debug " + marker;
        }
    }
}
