using Microsoft.AspNetCore.Http;
using VedAstro.Library;

namespace API;

/// <summary>
/// Custom simple logger for API, auto log to Postgres (was Azure Data Table).
/// </summary>
public static class APILogger
{

    /// <summary>
    /// Adds error log to OpenAPIErrorBook
    /// </summary>
    public static void Error(Exception exception, HttpRequest? incomingRequest = null)
    {
        try
        {
            //summarize exception data
            var exceptionData = Tools.ExceptionToJSON(exception).ToString(); //JSON string

            var errorLog = new OpenAPIErrorBookEntity()
            {
                PartitionKey = incomingRequest?.GetCallerIp()?.ToString() ?? "0.0.0.0",
                RowKey = DateTimeOffset.UtcNow.Ticks.ToString(),
                Branch = ThisAssembly.Version,
                URL = incomingRequest != null ? $"{incomingRequest.Path}{incomingRequest.QueryString}" : "no URL",
                Message = exceptionData
            };

            //creates record if no exist, update if already there
            Repositories.OpenAPIErrorBook.UpsertAsync(errorLog).GetAwaiter().GetResult();

        }
        catch (Exception deeperException)
        {
            //NOTE: to error on error loop, we quietly console
            //out here without stopping execution
            Console.WriteLine(exception.Message);
            Console.WriteLine(deeperException.Message);
        }


    }

}
