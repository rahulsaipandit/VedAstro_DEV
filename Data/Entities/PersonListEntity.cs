using System;

namespace VedAstro.Library
{
    /// <summary>
    /// Represents the data in 1 row of person list table.
    /// NOTE: this is a plain POCO with no dependency on VedAstro.Library's calculator types
    /// (Time, JObject, etc) because it physically lives in VedAstro.Data, which must not
    /// reference Library (Library references Data, not the reverse, to avoid a circular
    /// project reference). The Time/JSON-aware helper methods that used to live on this class
    /// (Clone/GetBirthYear/ToBirthTime/IsMale) are now extension methods in
    /// Library/Logic/PersonListEntityExtensions.cs.
    /// </summary>
    public class PersonListEntity : IPartitionRowKeyEntity
    {
        //NEEDED BY TABLE
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
        public string BirthTime { get; set; }
        public string Gender { get; set; }
        public string Notes { get; set; }
    }

}
