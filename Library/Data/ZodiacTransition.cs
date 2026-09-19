using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// A planet's zodiac sign in effect at a queried instant, when it ends, and which sign
    /// follows - see Calculate.MoonZodiacTransition.
    /// </summary>
    public record struct ZodiacTransition(ZodiacName Current, Time EndTime, ZodiacName Next) : IToJson
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
