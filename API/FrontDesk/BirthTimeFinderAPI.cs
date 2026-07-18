using VedAstro.Library;

namespace API
{
    /// <summary>
    /// Group of API calls related to finding birth based on dictionary attack on time and other methods
    /// </summary>
    public static class BirthTimeFinderAPI
    {
        public static void MapBirthTimeFinderEndpoints(this WebApplication app)
        {
            app.MapGet("/api/FindBirthTime/EventsChart/PersonId/{personId}", async (HttpContext context, string personId) =>
            {
                try
                {
                    //get person specified by caller
                    var foundPerson = Tools.GetPersonById(personId);

                    //generate the needed charts
                    var eventTags = new List<EventTag> { EventTag.PD1, EventTag.PD2, EventTag.PD3, EventTag.PD4, EventTag.PD5, EventTag.Gochara };
                    var algorithmFuncsList = new List<AlgorithmFuncs>() { Algorithm.General };
                    var summaryOptions = new ChartOptions(algorithmFuncsList);

                    //time range is preset to full life 100 years from birth
                    var start = foundPerson.BirthTime;
                    var end = foundPerson.BirthTime.AddYears(100);
                    var timeRange = new TimeRange(start, end);

                    //calculate based on max screen width,
                    var daysPerPixel = EventsChart.GetDayPerPixel(timeRange, 1500);

                    //get list of possible birth time slice in the current birth day
                    var possibleTimeList = Tools.GetTimeSlicesOnBirthDay(foundPerson, 1);

                    var combinedSvg = "";
                    var chartYPosition = 30; //start with top padding
                    var leftPadding = 10;
                    foreach (var possibleTime in possibleTimeList)
                    {
                        //replace original birth time
                        var personAdjusted = foundPerson.ChangeBirthTime(possibleTime);
                        var newChart = EventsChartFactory.GenerateEventsChart(personAdjusted, timeRange, daysPerPixel, eventTags, summaryOptions);
                        var adjustedBirth = personAdjusted.BirthTimeString;

                        //place in group with time above the chart
                        var wrappedChart = $@"
                                <g transform=""matrix(1, 0, 0, 1, {leftPadding}, {chartYPosition})"">
                                    <text style=""font-size: 16px; white-space: pre-wrap;"" x=""2"" y=""-6.727"">{adjustedBirth}</text>
                                    {newChart.ContentSvg}
                                  </g>
                                ";

                        //combine charts together
                        combinedSvg += wrappedChart;

                        //next chart goes below this one
                        chartYPosition += 390;
                    }

                    //put all charts in 1 big container
                    var finalSvg = EventsChartFactory.WrapSvgElements(
                        svgClass: "MultipleDasa",
                        combinedSvgString: combinedSvg,
                        svgWidth: 800,
                        svgTotalHeight: chartYPosition,
                        randomId: Tools.GenerateId(),
                        svgBackgroundColor: "#757575"); //grey easy on the eyes

                    //send image back to caller
                    await APITools.SendSvgToCaller(finalSvg, context);
                }
                catch (Exception e)
                {
                    //log error
                    APILogger.Error(e, context.Request);

                    //format error nicely to show user
                    await APITools.FailMessageJson(e, context);
                }
            });
        }
    }
}
