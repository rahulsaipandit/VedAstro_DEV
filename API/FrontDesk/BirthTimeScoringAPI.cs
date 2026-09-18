using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using VedAstro.Library;

namespace API
{
    /// <summary>
    /// Questionnaire-driven BTR scoring: ranks the same candidate birth-time sweep
    /// BirthTimeFinderAPI produces, but by how well each candidate's chart agrees with a
    /// practitioner's yes/no answers to the BtrQuestionBank life-event questionnaire, instead of
    /// requiring a human to visually compare a stack of SVG charts. See docs/BirthTimeFinder.md.
    /// </summary>
    public static class BirthTimeScoringAPI
    {
        public static void MapBirthTimeScoringEndpoints(this WebApplication app)
        {
            // GET /api/FindBirthTime/Questions
            app.MapGet("/api/FindBirthTime/Questions", async (HttpContext context) =>
            {
                try
                {
                    var questionsJson = new JArray();
                    foreach (var question in BtrQuestionBank.All)
                    {
                        questionsJson.Add(new JObject
                        {
                            ["Id"] = question.Id,
                            ["Text"] = question.Text,
                            ["Category"] = question.Category,
                            ["Tier"] = question.Tier.ToString()
                        });
                    }

                    await APITools.PassMessageJson(questionsJson, context);
                }
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson(e, context);
                }
            });

            // POST /api/FindBirthTime/Score/PersonId/{personId}
            // Body: { answers: [{questionId, answer}], precisionInHours?, startDate?, endDate?, startHour?, endHour?, ayanamsaName? }
            app.MapPost("/api/FindBirthTime/Score/PersonId/{personId}", async (HttpContext context, string personId) =>
            {
                try
                {
                    var requestJson = await APITools.ExtractDataFromRequestJson(context);

                    var ayanamsaName = requestJson["ayanamsaName"]?.Value<string>() ?? "Raman";
                    //Calculate.Ayanamsa is process-wide global state - set explicitly every request,
                    //same reasoning as BirthTimeFinderAPI
                    Calculate.Ayanamsa = (int)Tools.EnumFromUrl($"/Ayanamsa/{ayanamsaName}");

                    var precisionInHours = requestJson["precisionInHours"]?.Value<double>() ?? 1;
                    var startDate = requestJson["startDate"]?.Value<string>();
                    var endDate = requestJson["endDate"]?.Value<string>();
                    var startHour = requestJson["startHour"]?.Value<string>();
                    var endHour = requestJson["endHour"]?.Value<string>();

                    //malformed individual answers (missing questionId, unrecognized answer string -
                    //e.g. a stale client) are skipped rather than failing the whole batch, same
                    //effective effect as the practitioner leaving that one question unanswered
                    var answers = new List<BtrAnswerInput>();
                    foreach (var answerJson in requestJson["answers"] ?? new JArray())
                    {
                        if (answerJson["questionId"]?.Value<int?>() is not { } questionId) { continue; }
                        if (!Enum.TryParse<BtrAnswer>(answerJson["answer"]?.Value<string>(), ignoreCase: true, out var answer)) { continue; }

                        answers.Add(new BtrAnswerInput(questionId, answer));
                    }

                    var sweep = BirthTimeCandidateSweep.Build(personId, startDate, endDate, startHour, endHour, precisionInHours);
                    var candidates = BirthTimeCandidateSweep.CandidatePersons(sweep).ToList();
                    var ranked = BtrScoringEngine.RankCandidates(candidates, answers);

                    var resultJson = new JArray();
                    foreach (var candidateScore in ranked)
                    {
                        resultJson.Add(new JObject
                        {
                            ["Time"] = candidateScore.Time.ToString(),
                            ["RawScore"] = candidateScore.RawScore,
                            ["ConfidencePercent"] = Math.Round(candidateScore.ConfidencePercent, 1),
                            ["Bucket"] = candidateScore.Bucket.ToString()
                        });
                    }

                    await APITools.PassMessageJson(resultJson, context);
                }
                catch (Exception e)
                {
                    APILogger.Error(e, context.Request);
                    await APITools.FailMessageJson(e, context);
                }
            });
        }
    }
}
