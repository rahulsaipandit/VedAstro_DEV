using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;
using VedAstro.Library;

namespace API
{
    /// <summary>
    /// Common HTTP request/response helpers used across the Azure Functions endpoints in FrontDesk.
    /// Reconstructed from scratch - see Library/Logic/Calculate/CoreTime.cs header note in the Library
    /// project for the equivalent situation there.
    /// Response shape matches WebResult&lt;T&gt;'s expected wire format: {"Status": "Pass"/"Fail", "Payload": ...}
    /// </summary>
    public static class APITools
    {
        private static readonly HttpClient SharedHttpClient = new();

        /// <summary>Wraps a JSON-serializable payload in a "Pass" envelope and returns it as the HTTP response.</summary>
        public static HttpResponseData PassMessageJson(object payload, HttpRequestData incomingRequest)
        {
            var root = new JObject
            {
                ["Status"] = "Pass",
                ["Payload"] = payload is JToken token ? token : JToken.FromObject(payload)
            };

            return WriteJsonResponse(incomingRequest, HttpStatusCode.OK, root);
        }

        /// <summary>"Pass" envelope with no payload, for calls that only need to signal success.</summary>
        public static HttpResponseData PassMessageJson(HttpRequestData incomingRequest)
        {
            var root = new JObject { ["Status"] = "Pass", ["Payload"] = "" };
            return WriteJsonResponse(incomingRequest, HttpStatusCode.OK, root);
        }

        /// <summary>Wraps an error message in a "Fail" envelope and returns it as the HTTP response.</summary>
        public static HttpResponseData FailMessageJson(string message, HttpRequestData incomingRequest)
        {
            var root = new JObject { ["Status"] = "Fail", ["Payload"] = message };
            return WriteJsonResponse(incomingRequest, HttpStatusCode.OK, root); //note: OK at transport level, "Fail" is an app-level status
        }

        /// <summary>Wraps an exception's message in a "Fail" envelope.</summary>
        public static HttpResponseData FailMessageJson(Exception exception, HttpRequestData incomingRequest) =>
            FailMessageJson(exception.Message, incomingRequest);

        private static HttpResponseData WriteJsonResponse(HttpRequestData incomingRequest, HttpStatusCode statusCode, JObject body)
        {
            var response = incomingRequest.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(body.ToString());
            return response;
        }

        /// <summary>Sends a raw SVG string directly to the caller (no JSON envelope).</summary>
        public static HttpResponseData SendSvgToCaller(string svgContent, HttpRequestData incomingRequest) =>
            Tools.SendFileToCaller(System.Text.Encoding.UTF8.GetBytes(svgContent), incomingRequest, "image/svg+xml");

        /// <summary>Sends a raw plain-text string directly to the caller (no JSON envelope).</summary>
        public static HttpResponseData SendTextToCaller(string textContent, HttpRequestData incomingRequest) =>
            Tools.SendFileToCaller(System.Text.Encoding.UTF8.GetBytes(textContent), incomingRequest, "text/plain");

        /// <summary>
        /// Sends any calculator result to the caller: strings/SVG-like data go raw, everything else
        /// is wrapped in the standard Pass/Payload JSON envelope.
        /// </summary>
        public static HttpResponseData SendAnyToCaller(string calculatorName, object rawProcessedData, HttpRequestData incomingRequest)
        {
            if (rawProcessedData is string stringData)
            {
                return PassMessageJson(stringData, incomingRequest);
            }

            return PassMessageJson(rawProcessedData, incomingRequest);
        }

        /// <summary>Reads and parses the incoming request body as JSON.</summary>
        public static async Task<JObject> ExtractDataFromRequestJson(HttpRequestData incomingRequest)
        {
            using var reader = new StreamReader(incomingRequest.Body);
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

            //TODO: wire to real Azure Communication Services / SendGrid client when a connection string is configured
            Console.WriteLine($"[APITools.SendEmail] Would send '{fileName}.{fileExtension}' to {receiverEmail}");
        }

        /// <summary>Gets every person in the PersonList table. skipLifeEvents avoids the extra per-person query, for faster bulk listing.</summary>
        public static List<Person> GetAllPersonList(bool skipLifeEvents)
        {
            var result = new List<Person>();

            foreach (var row in AzureTable.PersonList.Query<PersonListEntity>())
            {
                var gender = row.IsMale() ? Gender.Male : Gender.Female;
                var lifeEvents = new List<LifeEvent>(); //life events intentionally skipped here for bulk-listing performance

                result.Add(new Person(row.PartitionKey, row.RowKey, row.Name, row.ToBirthTime(), gender, row.Notes ?? "", lifeEvents));
            }

            return result;
        }
    }
}
