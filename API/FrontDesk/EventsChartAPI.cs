using VedAstro.Library;
using Newtonsoft.Json.Linq;

namespace API
{

    public static class EventsChartAPI
    {
        public static void MapEventsChartEndpoints(this WebApplication app)
        {
            //▄▀█ █▀█ █   █▀▀ █░█ █▄░█ █▀▀ ▀█▀ █ █▀█ █▄░█ █▀
            //█▀█ █▀▀ █   █▀░ █▄█ █░▀█ █▄▄ ░█░ █ █▄█ █░▀█ ▄█

            /// Gets all saved/cached chart names for a person,
            /// NOTE: user than selects the chart, with data,
            /// when called via generate will auto get the cached chart
            app.MapGet("/api/SavedChartNameList/{personId}", async (HttpContext context, string personId) =>
            {
                try
                {
                    //get all in cache
                    var allSavedCharts = ChartCache.ListBlobs(personId);

                    var eventDataList = new JArray();
                    foreach (var chartName in allSavedCharts)
                    {
                        EventsChart parsedChart = await EventsChart.FromCacheName(chartName);

                        eventDataList.Add(parsedChart.ToJson());
                    }

                    await APITools.PassMessageJson(eventDataList, context);
                }
                //if any failure, show error in payload
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson(e.Message, context);
                }
            });

            /// Main func to generate event charts used by site, via awesome built in cache mechanism
            app.MapGet("/api/EventsChart/{*settingsUrl}", async (HttpContext context, string? settingsUrl) =>
            {
                settingsUrl ??= "";

                try
                {
                    //1 : CUSTOM AYANAMSA (removes ayanamsa once read)
                    settingsUrl = OpenAPI.ParseAndSetAyanamsa(settingsUrl);

                    //get basic spec on how to make chart
                    //check if the specs given is correct and readable
                    //this is partially filled chart with no generated svg content only specs
                    var chartSpecsOnly = await EventsChart.FromUrl(settingsUrl);

                    //a hash to id the chart's specs (caching)
                    var chartId = chartSpecsOnly.GetEventsChartSignature();

                    //PREPARE THE CALL
                    Func<string> generateChart = () =>
                    {
                        var chartSvg = EventsChartFactory.GenerateEventsChartSvg(chartSpecsOnly);
                        return chartSvg;
                    };

                    //NOTE USING CHART ID INSTEAD OF CALLER ID, FOR CACHE SHARING BETWEEN ALL WHO COME
                    Func<Task> cacheExecuteTask = () => ChartCache.ExecuteAndSaveToCache(generateChart, chartId);

                    //CACHE MECHANISM
                    //NOTE: mime type must be passed in up front (rather than mutated on the response
                    //afterwards, like the old HttpResponseData-based version did) since ASP.NET Core
                    //locks response headers as soon as the body starts streaming.
                    var callerInfo = new CallerInfo("101", "101");//disabled because no space to squeeze in URL
                    callerInfo.CallerId = chartId;//NOTE OVERRIDE CALLER ID TO CHART FOR CACHE SHARING
                    await ChartCache.CacheExecute(cacheExecuteTask, callerInfo, context, "image/svg+xml");
                }
                catch (Exception e)
                {
                    //log it
                    APILogger.Error(e);
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 200;
                        context.Response.Headers.Append("Call-Status", "Fail"); //caller checks this
                        context.Response.Headers.Append("Access-Control-Expose-Headers", "Call-Status"); //needed by silly browser to read call-status
                    }
                }

            });

            /// SPECIAL DEBUG version to generate life chart without cache for R & D purposes
            app.MapGet("/api/EventsChartNoCache/{*settingsUrl}", async (HttpContext context, string? settingsUrl) =>
            {
                settingsUrl ??= "";

                try
                {
                    //SET ayanamsa to RAMAN
                    Calculate.Ayanamsa = (int)Ayanamsa.RAMAN;

                    //check if the specs given is correct and readable
                    //this is partially filled chart with no generated svg content only specs
                    var chartSpecsOnly = await EventsChart.FromUrl(settingsUrl);

                    //PREPARE THE CALL
                    var chartSvg = EventsChartFactory.GenerateEventsChartSvg(chartSpecsOnly);

                    //send image back to caller
                    await APITools.SendSvgToCaller(chartSvg, context);

                }
                catch (Exception e)
                {
                    //log it
                    APILogger.Error(e);
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 200;
                        context.Response.Headers.Append("Call-Status", "Fail"); //caller checks this
                        context.Response.Headers.Append("Access-Control-Expose-Headers", "Call-Status"); //needed by silly browser to read call-status
                    }
                }

            });

            /// creates an event chart and send it to email
            /// calculates new does not use cache
            app.MapPost("/api/SendEventsChart/Email/{email}", async (HttpContext context, string email) =>
            {
                try
                {
                    //SET ayanamsa to RAMAN
                    Calculate.Ayanamsa = (int)Ayanamsa.RAMAN;

                    //data comes out of caller, basic spec on how the chart should be
                    var requestJson = await APITools.ExtractDataFromRequestJson(context);

                    //check if the specs given is correct and readable
                    //this is partially filled chart with no generated svg content only specs
                    var chartSpecsOnly = await EventsChart.FromJson(requestJson);

                    //PREPARE THE CALL
                    var foundPerson = Tools.GetPersonById(chartSpecsOnly.Person.Id);
                    var chartSvg = EventsChartFactory.GenerateEventsChartSvg(chartSpecsOnly);

                    //string to binary
                    byte[] rawFileBytes = System.Text.Encoding.UTF8.GetBytes(chartSvg); //SVG uses UTF-8
                    MemoryStream stream = new MemoryStream(rawFileBytes);

                    //using email sender, send file to given email
                    var fileName = $"Chart-{foundPerson.Name}";
                    APITools.SendEmail(fileName, "svg", email, stream);

                    await APITools.PassMessageJson("Email sent success", context);

                }
                catch (Exception e)
                {
                    //log it
                    APILogger.Error(e);
                    if (!context.Response.HasStarted)
                    {
                        context.Response.StatusCode = 200;
                        context.Response.Headers.Append("Call-Status", "Fail"); //caller checks this
                        context.Response.Headers.Append("Access-Control-Expose-Headers", "Call-Status"); //needed by silly browser to read call-status
                    }
                }
            });
        }
    }
}
