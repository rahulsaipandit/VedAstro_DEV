using Newtonsoft.Json.Linq;
using VedAstro.Library;

namespace MatchMLPipeline;

/// <summary>
/// Phase 2 of the ML validation pipeline described in docs/MLTesting.md: checks whether
/// MatchReportFactory's Kuta/Guna compatibility scoring actually tracks real marriage outcomes.
///
/// Unlike Phase 1 (DashaValidationPipeline, a single-person timing check), Kuta scoring is
/// inherently a two-person comparison, so the candidate set here is real *couples* - both
/// people must be linkable back to a Rodden AA birth-data row in PersonList-15k, which
/// HuggingFace/MarriageInfoDataset.csv only supports for ~1,450 of its ~17,900 marriage
/// entries (the rest only record the spouse's name as free text, with no PersonId link).
/// For that linked subset with a binary-clear outcome ("Happiness" or "Dissolution" - other
/// labels like "Struggle"/"Tragedy"/"Unknown" are dropped as ambiguous), this computes each
/// couple's total Kuta score via the existing MatchReportFactory.GetNewMatchReport and checks
/// whether Happiness-outcome couples score higher, on average and by rank correlation, than
/// Dissolution-outcome couples.
///
/// Read-only - writes nothing back to the DB.
/// </summary>
public static class KutaValidationPipeline
{
    public enum MarriageOutcome { Happiness, Dissolution }

    public record CoupleCandidate(PersonListEntity PersonA, PersonListEntity PersonB, MarriageOutcome Outcome);

    public record ValidationResult(string PersonAName, string PersonBName, MarriageOutcome Outcome, double KutaScore);

    /// <summary>
    /// Scans all marriage records for entries where the spouse has been linked back to their
    /// own PersonList row (marriage["PersonId"] populated), both people are Rodden AA rated,
    /// and the outcome is unambiguously "Happiness" or "Dissolution". Each couple is included
    /// once even though its marriage record may appear from both partners' sides.
    /// </summary>
    public static List<CoupleCandidate> BuildCoupleCandidateSet()
    {
        var people = DatasetFactory.personRepo.GetAllAsync().GetAwaiter().GetResult()
            // Notes is Python-dict-style, not JSON - e.g. "{'rodden': 'AA'}" (single quotes)
            .Where(p => p.Notes?.Contains("'rodden': 'AA'") ?? false)
            .ToDictionary(p => p.RowKey, p => p);

        var marriageRecords = DatasetFactory.marriageInfoDatasetRepo.GetAllAsync().GetAwaiter().GetResult();

        var seenCouples = new HashSet<string>();
        var candidates = new List<CoupleCandidate>();

        foreach (var record in marriageRecords)
        {
            if (!people.TryGetValue(record.PartitionKey, out var personA)) { continue; }

            // GetMarriagesWhereRoddenAA already filters to entries with a populated PersonId
            // (i.e. the spouse was successfully linked to a PersonList row) despite its name -
            // "Rodden AA" here describes the linkage step, not the rating itself.
            foreach (var marriage in record.GetMarriagesWhereRoddenAA())
            {
                var spouseId = marriage["PersonId"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(spouseId)) { continue; }
                if (!people.TryGetValue(spouseId, out var personB)) { continue; }

                if (!TryGetBinaryOutcome(marriage, out var outcome)) { continue; }

                // dedupe: same couple can appear from both partners' marriage records
                var coupleKey = string.CompareOrdinal(personA.RowKey, personB.RowKey) < 0
                    ? $"{personA.RowKey}|{personB.RowKey}"
                    : $"{personB.RowKey}|{personA.RowKey}";
                if (!seenCouples.Add(coupleKey)) { continue; }

                candidates.Add(new CoupleCandidate(personA, personB, outcome));
            }
        }

        return candidates;
    }

    private static bool TryGetBinaryOutcome(JToken marriage, out MarriageOutcome outcome)
    {
        outcome = default;
        var raw = marriage["outcome"]?.Value<string>();

        if (string.Equals(raw, "Happiness", StringComparison.OrdinalIgnoreCase))
        {
            outcome = MarriageOutcome.Happiness;
            return true;
        }
        if (string.Equals(raw, "Dissolution", StringComparison.OrdinalIgnoreCase))
        {
            outcome = MarriageOutcome.Dissolution;
            return true;
        }

        return false; // "Struggle"/"Tragedy"/"Unknown"/"None"/etc - ambiguous, not usable here
    }

    public static List<ValidationResult> RunValidation(List<CoupleCandidate> candidates)
    {
        // Sequential: MatchReportFactory's calculators ultimately call into the same
        // ephemeris/event-calculation machinery DashaValidationPipeline does (see that file's
        // RunValidation for why an outer Parallel.ForEach here would oversubscribe the thread
        // pool rather than help).
        var results = new List<ValidationResult>();
        var processed = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var candidate in candidates)
        {
            var (malePerson, femalePerson) = ToMaleFemalePersons(candidate.PersonA, candidate.PersonB);
            if (malePerson == null || femalePerson == null) { continue; } // both same gender in data - skip, can't run Kuta

            MatchReport report;
            try
            {
                report = MatchReportFactory.GetNewMatchReport(malePerson!.Value, femalePerson!.Value, "kuta-validation-pipeline");
            }
            catch (Exception e)
            {
                // best-effort, same policy as DatasetFactory's per-row calculators - but log so a
                // lower "Scored couples" count is traceable back to which couple and why
                Console.WriteLine($"Kuta validation FAILED for {candidate.PersonA.Name} + {candidate.PersonB.Name}: {e.Message}");
                continue;
            }

            results.Add(new ValidationResult(candidate.PersonA.Name, candidate.PersonB.Name, candidate.Outcome, report.KutaScore));

            processed++;
            if (processed % 100 == 0)
            {
                Console.WriteLine($"Kuta validation: processed {processed}/{candidates.Count} ({stopwatch.Elapsed.TotalSeconds / processed:F2}s/candidate)");
            }
        }

        return results;
    }

    private static (Person? male, Person? female) ToMaleFemalePersons(PersonListEntity a, PersonListEntity b)
    {
        if (a.IsMale() && !b.IsMale())
        {
            return (new Person(a.Name, a.ToBirthTime(), Gender.Male), new Person(b.Name, b.ToBirthTime(), Gender.Female));
        }
        if (b.IsMale() && !a.IsMale())
        {
            return (new Person(b.Name, b.ToBirthTime(), Gender.Male), new Person(a.Name, a.ToBirthTime(), Gender.Female));
        }

        return (null, null);
    }

    /// <summary>
    /// Spearman rank correlation between Kuta score and outcome (Happiness=1, Dissolution=0).
    /// A positive value means higher Kuta scores tend to go with Happiness outcomes, as the
    /// matching logic claims they should; near zero or negative means the scoring isn't
    /// tracking real outcomes for this sample.
    /// </summary>
    public static double SpearmanCorrelation(List<ValidationResult> results)
    {
        if (results.Count < 2) { return 0; }

        var scoreRanks = Rank(results.Select(r => r.KutaScore).ToList());
        var outcomeRanks = Rank(results.Select(r => r.Outcome == MarriageOutcome.Happiness ? 1.0 : 0.0).ToList());

        var n = results.Count;
        var meanScoreRank = scoreRanks.Average();
        var meanOutcomeRank = outcomeRanks.Average();

        double covariance = 0, scoreVariance = 0, outcomeVariance = 0;
        for (var i = 0; i < n; i++)
        {
            var scoreDelta = scoreRanks[i] - meanScoreRank;
            var outcomeDelta = outcomeRanks[i] - meanOutcomeRank;
            covariance += scoreDelta * outcomeDelta;
            scoreVariance += scoreDelta * scoreDelta;
            outcomeVariance += outcomeDelta * outcomeDelta;
        }

        var denominator = Math.Sqrt(scoreVariance * outcomeVariance);
        return denominator == 0 ? 0 : covariance / denominator;
    }

    /// <summary>Average-rank transform (ties get the mean of the ranks they'd otherwise span).</summary>
    private static List<double> Rank(List<double> values)
    {
        var indexed = values.Select((v, i) => (v, i)).OrderBy(x => x.v).ToList();
        var ranks = new double[values.Count];

        var position = 0;
        while (position < indexed.Count)
        {
            var tieEnd = position;
            while (tieEnd + 1 < indexed.Count && indexed[tieEnd + 1].v == indexed[position].v) { tieEnd++; }

            var averageRank = (position + tieEnd) / 2.0 + 1; // 1-based ranks
            for (var i = position; i <= tieEnd; i++) { ranks[indexed[i].i] = averageRank; }

            position = tieEnd + 1;
        }

        return ranks.ToList();
    }

    /// <summary>
    /// Entry point for this validation run - builds the couple candidate set, scores each
    /// couple, and reports mean Kuta score per outcome group plus the rank correlation.
    /// </summary>
    public static void RunAndPrintReport()
    {
        var candidates = BuildCoupleCandidateSet();
        Console.WriteLine($"Couple candidate set: {candidates.Count} linked couples with Rodden AA data and a Happiness/Dissolution outcome");

        var results = RunValidation(candidates);
        Console.WriteLine($"Scored couples: {results.Count} (some candidates skipped - same-gender pair in data, or a Kuta calculator threw)");

        var happy = results.Where(r => r.Outcome == MarriageOutcome.Happiness).ToList();
        var dissolved = results.Where(r => r.Outcome == MarriageOutcome.Dissolution).ToList();

        var meanHappy = happy.Count == 0 ? 0 : happy.Average(r => r.KutaScore);
        var meanDissolved = dissolved.Count == 0 ? 0 : dissolved.Average(r => r.KutaScore);
        var correlation = SpearmanCorrelation(results);

        Console.WriteLine($"Mean Kuta score - Happiness outcomes ({happy.Count}): {meanHappy:F2}");
        Console.WriteLine($"Mean Kuta score - Dissolution outcomes ({dissolved.Count}): {meanDissolved:F2}");
        Console.WriteLine($"Spearman correlation (Kuta score vs. Happiness=1/Dissolution=0): {correlation:F3}");
        Console.WriteLine();

        foreach (var result in results.OrderBy(r => r.KutaScore))
        {
            Console.WriteLine($"[{result.Outcome,-11}] {result.PersonAName,-25} + {result.PersonBName,-25} kutaScore={result.KutaScore:F1}");
        }
    }
}
