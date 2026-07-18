using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class WebsiteErrorLogRepositoryTests : KeyedRepositoryTestsBase<WebsiteErrorLogEntity, IWebsiteErrorLogRepository>
    {
        public WebsiteErrorLogRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IWebsiteErrorLogRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new WebsiteErrorLogRepository(factory);

        protected override void FillCustomData(WebsiteErrorLogEntity entity, string marker)
        {
            entity.Url = "https://example.com/" + marker;
            entity.UserAgent = "TestAgent/" + marker;
            entity.ErrorMessage = "error " + marker;
            entity.StackTrace = "stack " + marker;
        }
    }
}
