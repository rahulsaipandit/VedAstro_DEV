using System.Linq;
using System.Threading.Tasks;
using VedAstro.Library;

namespace VedAstro.Data.Repositories
{
    /// <summary>
    /// Generic CRUD surface shared by every ported Azure-Table-shaped Postgres repository.
    /// Mirrors the handful of operations the old `TableClient` calls actually used
    /// (Query/UpsertEntity/AddEntity/DeleteEntity) so call-sites port mechanically.
    /// </summary>
    public interface IKeyedRepository<TEntity> where TEntity : class, IPartitionRowKeyEntity
    {
        /// <summary>Exposes a queryable for arbitrary LINQ predicates (EF Core translates to SQL).</summary>
        IQueryable<TEntity> Query();

        Task<TEntity?> GetAsync(string partitionKey, string rowKey);
        Task<System.Collections.Generic.List<TEntity>> GetByPartitionKeyAsync(string partitionKey);
        Task<System.Collections.Generic.List<TEntity>> GetAllAsync();

        /// <summary>Insert or update - matches Azure Table's UpsertEntity semantics.</summary>
        Task UpsertAsync(TEntity entity);

        /// <summary>Insert only - matches Azure Table's AddEntity semantics (throws/fails on duplicate key).</summary>
        Task AddAsync(TEntity entity);

        Task DeleteAsync(string partitionKey, string rowKey);
    }
}
