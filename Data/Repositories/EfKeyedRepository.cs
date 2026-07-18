using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VedAstro.Library;

namespace VedAstro.Data.Repositories
{
    /// <summary>
    /// Generic Postgres-backed implementation shared by every named repository interface
    /// (IPersonRepository, IUserDataRepository, etc) - each concrete repository class is a thin
    /// subclass that only exists so it can be registered/resolved by its own DI interface type.
    /// </summary>
    public class EfKeyedRepository<TEntity> : IKeyedRepository<TEntity> where TEntity : class, IPartitionRowKeyEntity
    {
        // A fresh, short-lived AppDbContext is created per operation (via this factory) rather
        // than holding a single shared instance for the repository's whole lifetime. DbContext
        // is not thread-safe, and repositories are captured once at startup into the static
        // `Repositories` locator (see Library/Logic/Repositories.cs) and reused for the app's
        // entire lifetime - a shared long-lived context would (a) break under concurrent
        // requests and (b) stay corrupted after any single failed SaveChangesAsync (e.g. a
        // constraint violation), poisoning every later call until process restart.
        // IDbContextFactory<T> itself is thread-safe and fine to hold long-lived.
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public EfKeyedRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        // Synchronous by interface contract (callers do `Query().FirstOrDefault(...)` inline),
        // so the short-lived context can't outlive this method the way the async methods below
        // do - materialize eagerly here and hand back an in-memory queryable instead.
        public IQueryable<TEntity> Query()
        {
            using var db = _contextFactory.CreateDbContext();
            return db.Set<TEntity>().AsNoTracking().ToList().AsQueryable();
        }

        public async Task<TEntity?> GetAsync(string partitionKey, string rowKey)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            return await db.Set<TEntity>().AsNoTracking()
                .FirstOrDefaultAsync(e => e.PartitionKey == partitionKey && e.RowKey == rowKey);
        }

        public async Task<List<TEntity>> GetByPartitionKeyAsync(string partitionKey)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            return await db.Set<TEntity>().AsNoTracking()
                .Where(e => e.PartitionKey == partitionKey)
                .ToListAsync();
        }

        public async Task<List<TEntity>> GetAllAsync()
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            return await db.Set<TEntity>().AsNoTracking().ToListAsync();
        }

        public async Task UpsertAsync(TEntity entity)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            var existing = await db.Set<TEntity>()
                .FirstOrDefaultAsync(e => e.PartitionKey == entity.PartitionKey && e.RowKey == entity.RowKey);

            if (existing == null)
            {
                db.Set<TEntity>().Add(entity);
            }
            else
            {
                db.Entry(existing).CurrentValues.SetValues(entity);
            }

            await db.SaveChangesAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            db.Set<TEntity>().Add(entity);
            await db.SaveChangesAsync();
        }

        public async Task DeleteAsync(string partitionKey, string rowKey)
        {
            await using var db = await _contextFactory.CreateDbContextAsync();
            var existing = await db.Set<TEntity>()
                .FirstOrDefaultAsync(e => e.PartitionKey == partitionKey && e.RowKey == rowKey);

            if (existing != null)
            {
                db.Set<TEntity>().Remove(existing);
                await db.SaveChangesAsync();
            }
        }
    }
}
