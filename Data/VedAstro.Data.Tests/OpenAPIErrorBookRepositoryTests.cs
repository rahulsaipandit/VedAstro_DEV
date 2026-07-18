using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class OpenAPIErrorBookRepositoryTests : KeyedRepositoryTestsBase<OpenAPIErrorBookEntity, IOpenAPIErrorBookRepository>
    {
        public OpenAPIErrorBookRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IOpenAPIErrorBookRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new OpenAPIErrorBookRepository(factory);

        protected override void FillCustomData(OpenAPIErrorBookEntity entity, string marker)
        {
            entity.Branch = "main";
            entity.URL = "https://example.com/" + marker;
            entity.Message = "error " + marker;
        }
    }
}
