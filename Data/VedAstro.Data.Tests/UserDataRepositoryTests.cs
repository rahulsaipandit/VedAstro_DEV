using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;

namespace VedAstro.Data.Tests
{
    public class UserDataRepositoryTests : KeyedRepositoryTestsBase<UserDataListEntity, IUserDataRepository>
    {
        public UserDataRepositoryTests(PostgresFixture fixture) : base(fixture) { }

        protected override IUserDataRepository CreateRepository(IDbContextFactory<AppDbContext> factory) => new UserDataRepository(factory);

        protected override void FillCustomData(UserDataListEntity entity, string marker)
        {
            entity.Name = "User " + marker;
            entity.APIKey = "key-" + marker;
            entity.StripeCustomerID = "cus_" + marker;
        }
    }
}
