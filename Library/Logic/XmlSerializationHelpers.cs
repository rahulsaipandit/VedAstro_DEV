using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Generic JSON&lt;-&gt;XML bridge plus ToXml/FromXml wrappers for types that already have
    /// ToJson/FromJson but never got XML equivalents wired up. Reconstructed from scratch - see
    /// Library/Logic/Calculate/CoreTime.cs header note for the equivalent situation elsewhere.
    /// NOTE: this XML shape is self-consistent within this codebase, but is NOT guaranteed to
    /// match any external server's exact wire format - only relevant if talking to a legacy API.
    /// </summary>
    public static partial class Tools
    {
        /// <summary>Converts any JToken into a same-shaped XElement tree, named by the given root name.</summary>
        public static XElement JsonToXml(string elementName, JToken token)
        {
            var safeName = System.Xml.XmlConvert.EncodeName(elementName);

            if (token is JObject obj)
            {
                var el = new XElement(safeName);
                foreach (var prop in obj.Properties()) { el.Add(JsonToXml(prop.Name, prop.Value)); }
                return el;
            }

            if (token is JArray arr)
            {
                var el = new XElement(safeName);
                foreach (var item in arr) { el.Add(JsonToXml("Item", item)); }
                return el;
            }

            return new XElement(safeName, token?.ToString() ?? "");
        }

        /// <summary>Converts an XElement tree (produced by JsonToXml) back into a JObject.</summary>
        public static JObject XmlToJsonObject(XElement element)
        {
            var obj = new JObject();

            foreach (var child in element.Elements())
            {
                var key = System.Xml.XmlConvert.DecodeName(child.Name.LocalName);

                if (!child.HasElements) { obj[key] = child.Value; continue; }

                if (child.Elements().All(e => e.Name.LocalName == "Item"))
                {
                    var arr = new JArray();
                    foreach (var item in child.Elements())
                    {
                        arr.Add(item.HasElements ? (JToken)XmlToJsonObject(item) : (JToken)item.Value);
                    }
                    obj[key] = arr;
                }
                else
                {
                    obj[key] = XmlToJsonObject(child);
                }
            }

            return obj;
        }

        /// <summary>Generic single-value XML wrapper, named by the value's type.</summary>
        public static XElement AnyTypeToXml<T>(T value) => new(typeof(T).Name, value?.ToString() ?? "");

        /// <summary>Serializes an exception to a simple XML report, for logging.</summary>
        public static XElement ExceptionToXML(System.Exception exception)
        {
            return new XElement("Exception",
                new XElement("Message", exception.Message),
                new XElement("Source", exception.Source ?? ""),
                new XElement("StackTrace", exception.StackTrace ?? ""));
        }

        /// <summary>Given an address, gets its GeoLocation wrapped in a WebResult, for UI code that checks IsPass.</summary>
        public static async System.Threading.Tasks.Task<WebResult<GeoLocation>> AddressToGeoLocation(string address)
        {
            var result = await Calculate.AddressToGeoLocation(address);
            var isPass = !result.Equals(GeoLocation.Empty);

            return new WebResult<GeoLocation>(isPass, result);
        }

        /// <summary>Gets the timezone offset string (e.g. "+05:30") for a location at a given time.</summary>
        public static async System.Threading.Tasks.Task<string> GetTimezoneOffsetApi(GeoLocation geoLocation, System.DateTimeOffset timeAtLocation) =>
            await Calculate.GeoLocationToTimezone(geoLocation, timeAtLocation);

        /// <summary>Gets the timezone offset string (e.g. "+05:30") for a named location at a given std time text.</summary>
        public static async System.Threading.Tasks.Task<string> GetTimezoneOffsetString(string locationName, string stdTimeText)
        {
            var geoLocation = await Calculate.AddressToGeoLocation(locationName);

            var timeAtLocation = System.DateTimeOffset.TryParseExact(stdTimeText, Time.DateTimeFormatNoTimezone,
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsed)
                ? parsed
                : System.DateTimeOffset.UtcNow;

            return await Calculate.GeoLocationToTimezone(geoLocation, timeAtLocation);
        }
    }

    public static class XmlSerializationExtensions
    {
        public static XElement ToXml(this Time time) => Tools.JsonToXml(nameof(Time), time.ToJson());

        public static XElement ToXml(this GeoLocation geoLocation) => Tools.JsonToXml(nameof(GeoLocation), geoLocation.ToJson());

        public static XElement ToXml(this Person person) => Tools.JsonToXml(nameof(Person), person.ToJson());

        public static XElement ToXml(this UserData userData)
        {
            return new XElement(nameof(UserData),
                new XElement(nameof(UserData.Id), userData.Id ?? ""),
                new XElement(nameof(UserData.Name), userData.Name ?? ""),
                new XElement(nameof(UserData.Email), userData.Email ?? ""),
                new XElement(nameof(UserData.APIKey), userData.APIKey ?? ""),
                new XElement(nameof(UserData.StripeCustomerID), userData.StripeCustomerID ?? ""));
        }
    }

    public partial class UserData
    {
        public static UserData FromXml(XElement userDataXml)
        {
            string Get(string name) => userDataXml.Element(name)?.Value ?? "";

            return new UserData
            {
                Id = Get(nameof(Id)),
                Name = Get(nameof(Name)),
                Email = Get(nameof(Email)),
                APIKey = Get(nameof(APIKey)),
                StripeCustomerID = Get(nameof(StripeCustomerID)),
            };
        }
    }

    public partial class Event
    {
        /// <summary>Parses a list of Events from a WebResult-wrapped XML payload (as returned by the events API).</summary>
        public static List<Event> FromXml(WebResult<XElement> webResult)
        {
            if (!webResult.IsPass || webResult.Payload == null) { return new List<Event>(); }

            return webResult.Payload.Elements()
                .Select(eventXml => Event.FromJson(Tools.XmlToJsonObject(eventXml)))
                .ToList();
        }
    }

    public partial class MatchReport
    {
        /// <summary>Parses a single MatchReport from an XML element (as returned by the get/save-match-report APIs).</summary>
        public static MatchReport FromXml(XElement matchReportXml)
        {
            var json = Tools.XmlToJsonObject(matchReportXml);

            var male = json["Male"] != null ? Person.FromJson(json["Male"]) : Person.Empty;
            var female = json["Female"] != null ? Person.FromJson(json["Female"]) : Person.Empty;
            var kutaScore = json["KutaScore"]?.Value<double>() ?? 0;
            var notes = json["Notes"]?.Value<string>() ?? "";
            var id = json["Id"]?.Value<string>() ?? Tools.GenerateId();
            var userId = json["UserId"]?.Value<string>()?.Split(',') ?? new[] { "101" };

            return new MatchReport(id, male, female, kutaScore, notes, new List<MatchPrediction>(), userId);
        }

        /// <summary>Parses a list of MatchReports from a list of XML elements (as returned by the match report list API).</summary>
        public static List<MatchReport> FromXml(IEnumerable<XElement> matchReportXmlList) =>
            matchReportXmlList.Select(FromXml).ToList();
    }
}
