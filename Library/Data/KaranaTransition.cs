using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// The Karana (half-Tithi) in effect at a queried instant, when it ends, and which Karana
    /// follows - see Calculate.KaranaTransition.
    /// </summary>
    public record struct KaranaTransition(Karana Current, Time EndTime, Karana Next) : IToJson
    {
        public JObject ToJson()
        {
            var obj = new JObject();
            obj["Current"] = Current.ToString();
            obj["EndTime"] = EndTime.ToJson();
            obj["Next"] = Next.ToString();
            return obj;
        }
    }
}
