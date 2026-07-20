using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace VedAstro.Library
{
    /// <summary>
    /// Cache manager for chart images - was Azure-Blob-backed (formerly "AzureCache"), now
    /// delegates to Repositories.ChartCache (VedAstro.Data.Cache.IChartImageCache, local-disk
    /// backed). Public method surface kept identical to the old blob version so every existing
    /// caller (PersonAPI, EventsChartAPI, etc) keeps working unchanged, EXCEPT CacheExecute, whose
    /// HttpRequestData/HttpResponseData (Azure Functions Worker) parameters/return type changed
    /// to HttpContext (ASP.NET Core) since the API host moved off Azure Functions - its only
    /// caller (API/FrontDesk/EventsChartAPI.cs) was updated at the same time.
    /// </summary>
    public static class ChartCache
    {
        private static VedAstro.Data.Cache.IChartImageCache Cache => Repositories.ChartCache;

        public static List<string> ListBlobs(string searchKeyword) => Cache.ListBlobs(searchKeyword);

        public static async Task<bool> IsExist(string callerId) => await Cache.IsExist(callerId);

        /// <summary>
        /// Given caller ID, returns cached data as the requested type (string or byte[]).
        /// </summary>
        public static async Task<dynamic> GetData<T>(string callerId)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    return await Cache.GetDataString(callerId);
                }
                else if (typeof(T) == typeof(byte[]))
                {
                    return await Cache.GetDataBytes(callerId);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "";
            }

            throw new Exception("END OF LINE!");
        }

        /// <summary>
        /// Given any data type, will add to Cache, with specified name, mimetype is optional
        /// </summary>
        public static async Task Add<T>(string fileName, T value, string mimeType = "", Dictionary<string, string> metadata = null)
        {
#if DEBUG
            Console.WriteLine($"SAVING NEW DATA TO CACHE: {fileName}");
#endif

            if (typeof(T) == typeof(string))
            {
                await Cache.Add(fileName, (value as string) ?? string.Empty, mimeType, metadata);
            }
            else if (typeof(T) == typeof(byte[]))
            {
                await Cache.Add(fileName, (value as byte[]) ?? Array.Empty<byte>(), mimeType, metadata);
            }
        }

        public static async Task Delete(string callerId) => await Cache.Delete(callerId);

        /// <summary>
        /// If got data use that, else do calculations and give that.
        /// Also acts as polling URL, client only has to refresh to poll -
        /// response will auto change to full data file when needed.
        /// NOTE : HEADERS USED TO MARK STATUS PASS OR FAIL
        /// </summary>
        public static async Task CacheExecute(Func<Task> cacheExecuteTask3, CallerInfo callerInfo, HttpContext context, string mimeType = MediaTypeNames.Application.Json)
        {
            //1: CHECK IF RUNNING
            //check if call already made and is running
            //call is made again (polling), don't disturb running
            var isRunning = CallTracker.IsRunning(callerInfo.CallerId);

#if DEBUG
            var status = isRunning ? "ATM RUNNING" : "NOT RUNNING / NON EXIST";
            Console.WriteLine($"CALL IS {status}");
#endif

            //already running end here for quick reply
            if (isRunning)
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.Headers.Append("Call-Status", "Running"); //caller checks this
                context.Response.Headers.Append("Access-Control-Expose-Headers", "Call-Status"); //needed by silly browser to read call-status
                return;
            }
            //start new call
            else
            {
                //2: CHECK IF CACHED
                //if task not running next check cache
                var gotCache = await Cache.IsExist(callerInfo.CallerId);
                if (gotCache)
                {
#if DEBUG
                    Console.WriteLine($"USING CACHE : {callerInfo.CallerId}");
#endif

                    CallTracker.DeleteCall(callerInfo.CallerId); //clean call tracker record

                    var fileStream = Cache.OpenRead(callerInfo.CallerId);
                    await Tools.SendPassHeaderToCaller(fileStream, context, mimeType);
                    return;
                }
                //if no cache only now start task
                else
                {
#if DEBUG
                    Console.WriteLine($"C: NO CACHE! RUNNING COMPUTE : {callerInfo.CallerId}");
#endif
                    //no waiting
                    //will execute and save the data to cache,
                    //so on next call will retrieve from cache
                    _ = cacheExecuteTask3.Invoke();

#if DEBUG
                    Console.WriteLine($"BUSY NOW COME BACK LATER : {callerInfo.CallerId}");
#endif

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.Headers.Append("Call-Status", "Running"); //caller checks this
                    context.Response.Headers.Append("Access-Control-Expose-Headers", "Call-Status"); //needed by silly browser to read call-status
                    return;
                }
            }
        }

        /// <summary>
        /// Relies on cache names having prefix person ID to be detected and deleted
        /// Exp: Travis1985-EventsChart-20010202...
        /// </summary>
        public static async Task DeleteCacheRelatedToPerson(Person newPerson)
        {
            //if empty id, end here
            if (Person.Empty.Equals(newPerson)) { return; }

            await Cache.DeleteCacheRelatedToPerson(newPerson.Id);
        }

        /// <summary>
        /// Relies on cache names having prefix person ID to be detected and deleted
        /// Exp: Travis1985-EventsChart-20010202...
        /// </summary>
        public static async Task DeleteCacheRelatedToPerson(string personId)
        {
            await Cache.DeleteCacheRelatedToPerson(personId);
        }

        /// <summary>
        /// Given a cache generator function and a name for the data
        /// it'll calculate and save data to cache storage
        /// </summary>
        public static async Task ExecuteAndSaveToCache(Func<string> cacheGenerator, string cacheName, string mimeType = "")
        {
#if DEBUG
            Console.WriteLine($"A: NO CACHE! RUNNING COMPUTE : {cacheName}");
#endif

            try
            {
                //lets everybody know call is running
                CallTracker.CallStart(cacheName);

                //squeeze the Sky Juice!
                var chartBytes = cacheGenerator.Invoke();

                //save for future
                await Cache.Add(cacheName, chartBytes, mimeType);
            }
            //always mark the call as ended
            finally
            {
                CallTracker.CallEnd(cacheName); //mark the call as ended
            }
        }

        /// <summary>
        /// Uses cache if available else calculates the data
        /// also auto adds the newly calculated data cache for future
        /// </summary>
        public static async Task<T> CacheExecuteTask<T>(Func<Task<T>> generateChart, string callerId, string mimeType = "")
        {
            //check if cache exist
            var isExist = await Cache.IsExist(callerId);

            T chart;

            if (!isExist)
            {
                //squeeze the Sky Juice!
                chart = await generateChart.Invoke();
                //save for future
                await Add<T>(callerId, chart, mimeType);
            }
            else
            {
                chart = await GetData<T>(callerId);
            }

            return chart;
        }
    }
}
