using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Time/JSON-aware helpers for PersonListEntity (which lives in VedAstro.Data as a plain
    /// POCO and can't reference VedAstro.Library's Time type directly - see that file's header
    /// comment). Kept as extension methods here so every existing call site
    /// (row.GetBirthYear(), row.ToBirthTime(), row.IsMale(), row.Clone()) keeps working unchanged.
    /// </summary>
    public static class PersonListEntityExtensions
    {
        /// <summary>Full clone for easy modification</summary>
        public static PersonListEntity Clone(this PersonListEntity row)
        {
            return new PersonListEntity()
            {
                PartitionKey = row.PartitionKey,
                RowKey = row.RowKey,
                Timestamp = row.Timestamp,
                Name = row.Name,
                BirthTime = row.BirthTime,
                Gender = row.Gender,
                Notes = row.Notes
            };
        }

        /// <summary>parse and extract birth year on the fly</summary>
        public static string GetBirthYear(this PersonListEntity row)
        {
            var xxx = JObject.Parse(row.BirthTime);
            var stdTimeString = xxx["StdTime"].Value<string>();
            //break & take out center part dd/mm/year
            var ccc = stdTimeString.Split(" ");
            var ddMMYYYY = ccc[1];

            return ddMMYYYY;
        }

        public static Time ToBirthTime(this PersonListEntity row)
        {
            var birthTime = Time.FromJson(JObject.Parse(row.BirthTime));
            return birthTime;
        }

        public static bool IsMale(this PersonListEntity row)
        {
            return row.Gender.ToLower() == "male";
        }
    }
}
