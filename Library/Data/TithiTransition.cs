using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// The Tithi in effect at a queried instant, when it ends, and which Tithi follows - see
    /// Calculate.TithiTransition.
    /// </summary>
    public record struct TithiTransition(LunarDay Current, Time EndTime, LunarDay Next) : IToJson
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
