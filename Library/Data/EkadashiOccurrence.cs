using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// One Ekadashi's classical name plus its date - see Calculate.EkadashiCalendar.
    /// </summary>
    public record struct EkadashiOccurrence(string Name, Time Date) : IToJson
    {
        public JObject ToJson()
        {
            var obj = new JObject();
            obj["Name"] = Name;
            obj["Date"] = Date.ToJson();
            return obj;
        }
    }
}
