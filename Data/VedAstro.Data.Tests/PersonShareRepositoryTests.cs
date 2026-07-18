using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;
using Xunit;

namespace VedAstro.Data.Tests
{
    /// <summary>
    /// PersonShareRow has no write path in the real application (nothing calls
    /// IPersonShareRepository.Add/Upsert today) - so unlike the other repositories, these tests
    /// seed rows directly through AppDbContext (mimicking a row created by some other means/import)
    /// and only confirm the read side (GetAsync/GetByPartitionKeyAsync/GetAllAsync/Query) works.
    /// </summary>
    [Collection("Postgres")]
    public class PersonShareRepositoryTests
    {
        private readonly PostgresFixture _fixture;

        public PersonShareRepositoryTests(PostgresFixture fixture)
        {
            _fixture = fixture;
        }

        private async Task SeedAsync(PersonShareRow row)
        {
            await using var db = _fixture.CreateContext();
            db.PersonShareList.Add(row);
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task GetAsync_ReturnsSeededRow()
        {
            var ownerId = Guid.NewGuid().ToString();
            var sharedPersonId = Guid.NewGuid().ToString();
            await SeedAsync(new PersonShareRow(ownerId, sharedPersonId));

            var repo = new PersonShareRepository(_fixture.CreateContextFactory());

            var fetched = await repo.GetAsync(ownerId, sharedPersonId);

            Assert.NotNull(fetched);
            Assert.Equal(ownerId, fetched!.PartitionKey);
            Assert.Equal(sharedPersonId, fetched.RowKey);
        }

        [Fact]
        public async Task GetByPartitionKeyAsync_ReturnsAllSharesForOwner()
        {
            var ownerId = Guid.NewGuid().ToString();
            await SeedAsync(new PersonShareRow(ownerId, Guid.NewGuid().ToString()));
            await SeedAsync(new PersonShareRow(ownerId, Guid.NewGuid().ToString()));
            await SeedAsync(new PersonShareRow(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

            var repo = new PersonShareRepository(_fixture.CreateContextFactory());

            var rows = await repo.GetByPartitionKeyAsync(ownerId);

            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal(ownerId, r.PartitionKey));
        }

        [Fact]
        public async Task Query_SupportsLinqPredicate()
        {
            var ownerId = Guid.NewGuid().ToString();
            var sharedPersonId = Guid.NewGuid().ToString();
            await SeedAsync(new PersonShareRow(ownerId, sharedPersonId));

            var repo = new PersonShareRepository(_fixture.CreateContextFactory());

            var found = repo.Query().FirstOrDefault(r => r.RowKey == sharedPersonId);

            Assert.NotNull(found);
        }

        [Fact]
        public async Task GetAsync_UnknownKey_ReturnsNull()
        {
            var repo = new PersonShareRepository(_fixture.CreateContextFactory());

            var fetched = await repo.GetAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            Assert.Null(fetched);
        }
    }
}
