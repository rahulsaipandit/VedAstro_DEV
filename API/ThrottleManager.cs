using Microsoft.AspNetCore.Http;
using VedAstro.Library;
using System.Text.RegularExpressions;
using System.Linq;

namespace API
{
    /// <summary>
    /// Handles all logic related to controlling API call rate based on IP & caller type
    /// </summary>
    public static class ThrottleManager
    {

        /// <summary>
        /// if API key is present allows full speed call, else only allows slowed speed call except if browser
        /// </summary>
        public static async Task<string> HandleCall(HttpRequest incomingRequest, string fullParamString)
        {
            // if browser then let call through
            if (IsBrowser(incomingRequest)) { return fullParamString; }

            //not browser, check for API key
            else
            {
                // Extract the API key from the fullParamString
                string apiKey = ExtractAPIKey(ref fullParamString);
                if (APIKeyIsValid(apiKey)) //check if key is registered to user
                {
                    // Call contains valid API key, allow full access
                    // Make a record of the call count by API key (Postgres, was Azure Table Storage)
                    await RecordAPISubscriberCall(apiKey);
                }
                else
                {
                    //not valid KEY throttle call
                    await ThrottleCallBasedOnIp(incomingRequest);
                }

                return fullParamString;
            }
        }




        //------------------------- PRIVATE METHODS ---------------------------------

        /// <summary>
        /// Checks if API is registered to a user TODO api key checking is paused
        /// </summary>
        private static bool APIKeyIsValid(string apiKey)
        {
            // If the API key is null or empty, it's not valid
            if (string.IsNullOrEmpty(apiKey)) { return false; }
            else
            {
                return true;
            }
            //// api key checking is paused, same as before migration -
            //// see UserDataListEntity.APIKey / Repositories.UserData for the real lookup if
            //// this gets un-paused later.
        }


        /// <summary>
        /// True if caller is a browser
        /// </summary>
        private static bool IsBrowser(HttpRequest incomingRequest)
        {
            // Extract the User-Agent header
            if (incomingRequest.Headers.TryGetValue("User-Agent", out var userAgentValues))
            {
                var userAgent = userAgentValues.FirstOrDefault();

                // Check if the User-Agent corresponds to a known browser
                if (!IsBrowserUserAgent(userAgent))
                {
                    // Not a browser, deny access
                    return false;
                }
            }
            else
            {
                // No User-Agent header, not browser
                return false;
            }

            return true;
        }


        /// <summary>
        /// List of common browser identifiers in User-Agent strings (placed here for performance benefits)
        /// </summary>
        private static readonly Regex BrowserPattern = new Regex(
            @"(firefox|msie|trident|chrome|safari|edge|opera|opr|edg(?:e|ium))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Determines whether the User-Agent string corresponds to a known browser.
        /// </summary>
        private static bool IsBrowserUserAgent(string? userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

            // Use regex to check if the User-Agent matches any known browser pattern (case-insensitive)
            return BrowserPattern.IsMatch(userAgent);
        }


        /// <summary>
        /// Throttles API call processing for requests lacking a valid API key.
        /// This method applies a delay based on the caller's IP address and their call frequency
        /// over the last 30 seconds, helping to mitigate abuse.
        /// It retrieves a configured delay, counts recent calls from the IP,
        /// calculates the total delay, and applies it using Task.Delay
        /// to control the request rate.
        /// </summary>
        private static async Task ThrottleCallBasedOnIp(HttpRequest incomingRequest)
        {
            // Step 1: Get delay from environment settings
            var threshold = GetCallCountThresholdFromSettings();
            if (threshold == 0) return; //(don't control if not specified, to support private servers)

            // Step 2: Get the caller's IP address
            var ipAddress = incomingRequest?.GetCallerIp()?.ToString() ?? "0.0.0.0";

            // Step 3: Update the call count for this IP address
            await RecordAnonymousIPCall(ipAddress);

            // Step 4: Get the call count for this IP address
            var callCount = await GetCallsInLast60Seconds(ipAddress);

            // Step 5: if to many calls then slow down call
            if (callCount > threshold)
            {
                // Apply the delay
                await Task.Delay(TimeSpan.FromSeconds(100));
            }

        }

        /// <summary>
        /// record call by IP address (Postgres, was Azure Table Storage)
        /// </summary>
        private static async Task RecordAnonymousIPCall(string ipAddress)
        {
            var entity = new AnonymousIpCallRecordEntity
            {
                PartitionKey = ipAddress,
                RowKey = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow
            };

            await Repositories.AnonymousIpCallRecords.AddAsync(entity);
        }


        /// <summary>
        /// Gets the number of calls made by the specified IP address in the last 60 seconds.
        /// Also cleans up records older than 60 seconds and records older than 1 hour.
        /// </summary>
        private static async Task<int> GetCallsInLast60Seconds(string ipAddress)
        {
            // Define the time windows
            var currentTime = DateTime.UtcNow;
            var timeWindowStart = currentTime.AddSeconds(-60);
            var oneHourAgo = currentTime.AddHours(-1);

            // Query for entities within the 1-hour window
            var entities = Repositories.AnonymousIpCallRecords.Query()
                .Where(entity => entity.PartitionKey == ipAddress && entity.Timestamp >= oneHourAgo)
                .ToList();

            var callsInLast60Seconds = 0;

            foreach (var entity in entities)
            {
                if (entity.Timestamp < timeWindowStart)
                {
                    await Repositories.AnonymousIpCallRecords.DeleteAsync(entity.PartitionKey, entity.RowKey);
                }
                else
                {
                    callsInLast60Seconds++;
                }
            }

            return callsInLast60Seconds;
        }



        /// <summary>
        /// Get delay in seconds from API server settings if specified else returns 0 seconds
        /// </summary>
        private static double GetCallCountThresholdFromSettings()
        {
            // Retrieve the environment variable
            var thresholdStr = Environment.GetEnvironmentVariable("AnonymousIpCallThreshold");

            // Initialize delaySeconds to 0 by default
            int delaySeconds = 0;

            // Attempt to parse the environment variable, if it exists
            if (!string.IsNullOrEmpty(thresholdStr))
            {
                if (!int.TryParse(thresholdStr, out delaySeconds))
                {
                    // Parsing failed; use the default value
                    delaySeconds = 0;
                }
            }

            return delaySeconds;
        }

        /// <summary>
        /// Extracts out API key from given URL param string if any
        /// </summary>
        private static string ExtractAPIKey(ref string path)
        {
            //if no param string continue as empty
            if (path == null) { return ""; }

            // Split the path into segments, preserving empty entries to keep leading/trailing slashes
            var segments = path.Split(new char[] { '/' }, StringSplitOptions.None).ToList();
            string apiKey = null;
            int indexOfAPIKey = segments.IndexOf("APIKey");
            if (indexOfAPIKey != -1)
            {
                // Remove 'APIKey' from the segments
                segments.RemoveAt(indexOfAPIKey);
                // Check if there is a next segment for the value
                if (indexOfAPIKey < segments.Count)
                {
                    apiKey = segments[indexOfAPIKey];
                    segments.RemoveAt(indexOfAPIKey);
                }
            }
            // Rebuild the path from the remaining segments
            path = string.Join("/", segments);
            return apiKey;
        }


        /// <summary>
        /// Records the API call count for a given API key (Postgres, was Azure Table Storage).
        /// </summary>
        private static async Task RecordAPISubscriberCall(string apiKey)
        {
            var existing = await Repositories.SubscriberCallRecords.GetAsync(apiKey, "");

            if (existing != null)
            {
                existing.CallCount++;
                await Repositories.SubscriberCallRecords.UpsertAsync(existing);
            }
            else
            {
                var entity = new SubscriberCallRecordEntity
                {
                    PartitionKey = apiKey,
                    RowKey = "",
                    CallCount = 1,
                    Timestamp = DateTimeOffset.UtcNow
                };
                await Repositories.SubscriberCallRecords.AddAsync(entity);
            }
        }

    }
}
