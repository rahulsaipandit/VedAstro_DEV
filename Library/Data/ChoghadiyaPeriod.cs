using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// One named, qualified time window within a Choghadiya day - see Calculate.ChoghadiyaPeriods.
    /// </summary>
    public record struct ChoghadiyaPeriod(ChoghadiyaName Name, ChoghadiyaQuality Quality, Time Start, Time End, bool IsDayPeriod) : IToJson
    {
        public JObject ToJson()
        {
            var obj = new JObject();
            obj["Name"] = Name.ToString();
            obj["Quality"] = Quality.ToString();
            obj["Start"] = Start.ToJson();
            obj["End"] = End.ToJson();
            obj["IsDayPeriod"] = IsDayPeriod;
            return obj;
        }
    }
}
