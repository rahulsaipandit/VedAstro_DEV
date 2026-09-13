using System.Globalization;
using Newtonsoft.Json.Linq;
using VedAstro.Library;

namespace MatchMLPipeline;

/// <summary>
/// Phase 1 of the ML validation pipeline described in docs/MLTesting.md: for real, documented
/// people (HuggingFace/PersonList-15k.csv, Rodden AA rated) with a known real marriage date
/// (HuggingFace/MarriageInfoDataset.csv, high data-credibility, fully-dated), checks whether
/// Vimshottari Dasha analysis of their birth chart flags a "marriage-favorable" period that
/// actually contains that date.
///
/// This is a read-only accuracy check against existing data/logic - it writes nothing back to
/// the DB. Follows the same repo-wiring pattern as DatasetFactory (reuses its already-constructed
/// personRepo / marriageInfoDatasetRepo rather than opening a second DB connection).
/// </summary>
public static class DashaValidationPipeline
{
    public record MarriageCandidate(PersonListEntity Person, DateTimeOffset MarriageDate);

    public record ValidationResult(
        string PersonName,
        DateTimeOffset ActualMarriageDate,
        DateTimeOffset? WindowStart,
        DateTimeOffset? WindowEnd,
        bool Hit);

    /// <summary>
    /// Scans all famous people who have a marriage-info record, and keeps only the ones whose
    /// birth data is Rodden AA rated and whose marriage record is high-credibility with a
    /// fully-dated (day/month/year) marriage date - see docs/MLTesting.md "Build the candidate
    /// set" for why the other rows (LLM-low-confidence, year-only, "None"/"N/A") are unusable.
    /// </summary>
    public static List<MarriageCandidate> BuildCandidateSet()
    {
        var people = DatasetFactory.personRepo.GetAllAsync().GetAwaiter().GetResult()
            // Notes is Python-dict-style, not JSON - e.g. "{'rodden': 'AA'}" (single quotes)
            .Where(p => p.Notes?.Contains("'rodden': 'AA'") ?? false)
            .ToDictionary(p => p.RowKey, p => p);

        var marriageRecords = DatasetFactory.marriageInfoDatasetRepo.GetAllAsync().GetAwaiter().GetResult();

        var candidates = new List<MarriageCandidate>();
        foreach (var record in marriageRecords)
        {
            if (!people.TryGetValue(record.PartitionKey, out var person)) { continue; }

            foreach (var marriage in record.GetMarriages())
            {
                if (!IsHighCredibility(marriage)) { continue; }
                if (!TryParseFullMarriageDate(marriage["marriageDate"]?.Value<string>(), out var marriageDate)) { continue; }

                candidates.Add(new MarriageCandidate(person, marriageDate));
            }
        }

        return candidates;
    }

    private static bool IsHighCredibility(JToken marriage) =>
        string.Equals(marriage["dataCredibility"]?.Value<string>(), "high", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Marriage dates in the dataset show up as "dd/MM/yyyy", year-only ("1954"), or placeholder
    /// text ("None", "N/A", "Not Applicable", empty). Only the fully-dated form gives a real
    /// point in time to check a predicted window against, so everything else is rejected here.
    /// </summary>
    private static bool TryParseFullMarriageDate(string? rawDate, out DateTimeOffset parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(rawDate)) { return false; }

        return DateTimeOffset.TryParseExact(
            rawDate.Trim(),
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsed);
    }

    /// <summary>
    /// Finds the first antardasha (PD2, "Bhukti") period after age 18 ruled by a planet
    /// classically tied to marriage - Venus, the 7th-house lord, or (per BPHS, for women)
    /// Jupiter - and returns its time span as the predicted marriage window.
    /// Returns null if no such period is found within the scanned range.
    ///
    /// Deliberately does NOT use VimshottariDasa.DasaPeriodsOld (a brute-force life-span scan
    /// via EventManager.CalculateEvents at fine time resolution across all 4 Dasa levels) - in
    /// practice that took 30+ minutes for a single person, making a ~10k-candidate validation
    /// run infeasible. Instead this samples VimshottariDasa.CurrentDasa8Levels (the same
    /// analytic point-in-time lookup the live API/Muhurtha logic already uses elsewhere) at a
    /// monthly step, which is an O(1) arithmetic calculation per sample - a few thousand times
    /// cheaper per candidate. The tradeoff is ~1-month resolution on window boundaries instead
    /// of exact antardasha start/end - acceptable here since windows are already being compared
    /// against known dates at the day level with mean-window-width tracked as a caveat anyway.
    /// </summary>
    public static (DateTimeOffset windowStart, DateTimeOffset windowEnd)? PredictedMarriageWindow(
        Time birthTime, bool isMale, int scanYears = 100)
    {
        const double stepDays = 30;
        const double startAgeYears = 18;

        var seventhLord = Calculate.LordOfHouse(HouseName.House7, birthTime);

        DateTimeOffset? windowStart = null;

        for (var ageDays = startAgeYears * 365.25; ageDays <= scanYears * 365.25; ageDays += stepDays)
        {
            var sampleTime = birthTime.AddHours(ageDays * 24);
            var antardashaLord = VimshottariDasa.CurrentDasa8Levels(birthTime, sampleTime).PD2;
            var isFavorable = IsMarriageFavorableLord(antardashaLord, seventhLord, isMale);

            if (isFavorable && windowStart == null)
            {
                windowStart = sampleTime.GetStdDateTimeOffset();
            }
            else if (!isFavorable && windowStart != null)
            {
                return (windowStart.Value, sampleTime.GetStdDateTimeOffset());
            }
        }

        // still inside the favorable window when the scan range ran out - close it there
        return windowStart == null
            ? null
            : (windowStart.Value, birthTime.AddHours(scanYears * 365.25 * 24).GetStdDateTimeOffset());
    }

    private static bool IsMarriageFavorableLord(PlanetName antardashaLord, PlanetName seventhLord, bool isMale)
    {
        if (antardashaLord == PlanetName.Venus) { return true; }
        if (antardashaLord == seventhLord) { return true; }
        if (!isMale && antardashaLord == PlanetName.Jupiter) { return true; }

        return false;
    }

    public static List<ValidationResult> RunValidation(List<MarriageCandidate> candidates)
    {
        // Sequential on purpose: VimshottariDasa.DasaPeriodsOld -> EventManager.CalculateEvents
        // already runs its own internal Parallel.ForEach/Parallel.For per call, so wrapping this
        // loop in an outer Parallel.ForEach oversubscribes the thread pool badly (nested
        // parallelism) instead of speeding things up.
        var results = new List<ValidationResult>();
        var processed = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        foreach (var candidate in candidates)
        {
            var birthTime = candidate.Person.ToBirthTime();
            var isMale = candidate.Person.IsMale();

            var window = PredictedMarriageWindow(birthTime, isMale);

            var hit = window.HasValue
                      && candidate.MarriageDate >= window.Value.windowStart
                      && candidate.MarriageDate <= window.Value.windowEnd;

            results.Add(new ValidationResult(
                candidate.Person.Name,
                candidate.MarriageDate,
                window?.windowStart,
                window?.windowEnd,
                hit));

            processed++;
            if (processed % 100 == 0)
            {
                Console.WriteLine($"Dasha validation: processed {processed}/{candidates.Count} ({stopwatch.Elapsed.TotalSeconds / processed:F2}s/candidate)");
            }
        }

        return results;
    }

    /// <summary>
    /// Entry point for this validation run - builds the candidate set, runs the prediction
    /// check against it, and prints an accuracy summary plus per-person misses/no-window
    /// results (sorted misses-first, so failures are easy to eyeball first).
    /// See docs/MLTesting.md for why accuracy alone isn't enough - always read this alongside
    /// mean window width, since a wider window trivially raises the hit rate.
    /// </summary>
    public static void RunAndPrintReport()
    {
        var candidates = BuildCandidateSet();
        Console.WriteLine($"Candidate set: {candidates.Count} people with high-credibility, fully-dated marriages and Rodden AA birth data");

        var results = RunValidation(candidates);

        var withWindow = results.Where(r => r.WindowStart.HasValue).ToList();
        var hits = withWindow.Count(r => r.Hit);
        var accuracy = withWindow.Count == 0 ? 0 : (double)hits / withWindow.Count;
        var meanWindowYears = withWindow.Count == 0
            ? 0
            : withWindow.Average(r => (r.WindowEnd!.Value - r.WindowStart!.Value).TotalDays / 365.25);

        Console.WriteLine($"No predicted window found: {results.Count - withWindow.Count}");
        Console.WriteLine($"Accuracy (actual date inside predicted window): {accuracy:P1} ({hits}/{withWindow.Count})");
        Console.WriteLine($"Mean window width: {meanWindowYears:F1} years");
        Console.WriteLine();

        foreach (var result in results.OrderBy(r => r.Hit))
        {
            var windowText = result.WindowStart.HasValue
                ? $"{result.WindowStart:yyyy-MM-dd} .. {result.WindowEnd:yyyy-MM-dd}"
                : "(no window found)";

            Console.WriteLine($"[{(result.Hit ? "HIT " : "MISS")}] {result.PersonName,-30} actual={result.ActualMarriageDate:yyyy-MM-dd} window={windowText}");
        }
    }
}
