using System;

namespace VedAstro.Library
{
    // 3 tables backing MatchMLPipeline (a standalone offline console tool, not part of the
    // live API/Website) - previously raw Azure Table Storage rows accessed directly via
    // TableClient from MatchMLPipeline/DatasetFactory.cs, now plain POCOs living here.
    // NOTE: these are plain POCOs with no dependency on VedAstro.Library's JSON-parsing types
    // (JObject/JArray/JToken, etc) because they physically live in VedAstro.Data, which must not
    // reference Library (Library references Data, not the reverse, to avoid a circular project
    // reference). The JSON-aware helper methods that used to live on these classes
    // (InfoJson/IsJson/GetMarriages/SetMarriages/GetEmbeddingsArray) are now extension methods in
    // Library/Logic/MatchMLDatasetEntityExtensions.cs. These previously lived as full classes
    // directly under Library/Data/AzureTable/ - that's now superseded by these versions.
    // Mirrors the GeoLocationEntities.cs / GeoLocationEntityExtensions.cs precedent.

    /// <summary>
    /// Represents the data in 1 row of the marriage-info dataset table.
    /// </summary>
    public class MarriageInfoDatasetEntity : IPartitionRowKeyEntity
    {
        //NEEDED BY TABLE
        public string PartitionKey { get; set; }

        /// <summary>
        /// Time of creation
        /// </summary>
        public string RowKey { get; set; } = "";

        public string Info { get; set; }

        /// <summary>
        /// Time of change
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>
    /// Represents the data in 1 row of the body-info dataset table.
    /// </summary>
    public class BodyInfoDatasetEntity : IPartitionRowKeyEntity
    {
        //NEEDED BY TABLE
        public string PartitionKey { get; set; }

        public string RowKey { get; set; } = "";
        public string Info { get; set; }

        /// <summary>
        /// Time of change
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }
    }

    /// <summary>
    /// Represents the data in 1 row of the person-name-embeddings table.
    /// </summary>
    public class PersonNameEmbeddingsEntity : IPartitionRowKeyEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; } = ""; //set empty to stop error

        public string Embeddings { get; set; }
        public string PersonId { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
    }
}
