namespace VedAstro.Library
{
    /// <summary>
    /// A practitioner's answer to one BTR questionnaire item (see BtrQuestionBank), on the
    /// 5-point scale from docs/BirthTimeFinder.md's questionnaire mockup.
    /// </summary>
    public enum BtrAnswer
    {
        StronglyNo,
        No,
        Skip,
        Yes,
        StronglyYes
    }

    public static class BtrAnswerExtensions
    {
        /// <summary>
        /// How strongly this answer should count toward a candidate's score.
        /// Skip contributes nothing; "Strongly" answers count double a plain Yes/No.
        /// </summary>
        public static int Weight(this BtrAnswer answer) => answer switch
        {
            BtrAnswer.StronglyNo => 2,
            BtrAnswer.No => 1,
            BtrAnswer.Skip => 0,
            BtrAnswer.Yes => 1,
            BtrAnswer.StronglyYes => 2,
            _ => 0
        };

        /// <summary>
        /// True for Yes/StronglyYes, false for No/StronglyNo. Meaningless (and unused) for Skip.
        /// </summary>
        public static bool IsAffirmative(this BtrAnswer answer) =>
            answer == BtrAnswer.Yes || answer == BtrAnswer.StronglyYes;
    }
}
