using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VedAstro.Library;

namespace VedAstro.Library.Tests
{
    [TestClass]
    public class BtrScoringEngineTests
    {
        private static readonly Person TestPerson =
            new("BTR Test Person", CalculateTests.StandardHoroscope, Gender.Female);

        /// <summary>
        /// Cheap blanket check over the whole 95-question data table: every BtrEvaluator/House/
        /// Planet combination it references must actually be evaluable (i.e. BtrCondition.Evaluate
        /// doesn't throw), so a bad entry (missing House on a HouseLord* rule, missing Planet on a
        /// Planet* rule) fails fast here instead of surfacing as a runtime 500 from the API.
        /// </summary>
        [TestMethod]
        public void AllQuestionBankRules_EvaluateWithoutThrowing()
        {
            foreach (var question in BtrQuestionBank.All)
            {
                foreach (var rule in question.Rules)
                {
                    try
                    {
                        BtrCondition.Evaluate(rule, CalculateTests.StandardHoroscope);
                    }
                    catch (System.Exception e)
                    {
                        Assert.Fail($"Question {question.Id} (\"{question.Text}\") rule {rule.Evaluator} threw: {e.Message}");
                    }
                }
            }
        }

        [TestMethod]
        public void QuestionBank_HasNoDuplicateIds()
        {
            var ids = BtrQuestionBank.All.Select(q => q.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Duplicate question ids found in BtrQuestionBank.All");
        }

        [TestMethod]
        public void ScoreCandidate_SkipAnswer_ContributesNothing()
        {
            var firstQuestionId = BtrQuestionBank.All.First().Id;

            var skipScore = BtrScoringEngine.ScoreCandidate(TestPerson,
                new[] { new BtrAnswerInput(firstQuestionId, BtrAnswer.Skip) });

            Assert.AreEqual(0, skipScore);
        }

        [TestMethod]
        public void ScoreCandidate_UnknownQuestionId_ContributesNothing()
        {
            var score = BtrScoringEngine.ScoreCandidate(TestPerson,
                new[] { new BtrAnswerInput(-1, BtrAnswer.StronglyYes) });

            Assert.AreEqual(0, score);
        }

        [TestMethod]
        public void ScoreCandidate_StronglyAnswer_WeighsDoubleOfPlainAnswer()
        {
            var question = BtrQuestionBank.All.First();

            var plainScore = BtrScoringEngine.ScoreCandidate(TestPerson,
                new[] { new BtrAnswerInput(question.Id, BtrAnswer.Yes) });
            var strongScore = BtrScoringEngine.ScoreCandidate(TestPerson,
                new[] { new BtrAnswerInput(question.Id, BtrAnswer.StronglyYes) });

            //same direction of agreement/disagreement, but magnitude should double
            Assert.AreEqual(plainScore * 2, strongScore);
        }

        [TestMethod]
        public void ScoreCandidate_OppositeAnswers_ProduceOppositeSignScores()
        {
            var question = BtrQuestionBank.All.First();

            var yesScore = BtrScoringEngine.ScoreCandidate(TestPerson,
                new[] { new BtrAnswerInput(question.Id, BtrAnswer.Yes) });
            var noScore = BtrScoringEngine.ScoreCandidate(TestPerson,
                new[] { new BtrAnswerInput(question.Id, BtrAnswer.No) });

            Assert.AreEqual(-yesScore, noScore);
        }

        [TestMethod]
        public void RankCandidates_SortsDescendingByConfidence()
        {
            var candidates = new[]
            {
                new Person("A", new Time("06:00 16/10/1918 +05:30", GeoLocation.Bangalore), Gender.Female),
                new Person("B", new Time("12:00 16/10/1918 +05:30", GeoLocation.Bangalore), Gender.Female),
                new Person("C", new Time("18:00 16/10/1918 +05:30", GeoLocation.Bangalore), Gender.Female),
            };

            var answers = BtrQuestionBank.All.Take(10)
                .Select(q => new BtrAnswerInput(q.Id, BtrAnswer.StronglyYes))
                .ToList();

            var ranked = BtrScoringEngine.RankCandidates(candidates, answers);

            Assert.AreEqual(candidates.Length, ranked.Count);
            for (var i = 1; i < ranked.Count; i++)
            {
                Assert.IsTrue(ranked[i - 1].ConfidencePercent >= ranked[i].ConfidencePercent);
            }
        }

        [TestMethod]
        public void RankCandidates_NoAnswers_AllCandidatesTieAtFiftyPercent()
        {
            var candidates = new[]
            {
                new Person("A", new Time("06:00 16/10/1918 +05:30", GeoLocation.Bangalore), Gender.Female),
                new Person("B", new Time("18:00 16/10/1918 +05:30", GeoLocation.Bangalore), Gender.Female),
            };

            var ranked = BtrScoringEngine.RankCandidates(candidates, System.Array.Empty<BtrAnswerInput>());

            Assert.IsTrue(ranked.All(r => r.ConfidencePercent == 50.0));
        }
    }
}
