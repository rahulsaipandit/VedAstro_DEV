using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace VedAstro.Library
{
    /// <summary>
    /// Was HttpRequestData (Azure Functions Worker) based - now HttpRequest (ASP.NET Core),
    /// since the API host moved off Azure Functions. Data access moved from directly-built
    /// Azure TableServiceClient/TableClient fields (bypassing AzureTable.cs entirely) to the
    /// Repositories locator (Postgres-backed). The 6 statistic loggers below were previously
    /// declared but never wired up/reachable (their table client fields were never assigned) -
    /// they're now real, working loggers, though still not called from Log() by default (kept
    /// commented out exactly as before, so behavior doesn't change unless a human opts back in).
    /// </summary>
    public static class ApiStatistic
    {
        /// <summary>
        /// sample holder type when doing interop
        /// </summary>
        public record GeoLocationRawAPI(dynamic MainRow, dynamic MetadataRow);

        //-------------------------------------

        /// <summary>
        /// Logs IP to for statistics
        /// </summary>
        public static void LogIpAddress(HttpRequest incomingRequest)
        {
            // Step 1: Get the current month and year in the format "yyyy-MM"
            var todayRecord = DateTime.Now.ToString("yyyy-MM");

            // Step 2: Get the caller's IP address (or use "0.0.0.0" if not available)
            var ipAddress = incomingRequest?.GetCallerIp()?.ToString() ?? "0.0.0.0";

            // Step 3: Check if the IP address already exists in the table
            var recordFound = Repositories.IpAddressStatistic.Query()
                .FirstOrDefault(call => call.PartitionKey == ipAddress && call.RowKey == todayRecord);

            // If the IP address exists, update call statistics
            if (recordFound != null)
            {

                // Calculate calls per second
                if (recordFound.PerSecondTimestamp == null ||
                    ((DateTimeOffset.UtcNow - recordFound.PerSecondTimestamp.Value).TotalSeconds >= 60))
                {
                    recordFound.CallsPerSecond = 1;
                    recordFound.PerSecondTimestamp = DateTimeOffset.UtcNow;
                }
                else
                {
                    recordFound.CallsPerSecond++;
                }

                // Calculate calls per minute
                if (recordFound.PerMinuteTimestamp == null ||
                    ((DateTimeOffset.UtcNow - recordFound.PerMinuteTimestamp.Value).TotalMinutes >= 1))
                {
                    recordFound.CallsPerMinute = recordFound.CallsPerSecond;
                    recordFound.CallsPerSecond = 0;
                    recordFound.PerSecondTimestamp = null;
                    recordFound.PerMinuteTimestamp = DateTimeOffset.UtcNow;
                }
                else
                {
                    recordFound.CallsPerMinute += recordFound.CallsPerSecond;
                    recordFound.CallsPerSecond = 0;
                }

                // Calculate calls per hour
                if (recordFound.PerHourTimestamp == null ||
                    ((DateTimeOffset.UtcNow - recordFound.PerHourTimestamp.Value).TotalHours >= 1))
                {
                    recordFound.CallsPerHour = recordFound.CallsPerMinute;
                    recordFound.CallsPerMinute = 0;
                    recordFound.PerMinuteTimestamp = null;
                    recordFound.PerHourTimestamp = DateTimeOffset.UtcNow;
                }
                else
                {
                    recordFound.CallsPerHour += recordFound.CallsPerMinute;
                    recordFound.CallsPerMinute = 0;
                }

                // Calculate calls per day
                if (recordFound.PerDayTimestamp == null ||
                    ((DateTimeOffset.UtcNow - recordFound.PerDayTimestamp.Value).TotalDays >= 1))
                {
                    recordFound.CallsPerDay = recordFound.CallsPerHour;
                    recordFound.CallsPerHour = 0;
                    recordFound.PerHourTimestamp = null;
                    recordFound.PerDayTimestamp = DateTimeOffset.UtcNow;
                }
                else
                {
                    recordFound.CallsPerDay += recordFound.CallsPerHour;
                    recordFound.CallsPerHour = 0;
                }

                // Calculate calls per month
                if (recordFound.PerMonthTimestamp == null ||
                    ((DateTimeOffset.UtcNow - recordFound.PerMonthTimestamp.Value).TotalDays >= DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month)))
                {
                    recordFound.CallsPerMonth = recordFound.CallsPerDay;
                    recordFound.CallsPerDay = 0;
                    recordFound.PerDayTimestamp = null;
                    recordFound.PerMonthTimestamp = DateTimeOffset.UtcNow;
                }
                else
                {
                    recordFound.CallsPerMonth += recordFound.CallsPerDay;
                    recordFound.CallsPerDay = 0;
                }

                // Update the entity in the table
                Repositories.IpAddressStatistic.UpsertAsync(recordFound).GetAwaiter().GetResult();
            }
            else
            {
                //Create a new log entry for the IP address
                var newRow = new IpAddressStatisticEntity();
                newRow.PartitionKey = Tools.CleanAzureTableKey(ipAddress);
                newRow.RowKey = todayRecord;
                Repositories.IpAddressStatistic.AddAsync(newRow).GetAwaiter().GetResult();
            }

        }
        public static void LogWebPage(string webPage)
        {
            //get month and year in correct format 2019-10
            var todayRecord = DateTime.Now.ToString("yyyy-MM");

            var cleanWebPageUrl = Tools.CleanAzureTableKey(webPage);

            //execute search
            var recordFound = Repositories.WebPageStatistic.Query()
                .FirstOrDefault(call => call.PartitionKey == cleanWebPageUrl && call.RowKey == todayRecord);

            //# if existed, update call count
            var isExist = recordFound != null;
            if (isExist)
            {
                //update row
                recordFound.CallCount = ++recordFound.CallCount; //increment call count
                Repositories.WebPageStatistic.UpsertAsync(recordFound).GetAwaiter().GetResult();
            }

            //# if not exist, make new log
            else
            {
                var newRow = new WebPageStatisticEntity();

                newRow.PartitionKey = cleanWebPageUrl;
                //get month and year in correct format 2019-10
                newRow.RowKey = todayRecord;
                newRow.CallCount = 1;
                Repositories.WebPageStatistic.AddAsync(newRow).GetAwaiter().GetResult();
            }
        }

        public static void LogRequestUrl(HttpRequest incomingRequest)
        {

            //# get request URL
            var requestUrl = incomingRequest?.Path.ToString() ?? "no URL";

            //get month and year in correct format 2019-10
            var todayRecord = DateTime.Now.ToString("yyyy-MM");

            //# check if URL already exist
            //make a search for ip address stored under row key
            var cleanAzureTableKey = Tools.CleanAzureTableKey(requestUrl, "-").Truncate(100); //keep short as not overcrowd

            //execute search
            var recordFound = Repositories.RequestUrlStatistic.Query()
                .FirstOrDefault(call => call.PartitionKey == cleanAzureTableKey && call.RowKey == todayRecord);

            //# if existed, update call count
            var isExist = recordFound != null;
            if (isExist)
            {
                //update row
                recordFound.CallCount = ++recordFound.CallCount; //increment call count
                Repositories.RequestUrlStatistic.UpsertAsync(recordFound).GetAwaiter().GetResult();
            }

            //# if not exist, make new log
            else
            {
                var newRow = new RequestUrlStatisticEntity();

                newRow.PartitionKey = cleanAzureTableKey;
                //get month and year in correct format 2019-10
                newRow.RowKey = todayRecord;
                newRow.CallCount = 1;
                Repositories.RequestUrlStatistic.AddAsync(newRow).GetAwaiter().GetResult();
            }
        }

        public static void LogSubscriber(HttpRequest incomingRequest)
        {
            //get host address as main ID of record
            var host = incomingRequest.ExtractHostAddress();

            //get date that this record would be in (Row Key)
            var currentDate = DateTime.Now.ToString("yyyy-MM");

            //# check if URL already exist
            //make a search for ip address stored under row key
            var cleanHostAddress = Tools.CleanAzureTableKey(host, "|");

            //execute search
            var recordFound = Repositories.SubscriberStatistic.Query()
                .FirstOrDefault(call => call.PartitionKey == cleanHostAddress && call.RowKey == currentDate);

            //# if existed, update call count
            var isExist = recordFound != null;
            if (isExist)
            {
                //update row
                recordFound.CallCount = ++recordFound.CallCount; //increment call count
                Repositories.SubscriberStatistic.UpsertAsync(recordFound).GetAwaiter().GetResult();
            }

            //# if not exist, make new log
            else
            {
                var newRow = new SubscriberStatisticEntity();
                newRow.PartitionKey = cleanHostAddress;
                //get month and year in correct format 2019-10
                newRow.RowKey = currentDate;
                newRow.CallCount = 1; //start with 1
                //save to db
                Repositories.SubscriberStatistic.AddAsync(newRow).GetAwaiter().GetResult();
            }
        }

        public static void LogUserAgent(HttpRequest incomingRequest)
        {
            //get host address as main ID of record
            var userAgent = incomingRequest.Headers.TryGetValue("User-Agent", out var userAgentValues)
                ? userAgentValues.FirstOrDefault() ?? "no User-Agent"
                : "no User-Agent";

            //get date that this record would be in (Row Key)
            var currentDate = DateTime.Now.ToString("yyyy-MM");

            //# check if User-Agent already exist
            //make a search for ip address stored under row key
            var cleanUserAgent = Tools.CleanAzureTableKey(userAgent, "|");

            //execute search
            var recordFound = Repositories.UserAgentStatistic.Query()
                .FirstOrDefault(call => call.PartitionKey == cleanUserAgent && call.RowKey == currentDate);

            //# if existed, update call count
            var isExist = recordFound != null;
            if (isExist)
            {
                //update row
                recordFound.CallCount = ++recordFound.CallCount; //increment call count
                Repositories.UserAgentStatistic.UpsertAsync(recordFound).GetAwaiter().GetResult();
            }

            //# if not exist, make new log
            else
            {
                var newRow = new UserAgentStatisticEntity();
                newRow.PartitionKey = cleanUserAgent;
                //get month and year in correct format 2019-10
                newRow.RowKey = currentDate;
                newRow.CallCount = 1; //start with 1
                //save to db
                Repositories.UserAgentStatistic.AddAsync(newRow).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Makes raw full header log of what ever that comes in
        /// NOTE: high cost carefully use
        /// </summary>
        public static void LogRawRequest(HttpRequest incomingRequest)
        {
            //step 1: extract needed data from request
            var newRow = new RawRequestStatisticEntity();

            for (int i = 0; i < incomingRequest.Headers.Count; i++)
            {
                var currentHeader = incomingRequest.Headers.ElementAt(i);
                var currentHeaderKey = currentHeader.Key;
                string currentValue = string.Join(",", currentHeader.Value.ToArray());

                //match with correct header based on attribute and fill in the value
                // Get all properties of the current instance
                var properties = newRow.GetType().GetProperties();
                foreach (var property in properties)
                {
                    var attribute = (DescriptionAttribute)property.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault();
                    if (attribute?.Description.Equals(currentHeaderKey, StringComparison.OrdinalIgnoreCase) ?? false)
                    {
                        property.SetValue(newRow, currentValue);
                        break;
                    }
                }
            }

            //step 2: generate hash to identify the data
            newRow.PartitionKey = incomingRequest?.GetCallerIp()?.ToString() ?? "no ip";
            var url = incomingRequest != null ? $"{incomingRequest.Path}{incomingRequest.QueryString}" : "no URL";
            newRow.RowKey = Tools.CleanAzureTableKey(url, "|"); //place url

            //step 3: add entry to database
            //TODO check if exist before overwrite
            Repositories.RawRequestStatistic.UpsertAsync(newRow).GetAwaiter().GetResult();
        }

        private static bool IsLoggingEnabled()
        {
            // Retrieve the environment variable
            var isEnabled = Environment.GetEnvironmentVariable("EnableLogging");

            // If the variable is not defined, default to false
            if (string.IsNullOrEmpty(isEnabled))
            {
                return false;
            }

            // If the variable is defined, try to parse it as a boolean
            if (bool.TryParse(isEnabled, out bool result))
            {
                return result;
            }

            // If the variable cannot be parsed as a boolean, default to false
            return false;
        }

        public static void LogCallInfo(HttpRequest incomingRequest)
        {
            // Only continue if logging is enabled
            if (!IsLoggingEnabled())
            {
                return;
            }

            // Step 1: Extract needed data from request
            var callerIp = incomingRequest?.GetCallerIp()?.ToString() ?? "no ip";
            var userAgent = incomingRequest.Headers.TryGetValue("User-Agent", out var userAgentValues)
                ? userAgentValues.FirstOrDefault() ?? "no User-Agent"
                : "no User-Agent";
            var requestUrl = incomingRequest?.Path.ToString() ?? "no URL";

            // Step 3: Create a new log entry
            var callInfoEntity = new CallInfoStatisticEntity
            {
                PartitionKey = callerIp,
                RowKey = Guid.NewGuid().ToString(),
                UserAgent = userAgent,
                RequestUrl = requestUrl,
                Timestamp = DateTime.UtcNow
            };

            // Step 4: Add the log entry to the database
            Repositories.CallInfoStatistic.AddAsync(callInfoEntity).GetAwaiter().GetResult();
        }

        public static void Log(HttpRequest incomingRequest)
        {
            ApiStatistic.LogCallInfo(incomingRequest);
            //ApiStatistic.LogIpAddress(incomingRequest);
            //ApiStatistic.LogRequestUrl(incomingRequest);
            //ApiStatistic.LogRawRequest(incomingRequest);
            //ApiStatistic.LogSubscriber(incomingRequest);
            //ApiStatistic.LogUserAgent(incomingRequest);

        }
    }

}
