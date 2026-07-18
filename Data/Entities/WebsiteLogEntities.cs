using System;

namespace VedAstro.Library
{
    // Pulled out of API/FrontDesk/WebsiteLoggerAPI.cs so they can live alongside the other
    // ported table entities in VedAstro.Data.

    public class WebsiteDebugLogEntity : IPartitionRowKeyEntity
    {
        // Needed by Table
        public string PartitionKey { get; set; }

        /// <summary>
        /// Local Time
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Time of change
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }
        public string Url { get; set; }
        public string UserAgent { get; set; }

        public string Message { get; set; }
    }

    public class WebsiteErrorLogEntity : IPartitionRowKeyEntity
    {
        //NEEDED BY TABLE
        public string PartitionKey { get; set; }

        /// <summary>
        /// Client's Local Time
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Time of change
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }
        public string Url { get; set; }
        public string UserAgent { get; set; }
        public string ErrorMessage { get; set; }
        public string StackTrace { get; set; }
    }
}
