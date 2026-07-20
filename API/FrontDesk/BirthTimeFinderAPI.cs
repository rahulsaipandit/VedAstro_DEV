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
                string? endHour = null) =>
            {
                try
                {
                    //get person specified by caller
                    var foundPerson = Tools.GetPersonById(personId);

                    //generate the needed charts
                    var eventTags = new List<EventTag> { EventTag.PD1, EventTag.PD2, EventTag.PD3, EventTag.PD4, EventTag.PD5, EventTag.PD6, EventTag.PD7 };
                    var algorithmFuncsList = new List<AlgorithmFuncs>() { Algorithm.General, Algorithm.IshtaKashtaPhalaDegree, Algorithm.PlanetStrengthDegree };
                    var summaryOptions = new ChartOptions(algorithmFuncsList);

                    //time range defaults to full life (birth date -> birth date + 100 years), caller can override
                    var startDateParsed = startDate ?? foundPerson.BirthDateMonthYear;
                    var endDateParsed = endDate ?? $"{foundPerson.BirthDateMonthYear[..6]}{foundPerson.BirthYear + 100}";
                    var start = new Time($"00:00 {startDateParsed} {foundPerson.BirthTimeZoneString}", foundPerson.GetBirthLocation());
                    var end = new Time($"00:00 {endDateParsed} {foundPerson.BirthTimeZoneString}", foundPerson.GetBirthLocation());
                    var timeRange = new TimeRange(start, end);

                    //calculate based on max screen width,
                    var daysPerPixel = EventsChart.GetDayPerPixel(timeRange, maxWidth);

                    //get list of possible birth times within the given hour range on the birth day (defaults to whole day)
                    var startHourParsed = new Time($"{startHour ?? "00:00"} {foundPerson.BirthDateMonthYearOffset}", foundPerson.GetBirthLocation());
                    var endHourParsed = new Time($"{endHour ?? "23:59"} {foundPerson.BirthDateMonthYearOffset}", foundPerson.GetBirthLocation());
                    var possibleTimeList = Time.GetTimeListFromRange(startHourParsed, endHourParsed, precisionInHours);

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
