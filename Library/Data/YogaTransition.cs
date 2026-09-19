using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// The Nithya Yoga in effect at a queried instant, when it ends, and which Yoga follows - see
    /// Calculate.YogaTransition.
    /// </summary>
    public record struct YogaTransition(NithyaYoga Current, Time EndTime, NithyaYoga Next) : IToJson
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
