using System;

namespace VedAstro.Library
{
    /// <summary>
    /// Pulled out of API/TableData/OpenAPIErrorBookEntity.cs (was namespace API) so it can live
    /// alongside the other ported table entities in VedAstro.Data. Used by API/ApiLogger.cs.
    /// </summary>
    public class OpenAPIErrorBookEntity : IPartitionRowKeyEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string Branch { get; set; }
        public string URL { get; set; }
        public string Message { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>
    /// New entity (replaces the old raw untyped Azure TableEntity used in
    /// API/ThrottleManager.cs) - records a call made with a given subscriber API key.
    /// PartitionKey = API key, RowKey = "" (one row per key, like the original code).
    /// </summary>
    public class SubscriberCallRecordEntity : IPartitionRowKeyEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public int CallCount { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>
    /// New entity (replaces the old raw untyped Azure TableEntity used in
    /// API/ThrottleManager.cs) - records one anonymous (no API key) call from an IP,
    /// used for throttling. PartitionKey = caller IP, RowKey = random Guid per call.
    /// </summary>
    public class AnonymousIpCallRecordEntity : IPartitionRowKeyEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
