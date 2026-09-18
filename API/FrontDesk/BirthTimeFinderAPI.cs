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
            app.MapGet("/api/FindBirthTime/EventsChart/PersonId/{personId}", async (
                HttpContext context,
                string personId,
                int maxWidth = 800,
                double precisionInHours = 1,
                string? startDate = null,
                string? endDate = null,
                string? startHour = null,
                string? endHour = null,
                string ayanamsaName = "Raman") =>
            {
                try
                {
                    //Calculate.Ayanamsa is process-wide global state, so it must be set explicitly on
                    //every request rather than relying on whatever a previous request last left it as.
                    //Raman is the default because the PD1-PD7 Panchadasa dasa periods below assume its
                    //360-day solar year convention (see VimshottariDasa.cs)
                    Calculate.Ayanamsa = (int)Tools.EnumFromUrl($"/Ayanamsa/{ayanamsaName}");

                    //generate the needed charts
                    var eventTags = new List<EventTag> { EventTag.PD1, EventTag.PD2, EventTag.PD3, EventTag.PD4, EventTag.PD5, EventTag.PD6, EventTag.PD7 };
                    var algorithmFuncsList = new List<AlgorithmFuncs>() { Algorithm.General, Algorithm.IshtaKashtaPhalaDegree, Algorithm.PlanetStrengthDegree };
                    var summaryOptions = new ChartOptions(algorithmFuncsList);

                    //get person + candidate birth time sweep (shared with BirthTimeScoringAPI)
                    var sweep = BirthTimeCandidateSweep.Build(personId, startDate, endDate, startHour, endHour, precisionInHours);
                    var timeRange = sweep.LifeTimeRange;

                    //calculate based on max screen width,
                    var daysPerPixel = EventsChart.GetDayPerPixel(timeRange, maxWidth);

                    var combinedSvg = "";
                    var chartYPosition = 30; //start with top padding
                    var leftPadding = 10;
                    foreach (var personAdjusted in BirthTimeCandidateSweep.CandidatePersons(sweep))
                    {
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
                        svgWidth: maxWidth + 100,
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
