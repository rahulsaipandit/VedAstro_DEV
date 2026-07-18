using System;

namespace VedAstro.Library
{
    /// <summary>
    /// Pulled out of ApiStatistic.cs (was a nested class) so it can live in VedAstro.Data
    /// alongside the other ported table entities. Table: CallInfoStatistic.
    /// </summary>
    public class CallInfoStatisticEntity : IPartitionRowKeyEntity
    {
        public string UserAgent { get; set; }
        public string RequestUrl { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
