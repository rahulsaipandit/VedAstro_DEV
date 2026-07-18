using VedAstro.Library;

namespace API
{
    public static class WebsiteLoggerAPI
    {
        public static void MapWebsiteLoggerEndpoints(this WebApplication app)
        {
            /// <summary>
            /// Logs errors from website
            /// </summary>
            app.MapPost("/api/LogError", async (HttpContext context) =>
            {
                // Read error details from request body
                var errorDetails = await context.Request.ReadFromJsonAsync<ErrorDetails>();

                // Save error details to Postgres (was Azure Table Storage)
                var entity = new WebsiteErrorLogEntity
                {
                    PartitionKey = errorDetails.UserId,
                    RowKey = errorDetails.LocalTime,
                    ErrorMessage = errorDetails.Message,
                    StackTrace = errorDetails.Stack,
                    Url = errorDetails.Url,
                    UserAgent = errorDetails.UserAgent,
                    Timestamp = DateTime.UtcNow
                };
                await Repositories.WebsiteErrorLog.AddAsync(entity);
            });

            /// <summary>
            /// Logs general info from the website for debug purposes
            /// </summary>
            app.MapPost("/api/LogDebug", async (HttpContext context) =>
            {
                // Read debug details from request body
                var debugDetails = await context.Request.ReadFromJsonAsync<DebugDetails>();

                // Save debug details to Postgres (was Azure Table Storage)
                var entity = new WebsiteDebugLogEntity
                {
                    PartitionKey = debugDetails.UserId,
                    RowKey = debugDetails.LocalTime,
                    Message = debugDetails.Message,
                    Url = debugDetails.Url,
                    UserAgent = debugDetails.UserAgent,
                    Timestamp = DateTime.UtcNow
                };
                await Repositories.WebsiteDebugLog.AddAsync(entity);
            });
        }
    }

    // New class for DebugDetails
    public class DebugDetails
    {
        public string UserId { get; set; }
        public string LocalTime { get; set; }

        public string Url { get; set; }
        public string Message { get; set; }

        public string UserAgent { get; set; }
    }

    public class ErrorDetails
    {
        public string UserId { get; set; }
        public string LocalTime { get; set; }
        public string Url { get; set; }
        public string Message { get; set; }
        public string Stack { get; set; }
        public string UserAgent { get; set; }
    }

}
