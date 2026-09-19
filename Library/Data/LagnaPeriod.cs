using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// One ascendant (Lagna) sign's window within a day - see Calculate.LagnaPeriods.
    /// </summary>
    public record struct LagnaPeriod(ZodiacName Sign, Time Start, Time End) : IToJson
    {
        public JObject ToJson()
        {
            var obj = new JObject();
            obj["Sign"] = Sign.ToString();
            obj["Start"] = Start.ToJson();
            obj["End"] = End.ToJson();
            return obj;
        }
    }
}
