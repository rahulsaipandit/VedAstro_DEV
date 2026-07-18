using System;

namespace VedAstro.Library
{
    public class CallStatusEntity : IPartitionRowKeyEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public bool IsRunning { get; set; }
    }
}
