using System.Collections.Generic;
using static VedAstro.Library.HouseName;
using static VedAstro.Library.PlanetName;

namespace VedAstro.Library
{
    /// <summary>
    /// How confidently a BtrQuestion's rule mapping is grounded in a documented classical/BTR
    /// source vs. a generic karaka (significator) heuristic used when no specific event rule
    /// exists for the question. See "Relation to btr-literature" in docs/BirthTimeFinder.md.
    /// </summary>
    public enum BtrRuleTier
    {
        /// <summary>Maps to a house whose classical significations directly cover the life event asked about.</summary>
        Classical,
        /// <summary>No specific event rule exists for this question; mapped via a general planetary karaka association instead.</summary>
        Heuristic
    }

    public record BtrQuestion(int Id, string Text, string Category, BtrRuleTier Tier, IReadOnlyList<BtrRuleRef> Rules);

    /// <summary>
    /// The BTR questionnaire from docs/BirthTimeFinder.md's mockup (95 life-event questions),
    /// each mapped to one or more BtrRuleRef conditions evaluated by BtrCondition/BtrScoringEngine
    /// against a candidate birth chart. Classical-house significations used throughout:
    /// H1=self/body, H2=wealth/family, H3=siblings/courage, H4=mother/home/property,
    /// H5=children/intellect, H6=disease/enemies/debts, H7=spouse/marriage, H8=longevity/death,
    /// H9=father/fortune/dharma, H10=career, H11=gains/elder siblings, H12=losses/foreign/sleep.
    /// </summary>
    public static class BtrQuestionBank
    {
        private static BtrRuleRef HS(HouseName h) => new(BtrEvaluator.HouseLordStrong, h, null, true);
        private static BtrRuleRef HA(HouseName h) => new(BtrEvaluator.HouseLordAfflicted, h, null, true);
        private static BtrRuleRef PW(PlanetName p) => new(BtrEvaluator.PlanetWellPlaced, null, p, true);
        private static BtrRuleRef PA(PlanetName p) => new(BtrEvaluator.PlanetAfflicted, null, p, true);

        public static readonly IReadOnlyList<BtrQuestion> All = new List<BtrQuestion>
        {
            new(1, "Have you been involved in court proceedings regarding your marriage?", "Marriage", BtrRuleTier.Classical, new[] { HA(House7) }),
            new(2, "Have you ever had a son?", "Children", BtrRuleTier.Classical, new[] { HS(House5) }),
            new(3, "Do you follow a regular exercise routine?", "Physical", BtrRuleTier.Heuristic, new[] { PW(Mars) }),
            new(4, "Do you often find good opportunities by chance?", "Luck", BtrRuleTier.Classical, new[] { HS(House9) }),
            new(5, "Does your horoscope estimate your lifespan to be between 32 and 75 years?", "Longevity", BtrRuleTier.Heuristic, new[] { HS(House8) }),
            new(6, "Do you often feel mentally worried or unhappy?", "Psychological", BtrRuleTier.Classical, new[] { PA(Moon) }),
            new(7, "Do you enjoy reading or writing poetry?", "Personality", BtrRuleTier.Heuristic, new[] { PW(Mercury) }),
            new(8, "Have you ever had suicidal thoughts?", "Psychological", BtrRuleTier.Classical, new[] { HA(House8) }),
            new(9, "Were you adopted by a family that is not your birth family?", "Family", BtrRuleTier.Heuristic, new[] { HA(House4) }),
            new(10, "Do you feel content with your wife as your partner?", "Marriage", BtrRuleTier.Classical, new[] { HS(House7) }),
            new(11, "Have you ever betrayed a teacher's trust?", "Personality", BtrRuleTier.Heuristic, new[] { HA(House9) }),
            new(12, "Do you sleep comfortably no matter where you are?", "Physical", BtrRuleTier.Heuristic, new[] { HS(House12) }),
            new(13, "Did you feel happier after your 30th birthday?", "Luck", BtrRuleTier.Heuristic, new[] { HS(House11) }),
            new(14, "Has your brother provided you with financial support?", "Siblings", BtrRuleTier.Classical, new[] { HS(House11) }),
            new(15, "Do you have two or more diagnosed health conditions?", "Health", BtrRuleTier.Classical, new[] { HA(House6) }),
            new(16, "Do you often argue with your mother?", "Mother", BtrRuleTier.Classical, new[] { PA(Moon) }),
            new(17, "Has your married partner died?", "Marriage", BtrRuleTier.Classical, new[] { HA(House7) }),
            new(18, "Do you struggle to afford basic necessities?", "Finance", BtrRuleTier.Classical, new[] { HA(House2) }),
            new(19, "Do you engage in sexual behavior that others describe as animalistic?", "Personality", BtrRuleTier.Heuristic, new[] { PA(Mars) }),
            new(20, "Have you caused your mother's death?", "Mother", BtrRuleTier.Classical, new[] { PA(Moon) }),
            new(21, "Do you struggle to afford basic living expenses?", "Finance", BtrRuleTier.Classical, new[] { HA(House2) }),
            new(22, "Have you ever been injured in an accident?", "Health", BtrRuleTier.Classical, new[] { PA(Mars) }),
            new(23, "Do you feel confident sharing your thoughts?", "Personality", BtrRuleTier.Heuristic, new[] { PW(Sun) }),
            new(24, "Do you have two or fewer brothers?", "Siblings", BtrRuleTier.Heuristic, new[] { HA(House3) }),
            new(25, "Do you own a house that you consider attractive?", "Property", BtrRuleTier.Classical, new[] { HS(House4) }),
            new(26, "Does your relationship appear stable on the outside?", "Marriage", BtrRuleTier.Heuristic, new[] { HS(House7) }),
            new(27, "Does your wife treat you with kindness?", "Marriage", BtrRuleTier.Classical, new[] { PW(Venus) }),
            new(28, "Do  you feel at peace with the idea of death?", "Spiritual", BtrRuleTier.Heuristic, new[] { HS(House12) }),
            new(29, "Have you never been married?", "Marriage", BtrRuleTier.Classical, new[] { HA(House7) }),
            new(30, "Have you ever been charged with a crime?", "Legal", BtrRuleTier.Classical, new[] { HA(House6) }),
            new(31, "Have you ever paid a penalty that cost you a substantial amount of money?", "Finance", BtrRuleTier.Classical, new[] { HA(House2) }),
            new(32, "Do you often stutter or repeat sounds when you speak?", "Physical", BtrRuleTier.Classical, new[] { PA(Mercury) }),
            new(33, "Do you own substantial assets?", "Finance", BtrRuleTier.Classical, new[] { HS(House2) }),
            new(34, "Does your wife lie to people repeatedly?", "Marriage", BtrRuleTier.Heuristic, new[] { PA(Venus) }),
            new(35, "Do you travel to various religious centres?", "Spiritual", BtrRuleTier.Classical, new[] { HS(House9) }),
            new(36, "Have you been diagnosed with an eye disease by a medical professional?", "Health", BtrRuleTier.Classical, new[] { PA(Sun) }),
            new(37, "Is your marriage happy?", "Marriage", BtrRuleTier.Classical, new[] { HS(House7) }),
            new(38, "Do your real estate transactions usually go smoothly?", "Property", BtrRuleTier.Classical, new[] { HS(House4) }),
            new(39, "Have your relatives ever caused problems for you?", "Family", BtrRuleTier.Heuristic, new[] { HA(House6) }),
            new(40, "Do you own several real estate properties such as homes or land?", "Property", BtrRuleTier.Classical, new[] { HS(House4) }),
            new(41, "Do you follow a balanced diet?", "Physical", BtrRuleTier.Heuristic, new[] { HS(House6) }),
            new(42, "Do you earn a high income?", "Finance", BtrRuleTier.Classical, new[] { HS(House11) }),
            new(43, "Do you prefer furniture with artistic designs?", "Personality", BtrRuleTier.Heuristic, new[] { PW(Venus) }),
            new(44, "Have you ever lost valuable belongings?", "Finance", BtrRuleTier.Classical, new[] { HA(House12) }),
            new(45, "Do you believe you will become a celestial being after death?", "Spiritual", BtrRuleTier.Heuristic, new[] { PW(Jupiter) }),
            new(46, "Have you ever experienced the death of one of your children?", "Children", BtrRuleTier.Classical, new[] { HA(House5) }),
            new(47, "Have you faced unexpected expenses because of a child's needs?", "Children", BtrRuleTier.Classical, new[] { HA(House5) }),
            new(48, "Is it correct that your father was not present at the time of your birth?", "Father", BtrRuleTier.Classical, new[] { PA(Sun) }),
            new(49, "Do you feel satisfied with your physical health?", "Health", BtrRuleTier.Classical, new[] { HS(House1) }),
            new(50, "Do you have a mental health condition?", "Psychological", BtrRuleTier.Classical, new[] { PA(Moon) }),
            new(51, "Do you live on a low income?", "Finance", BtrRuleTier.Classical, new[] { HA(House11) }),
            new(52, "Have you ever been widowed?", "Marriage", BtrRuleTier.Classical, new[] { HA(House7) }),
            new(53, "Do you enjoy engaging in debates?", "Personality", BtrRuleTier.Heuristic, new[] { PW(Mercury) }),
            new(54, "Do you own more than one house?", "Property", BtrRuleTier.Classical, new[] { HS(House4) }),
            new(55, "Have you ever done something that made your father happy?", "Father", BtrRuleTier.Classical, new[] { PW(Sun) }),
            new(56, "Do you have a heavy-set physique?", "Physical", BtrRuleTier.Heuristic, new[] { PW(Jupiter) }),
            new(57, "Do you often feel unhappy in your marriage?", "Marriage", BtrRuleTier.Classical, new[] { HA(House7) }),
            new(58, "Do you have at least three brothers?", "Siblings", BtrRuleTier.Classical, new[] { HS(House3) }),
            new(59, "Do you often struggle to focus on tasks?", "Psychological", BtrRuleTier.Heuristic, new[] { PA(Mercury) }),
            new(60, "Do you find it hard to predict when your brothers will prosper financially?", "Siblings", BtrRuleTier.Heuristic, new[] { HA(House11) }),
            new(61, "Have you ever been arrested by law enforcement?", "Legal", BtrRuleTier.Classical, new[] { HA(House8) }),
            new(62, "Do you often find your thoughts consumed by worries?", "Psychological", BtrRuleTier.Classical, new[] { PA(Moon) }),
            new(63, "Do you struggle to afford your basic living expenses?", "Finance", BtrRuleTier.Classical, new[] { HA(House2) }),
            new(64, "Do you have difficulty seeing things clearly?", "Health", BtrRuleTier.Heuristic, new[] { PA(Sun) }),
            new(65, "Have your children ever given you money or other valuable gifts?", "Children", BtrRuleTier.Classical, new[] { HS(House5) }),
            new(66, "Do you spend your income on many different expenses leaving little savings?", "Finance", BtrRuleTier.Heuristic, new[] { HA(House11) }),
            new(67, "Do you feel that luck has been on your side?", "Luck", BtrRuleTier.Classical, new[] { HS(House9) }),
            new(68, "Have you ever been subjected to torture?", "Health", BtrRuleTier.Classical, new[] { HA(House8) }),
            new(69, "Do you consider yourself financially well-off?", "Finance", BtrRuleTier.Classical, new[] { HS(House2) }),
            new(70, "Do you regularly donate money or goods to help others?", "Spiritual", BtrRuleTier.Heuristic, new[] { PW(Jupiter) }),
            new(71, "Do you suffer from any serious health problems?", "Health", BtrRuleTier.Classical, new[] { HA(House6) }),
            new(72, "Do you have health issues?", "Health", BtrRuleTier.Classical, new[] { HA(House6) }),
            new(73, "Do you have difficulty seeing objects in low light?", "Health", BtrRuleTier.Heuristic, new[] { PA(Sun) }),
            new(74, "Do you follow your father's instructions?", "Father", BtrRuleTier.Heuristic, new[] { PW(Sun) }),
            new(75, "Do you hold a valid passport?", "Travel", BtrRuleTier.Heuristic, new[] { HS(House12) }),
            new(76, "Do you lack comfortable bedding due to financial constraints?", "Finance", BtrRuleTier.Heuristic, new[] { HA(House12) }),
            new(77, "Have you experienced more than one episode of high fever during your early life?", "Health", BtrRuleTier.Heuristic, new[] { HA(House6) }),
            new(78, "Have you ever had a severe fever during your childhood?", "Health", BtrRuleTier.Heuristic, new[] { HA(House6) }),
            new(79, "Do you feel completely free from all desires and suffering?", "Spiritual", BtrRuleTier.Heuristic, new[] { HS(House12) }),
            new(80, "Has your husband or wife died?", "Marriage", BtrRuleTier.Classical, new[] { HA(House7) }),
            new(81, "Have you ever lost money because of your spouse?", "Marriage", BtrRuleTier.Heuristic, new[] { HA(House7) }),
            new(82, "Do you read materials on metaphysical lore?", "Spiritual", BtrRuleTier.Heuristic, new[] { PW(Jupiter) }),
            new(83, "Do you earn your income through brokerage?", "Finance", BtrRuleTier.Heuristic, new[] { PW(Mercury) }),
            new(84, "Have you been diagnosed with a life-limiting medical condition?", "Health", BtrRuleTier.Classical, new[] { HA(House8) }),
            new(85, "Does your country's law allow you to have more than one wife?", "Marriage", BtrRuleTier.Heuristic, new[] { HS(House7) }),
            new(86, "Do events typically turn out in your favor?", "Luck", BtrRuleTier.Classical, new[] { HS(House9) }),
            new(87, "Does your mother express love or affection toward you?", "Mother", BtrRuleTier.Classical, new[] { PW(Moon) }),
            new(88, "Have you ever married more than one woman?", "Marriage", BtrRuleTier.Heuristic, new[] { PW(Venus) }),
            new(89, "Do you earn money by selling precious stones?", "Finance", BtrRuleTier.Heuristic, new[] { PW(Venus) }),
            new(90, "Has your father passed away?", "Father", BtrRuleTier.Classical, new[] { PA(Sun) }),
            new(91, "Do you spend time in forests or mountainous regions?", "Personality", BtrRuleTier.Heuristic, new[] { HS(House12) }),
            new(92, "Do you owe money to any creditor?", "Finance", BtrRuleTier.Classical, new[] { HA(House6) }),
            new(93, "Do you often break rules?", "Personality", BtrRuleTier.Heuristic, new[] { PA(Saturn) }),
            new(94, "Do you have people who openly oppose or dislike you?", "Legal", BtrRuleTier.Classical, new[] { HA(House6) }),
            new(95, "Do you have difficulty reading small text?", "Health", BtrRuleTier.Heuristic, new[] { PA(Sun) }),
        };
    }
}
