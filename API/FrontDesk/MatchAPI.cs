using VedAstro.Library;

namespace API
{
    /// <summary>
    /// API with match related stuff
    /// </summary>
    public static class MatchAPI
    {
        public static void MapMatchEndpoints(this WebApplication app)
        {
            app.MapGet("/api/FindMatch/PersonId/{personId}", async (HttpContext context, string personId) =>
            {
                var person = Tools.GetPersonById(personId);

                var personList = await GetAllPersonByMatchStrength(person);

                var returnJson = PersonKutaScore.ToJsonList(personList);

                await APITools.PassMessageJson(returnJson, context);
            });

            // Live-computed one-on-one compatibility report for two specific people (not persisted -
            // see WebsiteNative's Match/Report screen). NOTE: this is scope-limited to what
            // MatchReportFactory already computes; the old Blazor site's "saved reports"
            // feature (GetMatchReportList/SaveMatchReport, an XML-only endpoint pair) was never
            // ported to this ASP.NET Core API in Phase 1+2 and has no Postgres-backed persistence -
            // that remains a known gap, not something this endpoint fixes.
            app.MapGet("/api/GetMatchReport/MaleId/{maleId}/FemaleId/{femaleId}", async (HttpContext context, string maleId, string femaleId) =>
            {
                try
                {
                    var male = Tools.GetPersonById(maleId);
                    var female = Tools.GetPersonById(femaleId);

                    var report = MatchReportFactory.GetNewMatchReport(male, female, "101");

                    await APITools.PassMessageJson(report.ToJson(), context);
                }
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson(e, context);
                }
            });
        }


        //PRIVATE


        /// <summary>
        /// Gets all people ordered by kuta total strength 0 is highest kuta score
        /// note : chart created to make score is discarded
        /// </summary>
        public static async Task<List<PersonKutaScore>> GetAllPersonByMatchStrength(Person inputPerson)
        {
            var resultList = new List<MatchReport>();

            //set input person in correct gender order
            var inputPersonIsMale = inputPerson.Gender == Gender.Male;

            //get everybody (skip life events, since not needed & faster)
            var everybody = APITools.GetAllPersonList(true);

            //this makes sure each person is cross checked against this person correctly
            foreach (var personMatch in everybody)
            {
                //skip own record
                if (personMatch.Equals(inputPerson)) { continue; }

                //add report to list
                MatchReport report;

                //sex orientation depends on input person only
                //in other words input person is always placed in correct sex calculator
                //note : done so that same sex can be checked without to much code
                //       & male can be checked from female position
                if (inputPersonIsMale)
                {
                    report = MatchReportFactory.GetNewMatchReport(inputPerson, personMatch, "101");
                }
                //input person is female
                else
                {
                    report = MatchReportFactory.GetNewMatchReport(personMatch, inputPerson, "101");
                }

                resultList.Add(report);
            }

            //SORT
            //order the list by strength, highest at 0 index
            var resultListOrdered = resultList.OrderByDescending(o => o.KutaScore).ToList();

            //only above 70 should be considered perfect
            var minimumScore = 70;

            //FILTER
            //needs to meets minimum score to make into list
            var finalList =
                from matchReport in resultListOrdered
                where matchReport.KutaScore >= minimumScore
                select matchReport;

            //package together all the needed data
            //get needed details, person name and score to them
            List<PersonKutaScore> personList2;
            personList2 = finalList.Select(matchReport =>
            {
                //if male put in female
                //if female put in male
                var matchPerson = inputPersonIsMale ? matchReport.Female : matchReport.Male;
                var id = matchPerson.Id;
                var name = matchPerson.Name;
                var gender = matchPerson.Gender;
                var age = matchPerson.GetAge();
                return new PersonKutaScore(id, name, gender, age, matchReport.KutaScore);
            }).ToList();

            return personList2;
        }

    }

}
