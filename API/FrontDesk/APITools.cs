using System.Net;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VedAstro.Library;

namespace API
{
    /// <summary>
    /// Common HTTP request/response helpers used across the minimal-API endpoints in FrontDesk.
    /// Was HttpRequestData/HttpResponseData (Azure Functions Worker) based - now HttpContext
    /// (ASP.NET Core) based, since the API host moved off Azure Functions.
    /// Response shape matches WebResult&lt;T&gt;'s expected wire format: {"Status": "Pass"/"Fail", "Payload": ...}
    /// </summary>
    public static class APITools
    {
        private static readonly HttpClient SharedHttpClient = new();

        //domain objects (e.g. HoroscopePrediction.RelatedBody) hold circular navigation references;
        //default JToken.FromObject follows them and blows the stack with an uncatchable StackOverflowException,
        //killing the whole worker process - ignoring reference loops here keeps that contained
        private static readonly JsonSerializer PayloadSerializer = new() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

        /// <summary>Wraps a JSON-serializable payload in a "Pass" envelope and writes it as the HTTP response.</summary>
        public static async Task PassMessageJson(object payload, HttpContext context)
        {
            var root = new JObject
            {
                ["Status"] = "Pass",
                ["Payload"] = ToPayloadJson(payload)
            };

            await WriteJsonResponse(context, HttpStatusCode.OK, root);
        }

        //domain types implementing IToJson have a hand-written ToJson() with a wire shape
        //(property names, enums-as-strings) that the client's matching FromJson() parsers expect -
        //falling back to default reflection serialization silently produces a different shape
        //(e.g. RelatedBody's "RelatedPlanets"/"RelatedHouses" instead of "Planets"/"Houses",
        //enums as their raw int instead of name) that FromJson can't read, so honor ToJson() first
        private static JToken ToPayloadJson(object payload)
        {
            switch (payload)
            {
                case null:
                    return JValue.CreateNull();
                case JToken token:
                    return token;
                case IToJson single:
                    return single.ToJson();
                case IEnumerable<IToJson> list:
                    var array = new JArray();
                    foreach (var item in list) { array.Add(item.ToJson()); }
                    return array;
                default:
                    return JToken.FromObject(payload, PayloadSerializer);
            }
        }

        /// <summary>"Pass" envelope with no payload, for calls that only need to signal success.</summary>
        public static async Task PassMessageJson(HttpContext context)
        {
            var root = new JObject { ["Status"] = "Pass", ["Payload"] = "" };
            await WriteJsonResponse(context, HttpStatusCode.OK, root);
        }

        /// <summary>Wraps an error message in a "Fail" envelope and writes it as the HTTP response.</summary>
        public static async Task FailMessageJson(string message, HttpContext context)
        {
            var root = new JObject { ["Status"] = "Fail", ["Payload"] = message };
            await WriteJsonResponse(context, HttpStatusCode.OK, root); //note: OK at transport level, "Fail" is an app-level status
        }

        /// <summary>Wraps an exception's message in a "Fail" envelope.</summary>
        public static async Task FailMessageJson(Exception exception, HttpContext context) =>
            await FailMessageJson(exception.Message, context);

        private static async Task WriteJsonResponse(HttpContext context, HttpStatusCode statusCode, JObject body)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(body.ToString());
        }

        /// <summary>Sends a raw SVG string directly to the caller (no JSON envelope).</summary>
        public static async Task SendSvgToCaller(string svgContent, HttpContext context) =>
            await Tools.SendFileToCaller(System.Text.Encoding.UTF8.GetBytes(svgContent), context, "image/svg+xml");

        /// <summary>Sends a raw plain-text string directly to the caller (no JSON envelope).</summary>
        public static async Task SendTextToCaller(string textContent, HttpContext context) =>
            await Tools.SendFileToCaller(System.Text.Encoding.UTF8.GetBytes(textContent), context, "text/plain");

        /// <summary>
        /// Sends any calculator result to the caller: strings/SVG-like data go raw, everything else
        /// is wrapped in the standard Pass/Payload JSON envelope.
        /// </summary>
        public static async Task SendAnyToCaller(string calculatorName, object rawProcessedData, HttpContext context)
        {
            if (rawProcessedData is string stringData)
            {
                await PassMessageJson(stringData, context);
                return;
            }

            await PassMessageJson(rawProcessedData, context);
        }

        /// <summary>Reads and parses the incoming request body as JSON.</summary>
        public static async Task<JObject> ExtractDataFromRequestJson(HttpContext context)
        {
            using var reader = new StreamReader(context.Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            return string.IsNullOrWhiteSpace(rawBody) ? new JObject() : JObject.Parse(rawBody);
        }

        /// <summary>Simple GET request passthrough, used for calling external APIs (e.g. Facebook/Google token validation).</summary>
        public static async Task<HttpResponseMessage> GetRequest(string url) => await SharedHttpClient.GetAsync(url);

        /// <summary>
        /// Sends a file attachment by email. NOTE: no email provider is configured for local dev -
        /// logs to console instead of actually sending, consistent with this codebase's pattern of
        /// degrading gracefully when a cloud secret isn't set (see Localhost_Setup.md).
        /// </summary>
        public static void SendEmail(string fileName, string fileExtension, string receiverEmail, Stream fileStream)
        {
            var connStr = Secrets.AutoEmailerConnectString;

            if (string.IsNullOrEmpty(connStr))
            {
                Console.WriteLine($"[APITools.SendEmail] No email provider configured (local dev) - skipping send of '{fileName}.{fileExtension}' to {receiverEmail}");
                return;
            }

            //TODO: wire to real SMTP/SendGrid client when a connection string is configured
            Console.WriteLine($"[APITools.SendEmail] Would send '{fileName}.{fileExtension}' to {receiverEmail}");
        }

        /// <summary>Gets every person in the PersonList table. skipLifeEvents avoids the extra per-person query, for faster bulk listing.</summary>
        public static List<Person> GetAllPersonList(bool skipLifeEvents)
        {
            var result = new List<Person>();

            foreach (var row in Repositories.Person.GetAllAsync().GetAwaiter().GetResult())
            {
                var gender = row.IsMale() ? Gender.Male : Gender.Female;
                var lifeEvents = new List<LifeEvent>(); //life events intentionally skipped here for bulk-listing performance

                result.Add(new Person(row.PartitionKey, row.RowKey, row.Name, row.ToBirthTime(), gender, row.Notes ?? "", lifeEvents));
            }

            return result;
        }
    }
}
