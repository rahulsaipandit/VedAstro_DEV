using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// JSON-aware helper methods for the MatchMLPipeline dataset entities that physically live
    /// in VedAstro.Data (Data/Entities/MatchMLDatasetEntities.cs) as plain POCOs - kept here as
    /// extension methods (rather than instance methods on the entities) because Newtonsoft.Json's
    /// JObject/JArray/JToken are not referenced by VedAstro.Data, and VedAstro.Data must not
    /// reference Library (Library references Data, not the reverse, to avoid a circular project
    /// reference). Mirrors the GeoLocationEntityExtensions.cs pattern used elsewhere.
    /// </summary>
    public static class MatchMLDatasetEntityExtensions
    {
        public static JObject? InfoJson(this MarriageInfoDatasetEntity row)
        {
            try
            {
                return JObject.Parse(row.Info);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool IsJson(this MarriageInfoDatasetEntity row)
        {
            try
            {
                var obj = JToken.Parse(row.Info);
                return true; // JSON is valid
            }
            catch (JsonReaderException)
            {
                return false; // JSON is invalid
            }
        }

        public static void SetMarriages(this MarriageInfoDatasetEntity row, List<JToken> marriages)
        {
            var jsonParsed = row?.InfoJson() ?? new JObject();
            jsonParsed["marriages"] = new JArray(marriages);
            row.Info = jsonParsed.ToString();
        }

        public static List<JToken> GetMarriages(this MarriageInfoDatasetEntity row)
        {
            //1 : convert to json
            var jsonParsed = row?.InfoJson();
            var returnList = new List<JToken>();
            if (jsonParsed != null)
            {
                var marriagesArray = jsonParsed["marriages"];
                foreach (var marriagesJson in marriagesArray)
                {
                    returnList.Add(marriagesJson);
                }
            }

            return returnList;
        }

        public static List<JToken> GetMarriagesWhereRoddenAA(this MarriageInfoDatasetEntity row)
        {
            var allMarriages = row.GetMarriages();

            var filtered = allMarriages.Where(x =>
            {
                var personId = x["PersonId"]?.Value<string>();
                //if NOT null add to list, meaning person was found in valid dataset
                return !string.IsNullOrEmpty(personId);
            });

            return filtered.ToList();
        }

        public static JObject InfoJson(this BodyInfoDatasetEntity row)
        {
            return JObject.Parse(row.Info);
        }

        public static bool IsJson(this BodyInfoDatasetEntity row)
        {
            try
            {
                var obj = JToken.Parse(row.Info);
                return true; // JSON is valid
            }
            catch (JsonReaderException)
            {
                return false; // JSON is invalid
            }
        }

        public static double[] GetEmbeddingsArray(this PersonNameEmbeddingsEntity row)
        {
            var docEmbedding = JArray.Parse(row.Embeddings);
            var newQueryEmbedsgg = docEmbedding.Select(jv => (double)jv).ToArray();

            return newQueryEmbedsgg;
        }
    }
}
