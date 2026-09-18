using System.Collections.Generic;

namespace VedAstro.Library
{
    /// <summary>
    /// The kind of astrological condition a BtrRuleRef checks. Kept to a small, generic set
    /// (house-lord strength/affliction, planet strength/affliction) built on top of existing
    /// Calculate.* primitives, rather than one bespoke method per questionnaire item - the same
    /// evaluator is reused across many questions by varying House/Planet.
    /// </summary>
    public enum BtrEvaluator
    {
        /// <summary>Lord of House is well placed (Kendra/Trikona from Lagna, not malefic-afflicted).</summary>
        HouseLordStrong,
        /// <summary>Lord of House is in a Dusthana (6/8/12) from Lagna, or malefic-afflicted.</summary>
        HouseLordAfflicted,
        /// <summary>Planet is well placed (Kendra/Trikona from Lagna or exalted, not malefic-afflicted).</summary>
        PlanetWellPlaced,
        /// <summary>Planet is in a Dusthana (6/8/12) from Lagna, or malefic-afflicted.</summary>
        PlanetAfflicted,
    }

    /// <summary>
    /// One rule reference inside a BtrQuestion: which evaluator to run, with which House/Planet
    /// parameter, and whether it's expected to be true (Occuring) when the practitioner answers
    /// "Yes" to the question.
    /// </summary>
    public record BtrRuleRef(BtrEvaluator Evaluator, HouseName? House, PlanetName? Planet, bool ExpectedWhenYes);

    /// <summary>
    /// Evaluates BtrRuleRef conditions against a candidate chart, reusing existing house-lord /
    /// planet-strength primitives from Core.cs &amp; CoreRelationships.cs instead of new astronomical
    /// calculation. See docs/BirthTimeFinder.md for the design rationale.
    ///
    /// NOTE: Rahu and Ketu are always exactly opposite each other, and
    /// Calculate.IsPlanetAspectedByMaleficPlanets treats that 180° relationship as a permanent
    /// mutual aspect - so PlanetWellPlaced(Rahu/Ketu) is always false and PlanetAfflicted(Rahu/Ketu)
    /// is always true, for every chart. Don't reference Rahu/Ketu from BtrQuestionBank via these
    /// two evaluators (BtrQuestionBank uses house-based rules instead for anything Ketu/Rahu-flavored).
    /// </summary>
    public static class BtrCondition
    {
        private static readonly HashSet<HouseName> Dusthana = new()
        {
            HouseName.House6, HouseName.House8, HouseName.House12
        };

        /// <summary>Runs the given rule's evaluator against the candidate's birth time.</summary>
        public static bool Evaluate(BtrRuleRef rule, Time candidateBirthTime)
        {
            return rule.Evaluator switch
            {
                BtrEvaluator.HouseLordStrong => IsHouseLordStrong(rule.House!.Value, candidateBirthTime),
                BtrEvaluator.HouseLordAfflicted => IsHouseLordAfflicted(rule.House!.Value, candidateBirthTime),
                BtrEvaluator.PlanetWellPlaced => IsPlanetWellPlaced(rule.Planet!, candidateBirthTime),
                BtrEvaluator.PlanetAfflicted => IsPlanetAfflicted(rule.Planet!, candidateBirthTime),
                _ => false
            };
        }

        public static bool IsHouseLordStrong(HouseName house, Time time)
        {
            var lord = Calculate.LordOfHouse(house, time);
            return IsPlanetWellPlaced(lord, time);
        }

        public static bool IsHouseLordAfflicted(HouseName house, Time time)
        {
            var lord = Calculate.LordOfHouse(house, time);
            return IsPlanetAfflicted(lord, time);
        }

        public static bool IsPlanetWellPlaced(PlanetName planet, Time time)
        {
            var placementIsGood = Calculate.IsPlanetInKendra(planet, time)
                                   || Calculate.IsPlanetInTrikona(planet, time)
                                   || Calculate.IsPlanetExaltedSign(planet, time);
            var notAfflicted = !Calculate.IsPlanetConjunctWithMaleficPlanets(planet, time)
                                && !Calculate.IsPlanetAspectedByMaleficPlanets(planet, time);

            return placementIsGood && notAfflicted;
        }

        public static bool IsPlanetAfflicted(PlanetName planet, Time time)
        {
            var occupiedHouse = Calculate.HousePlanetOccupiesBasedOnSign(planet, time);
            var badPlacement = Dusthana.Contains(occupiedHouse);
            var afflicted = Calculate.IsPlanetConjunctWithMaleficPlanets(planet, time)
                             || Calculate.IsPlanetAspectedByMaleficPlanets(planet, time);

            return badPlacement || afflicted;
        }
    }
}
