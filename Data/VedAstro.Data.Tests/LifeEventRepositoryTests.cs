using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class LifeEventRepositoryTests : KeyedRepositoryTestsBase<LifeEventRow, ILifeEventRepository>
    {
        public LifeEventRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override ILifeEventRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new LifeEventRepository(factory);

        protected override void FillCustomData(LifeEventRow entity, string marker)
        {
            entity.Name = "Event " + marker;
            entity.Description = "Description " + marker;
            entity.StartTime = "00:00 01/01/2010 +00:00";
            entity.Nature = "Good";
            entity.Weight = "1";
        }
    }
}
