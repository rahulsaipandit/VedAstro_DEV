namespace VedAstro.Library
{
    /// <summary>
    /// Replaces Azure.Data.Tables' ITableEntity. Every ported table entity keeps the same
    /// PartitionKey/RowKey composite-key shape it had as an Azure Table row - this interface
    /// is what lets the generic Postgres repository (VedAstro.Data.Repositories.EfKeyedRepository)
    /// work against any of them without per-table code.
    /// </summary>
    public interface IPartitionRowKeyEntity
    {
        string PartitionKey { get; set; }
        string RowKey { get; set; }
    }
}
