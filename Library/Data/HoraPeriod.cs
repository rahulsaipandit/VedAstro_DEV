using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// One named time window within a day's 24-hora table - see Calculate.HoraPeriods. Unlike
    /// <see cref="Calculate.HoraAtBirth"/>, which only answers "which hora governs this one
    /// instant", this models the full day/night sequence of 12+12 hora windows.
    /// </summary>
    public record struct HoraPeriod(PlanetName Lord, Time Start, Time End, bool IsDayPeriod) : IToJson
    {
        public JObject ToJson()
        {
            var obj = new JObject();
            obj["Lord"] = Lord.ToString();
            obj["Start"] = Start.ToJson();
            obj["End"] = End.ToJson();
            obj["IsDayPeriod"] = IsDayPeriod;
            return obj;
        }
    }
}
