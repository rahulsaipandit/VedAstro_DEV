using System;
using System.Collections.Generic;
using System.Linq;

namespace VedAstro.Library
{
    public enum BtrConfidenceBucket { Low, Medium, High }

    /// <summary>One answered questionnaire item, ready to score against a candidate chart.</summary>
    public record BtrAnswerInput(int QuestionId, BtrAnswer Answer);

    /// <summary>A single candidate birth time with its raw and normalized BTR score.</summary>
    public record BtrCandidateScore(Time Time, double RawScore, double ConfidencePercent, BtrConfidenceBucket Bucket);

    /// <summary>
    /// Scores each candidate birth time produced by BirthTimeCandidateSweep against a set of
    /// answered BtrQuestionBank questions, ranking candidates by how well their chart agrees with
    /// the practitioner's answers. See docs/BirthTimeFinder.md.
    /// </summary>
    public static class BtrScoringEngine
    {
        private static readonly Dictionary<int, BtrQuestion> QuestionsById =
            BtrQuestionBank.All.ToDictionary(q => q.Id);

        /// <summary>
        /// Raw (unnormalized) score for one candidate: for each answered (non-Skip) question, each
        /// of its rules is evaluated against the candidate's chart; agreement between the rule's
        /// expected result and the answer's direction adds the answer's weight, disagreement
        /// subtracts it. Skip answers, or an unknown question id, contribute nothing.
        /// </summary>
        public static double ScoreCandidate(Person candidate, IReadOnlyList<BtrAnswerInput> answers)
        {
            var candidateTime = candidate.BirthTime;
            double score = 0;

            foreach (var answerInput in answers)
            {
                if (answerInput.Answer == BtrAnswer.Skip) { continue; }
                if (!QuestionsById.TryGetValue(answerInput.QuestionId, out var question)) { continue; }

                var weight = answerInput.Answer.Weight();
                var answerIsAffirmative = answerInput.Answer.IsAffirmative();

                foreach (var rule in question.Rules)
                {
                    var ruleIsTrueForCandidate = BtrCondition.Evaluate(rule, candidateTime);

                    //agreement: the rule firing matches what the answer implies (Yes -> expect
                    //ExpectedWhenYes, No -> expect the opposite of ExpectedWhenYes)
                    var expectedForThisAnswer = answerIsAffirmative ? rule.ExpectedWhenYes : !rule.ExpectedWhenYes;
                    var agrees = ruleIsTrueForCandidate == expectedForThisAnswer;

                    score += agrees ? weight : -weight;
                }
            }

            return score;
        }

        /// <summary>
        /// Scores every candidate, then normalizes raw scores into a 0-100 confidence percent
        /// (min-max normalized across the candidate set) and a High/Medium/Low bucket by tercile,
        /// sorted with the best-matching candidate first.
        /// </summary>
        public static IReadOnlyList<BtrCandidateScore> RankCandidates(
            IReadOnlyList<Person> candidates, IReadOnlyList<BtrAnswerInput> answers)
        {
            if (candidates.Count == 0) { return Array.Empty<BtrCandidateScore>(); }

            var raw = candidates
                .Select(c => (Time: c.BirthTime, RawScore: ScoreCandidate(c, answers)))
                .ToList();

            var min = raw.Min(r => r.RawScore);
            var max = raw.Max(r => r.RawScore);
            var range = max - min;

            var ranked = raw
                .Select(r =>
                {
                    //all candidates scored identically (e.g. no answers given) -> flat 50% for everyone
                    var confidencePercent = range <= 0 ? 50.0 : (r.RawScore - min) / range * 100.0;
                    var bucket = confidencePercent >= 66.6 ? BtrConfidenceBucket.High
                               : confidencePercent >= 33.3 ? BtrConfidenceBucket.Medium
                               : BtrConfidenceBucket.Low;

                    return new BtrCandidateScore(r.Time, r.RawScore, confidencePercent, bucket);
                })
                .OrderByDescending(c => c.ConfidencePercent)
                .ToList();

            return ranked;
        }
    }
}
