using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// The Nakshatra (Moon's constellation) in effect at a queried instant, when it ends, and
    /// which Nakshatra follows - see Calculate.NakshatraTransition.
    /// </summary>
    public record struct NakshatraTransition(Constellation Current, Time EndTime, Constellation Next) : IToJson
    {
        public JObject ToJson()
        {
            var obj = new JObject();
            obj["Current"] = Current.ToJson();
            obj["EndTime"] = EndTime.ToJson();
            obj["Next"] = Next.ToJson();
            return obj;
        }
    }
}
