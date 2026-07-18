using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class PersonRepositoryTests : KeyedRepositoryTestsBase<PersonListEntity, IPersonRepository>
    {
        public PersonRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IPersonRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new PersonRepository(factory);

        protected override void FillCustomData(PersonListEntity entity, string marker)
        {
            entity.Name = "Person " + marker;
            entity.BirthTime = "00:00 01/01/2000 +00:00 90:00,25:00";
            entity.Gender = "Male";
            entity.Notes = "note " + marker;
        }
    }
}
