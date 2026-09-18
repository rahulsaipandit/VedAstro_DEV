using System;
using System.Collections.Generic;
using System.Linq;

namespace VedAstro.Library
{
    /// <summary>
    /// Shared candidate-birth-time sweep logic used by both the existing SVG-stack BTR endpoint
    /// (BirthTimeFinderAPI) and the questionnaire scoring endpoint (BirthTimeScoringAPI) - pulled
    /// out of BirthTimeFinderAPI.cs so the two don't duplicate the sweep/defaulting logic.
    /// </summary>
    public static class BirthTimeCandidateSweep
    {
        public record Result(Person FoundPerson, TimeRange LifeTimeRange, List<Time> PossibleTimeList);

        /// <summary>
        /// Looks up the person, defaults the life-span time range (birth date -> birth date + 100
        /// years unless overridden), and sweeps the given hour window on the birth day at
        /// precisionInHours steps (defaults to the whole day).
        /// </summary>
        public static Result Build(string personId, string? startDate, string? endDate,
            string? startHour, string? endHour, double precisionInHours)
        {
            var foundPerson = Tools.GetPersonById(personId);

            //Tools.GetPersonById doesn't throw for an unknown id, it silently returns Person.Empty
            //(a placeholder with a garbage birth time) - the SVG endpoint happened to still fail
            //downstream in chart generation on that garbage data, but the scoring engine below has
            //no such downstream check, so it must be rejected explicitly here.
            if (Person.Empty.Equals(foundPerson))
            {
                throw new Exception($"Person not found: {personId}");
            }

            //time range defaults to full life (birth date -> birth date + 100 years), caller can override
            var startDateParsed = startDate ?? foundPerson.BirthDateMonthYear;
            var endDateParsed = endDate ?? $"{foundPerson.BirthDateMonthYear[..6]}{foundPerson.BirthYear + 100}";
            var start = new Time($"00:00 {startDateParsed} {foundPerson.BirthTimeZoneString}", foundPerson.GetBirthLocation());
            var end = new Time($"00:00 {endDateParsed} {foundPerson.BirthTimeZoneString}", foundPerson.GetBirthLocation());
            var timeRange = new TimeRange(start, end);

            //get list of possible birth times within the given hour range on the birth day (defaults to whole day)
            var startHourParsed = new Time($"{startHour ?? "00:00"} {foundPerson.BirthDateMonthYearOffset}", foundPerson.GetBirthLocation());
            var endHourParsed = new Time($"{endHour ?? "23:59"} {foundPerson.BirthDateMonthYearOffset}", foundPerson.GetBirthLocation());
            var possibleTimeList = Time.GetTimeListFromRange(startHourParsed, endHourParsed, precisionInHours);

            return new Result(foundPerson, timeRange, possibleTimeList);
        }

        /// <summary>A Person clone per candidate time, birth time swapped in via ChangeBirthTime.</summary>
        public static IEnumerable<Person> CandidatePersons(Result sweep) =>
            sweep.PossibleTimeList.Select(t => sweep.FoundPerson.ChangeBirthTime(t));
    }
}
