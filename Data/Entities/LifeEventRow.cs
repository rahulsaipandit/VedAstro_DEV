using System;

namespace VedAstro.Library
{
    public class LifeEventRow : IPartitionRowKeyEntity
    {
        /// <summary>
        /// Person ID
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Time of creation
        /// </summary>
        public string RowKey { get; set; }

        /// <summary>
        /// Time of change
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        //CUSTOM DATA
        public string Name { get; set; }
        public string Description { get; set; }
        public string StartTime { get; set; }
        public string Nature { get; set; }
        public string Weight { get; set; }
    }

}
