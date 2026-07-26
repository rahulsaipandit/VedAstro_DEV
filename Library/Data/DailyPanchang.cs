using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// The 5 core limbs of a daily Panchang (Tithi, Vara, Nakshatra, Yoga, Karana) plus
    /// Sunrise/Sunset - a reference "snapshot" of the day's Panchang elements, as opposed to
    /// Choghadiya's period-by-period "is now a good time to act" breakdown (see
    /// Calculate.ChoghadiyaPeriods). Deliberately excludes HoraLord/DishaShool/IshtaKaala, which
    /// the older, never-implemented <see cref="PanchangaTable"/> data holder also models but
    /// nothing in the codebase actually computes - not included here either, since each needs its
    /// own dedicated formula and verification before being trusted.
    /// </summary>
    public record struct DailyPanchang(LunarDay Tithi, LunarMonth LunarMonth, DayOfWeek Vara, Constellation Nakshatra, NithyaYoga Yoga, Karana Karana, Time Sunrise, Time Sunset) : IToJson
    {
        public JObject ToJson()
        {
            var obj = new JObject();
            obj["Tithi"] = Tithi.ToJson();
            obj["LunarMonth"] = LunarMonth.ToString();
            obj["Vara"] = Vara.ToString();
            obj["Nakshatra"] = Nakshatra.ToJson();
            obj["Yoga"] = Yoga.ToJson();
            obj["Karana"] = Karana.ToString();
            obj["Sunrise"] = Sunrise.ToJson();
            obj["Sunset"] = Sunset.ToJson();
            return obj;
        }
    }
}
