using Microsoft.EntityFrameworkCore;
using VedAstro.Data.Repositories;
using VedAstro.Library;
using Xunit;

namespace VedAstro.Data.Tests
{
    /// <summary>
    /// Shared CRUD contract test suite, run once per concrete repository via inheritance
    /// (xUnit discovers and runs [Fact] methods declared on base classes too). Each concrete
    /// subclass only supplies how to construct its repository and how to fill in the entity's
    /// custom (non-key) fields - the actual assertions here exercise the exact surface every
    /// IKeyedRepository&lt;TEntity&gt; exposes: Query/GetAsync/GetByPartitionKeyAsync/GetAllAsync/
    /// UpsertAsync/AddAsync/DeleteAsync.
    ///
    /// Every test uses a fresh Guid-based PartitionKey/RowKey so tests can safely share one
    /// Postgres container/database across the whole "Postgres" collection without colliding.
    ///
    /// The repository is constructed once per test from a context *factory* (matching how
    /// API/Program.cs wires repositories - each operation gets its own short-lived AppDbContext
    /// internally, see EfKeyedRepository), so there's no need to juggle explicit AppDbContext
    /// instances/scopes here anymore.
    /// </summary>
    [Collection("Postgres")]
    public abstract class KeyedRepositoryTestsBase<TEntity, TRepo>
        where TEntity : class, IPartitionRowKeyEntity, new()
        where TRepo : IKeyedRepository<TEntity>
    {
        protected readonly PostgresFixture Fixture;

        protected KeyedRepositoryTestsBase(PostgresFixture fixture)
        {
            Fixture = fixture;
        }

        protected abstract TRepo CreateRepository(IDbContextFactory<AppDbContext> factory);

        /// <summary>Fill in whatever non-key fields this entity type needs (also free to set Timestamp).</summary>
        protected abstract void FillCustomData(TEntity entity, string marker);

        private TEntity MakeEntity(string partitionKey, string rowKey, string marker)
        {
            var entity = new TEntity { PartitionKey = partitionKey, RowKey = rowKey };
            FillCustomData(entity, marker);
            return entity;
        }

        private TRepo Repo() => CreateRepository(Fixture.CreateContextFactory());

        [Fact]
        public async Task AddAsync_ThenGetAsync_ReturnsEntity()
        {
            var pk = Guid.NewGuid().ToString();
            var rk = Guid.NewGuid().ToString();
            var repo = Repo();

            await repo.AddAsync(MakeEntity(pk, rk, "add"));

            var fetched = await Repo().GetAsync(pk, rk);

            Assert.NotNull(fetched);
            Assert.Equal(pk, fetched!.PartitionKey);
            Assert.Equal(rk, fetched.RowKey);
        }

        [Fact]
        public async Task GetAsync_UnknownKey_ReturnsNull()
        {
            var fetched = await Repo().GetAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            Assert.Null(fetched);
        }

        [Fact]
        public async Task GetByPartitionKeyAsync_ReturnsOnlyMatchingRows()
        {
            var pk = Guid.NewGuid().ToString();
            var otherPk = Guid.NewGuid().ToString();
            var repo = Repo();
            await repo.AddAsync(MakeEntity(pk, "row1", "a"));
            await repo.AddAsync(MakeEntity(pk, "row2", "b"));
            await repo.AddAsync(MakeEntity(otherPk, "row1", "c"));

            var rows = await Repo().GetByPartitionKeyAsync(pk);

            Assert.Equal(2, rows.Count);
            Assert.All(rows, r => Assert.Equal(pk, r.PartitionKey));
        }

        [Fact]
        public async Task GetAllAsync_IncludesAddedEntity()
        {
            var pk = Guid.NewGuid().ToString();
            var rk = Guid.NewGuid().ToString();
            await Repo().AddAsync(MakeEntity(pk, rk, "all"));

            var all = await Repo().GetAllAsync();

            Assert.Contains(all, e => e.PartitionKey == pk && e.RowKey == rk);
        }

        [Fact]
        public async Task Query_SupportsLinqPredicate()
        {
            var pk = Guid.NewGuid().ToString();
            var rk = Guid.NewGuid().ToString();
            await Repo().AddAsync(MakeEntity(pk, rk, "query"));

            var found = Repo().Query().FirstOrDefault(e => e.PartitionKey == pk && e.RowKey == rk);

            Assert.NotNull(found);
        }

        [Fact]
        public async Task UpsertAsync_InsertsWhenMissing()
        {
            var pk = Guid.NewGuid().ToString();
            var rk = Guid.NewGuid().ToString();

            await Repo().UpsertAsync(MakeEntity(pk, rk, "upsert-insert"));

            var fetched = await Repo().GetAsync(pk, rk);
            Assert.NotNull(fetched);
        }

        [Fact]
        public async Task UpsertAsync_UpdatesWhenExists()
        {
            var pk = Guid.NewGuid().ToString();
            var rk = Guid.NewGuid().ToString();

            await Repo().AddAsync(MakeEntity(pk, rk, "original"));
            await Repo().UpsertAsync(MakeEntity(pk, rk, "updated"));

            // Upsert must not have created a duplicate row for the same (pk, rk).
            var rows = await Repo().GetByPartitionKeyAsync(pk);
            Assert.Single(rows);
        }

        [Fact]
        public async Task DeleteAsync_RemovesEntity()
        {
            var pk = Guid.NewGuid().ToString();
            var rk = Guid.NewGuid().ToString();

            await Repo().AddAsync(MakeEntity(pk, rk, "delete-me"));
            await Repo().DeleteAsync(pk, rk);

            var fetched = await Repo().GetAsync(pk, rk);
            Assert.Null(fetched);
        }

        [Fact]
        public async Task DeleteAsync_UnknownKey_DoesNotThrow()
        {
            var ex = await Record.ExceptionAsync(() =>
                Repo().DeleteAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()));

            Assert.Null(ex);
        }
    }
}
