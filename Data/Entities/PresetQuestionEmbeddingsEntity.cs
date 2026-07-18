using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Plain POCO - physically lives in VedAstro.Data (not Library), same reasoning as
    /// ChatMessageEntity.cs/PersonListEntity.cs. GetEmbeddingsArray only depends on
    /// Newtonsoft.Json (an external NuGet package, not VedAstro.Library), so it's safe to keep
    /// here rather than as a separate Library-side extension method.
    /// </summary>
    public class PresetQuestionEmbeddingsEntity : IPartitionRowKeyEntity
    {

        /// <summary>
        /// input query
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// category
        /// </summary>
        public string RowKey { get; set; }


        public string Embeddings { get; set; }


        /// <summary>
        /// mandatory
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        public double[] GetEmbeddingsArray()
        {
            var docEmbedding = JArray.Parse(this.Embeddings);
            var newQueryEmbedsgg = docEmbedding.Select(jv => (double)jv).ToArray();

            return newQueryEmbedsgg;
        }
    }
}
