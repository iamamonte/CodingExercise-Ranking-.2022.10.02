using System.Collections.Generic;
using System.Linq;

namespace FilterAndRank
{
    public static class Ranking
    {
        private record PeopleRankModel(long Id, string Name, CountryRanking Ranking);

        public static IList<RankedResult> FilterByCountryWithRank(
            IList<Person> people,
            IList<CountryRanking> rankingData,
            IList<string> countryFilter,
            int minRank, int maxRank,
            int maxCount)
        {
            // no countries to filter returns empty list
            var result = new List<RankedResult>();
            if (countryFilter.Count == 0) return result;

            // remove persons without rank or not matching the country
            var validPersons = people.Select(p =>
                new PeopleRankModel(p.Id, p.Name, rankingData.FirstOrDefault(r => r.PersonId == p.Id)))
                .Where(pr =>
                    countryFilter.Any(c =>
                        pr.Ranking?.Country.Equals(c, System.StringComparison.OrdinalIgnoreCase) ?? false));

            // enforce min/max ranking bounds
            validPersons = validPersons.Where(pr => pr.Ranking.Rank >= minRank && pr.Ranking.Rank <= maxRank);

            // order
            var orderedRecords = validPersons.OrderBy(pr => pr.Ranking.Rank);

            if (!orderedRecords.Any()) return result;

            var count = 0;
            while (count < maxCount)
            {

                foreach (var grouping in orderedRecords.GroupBy(pr => pr.Ranking.Rank))
                {
                    foreach (var country in countryFilter)
                    {
                        var resultsToAdd = grouping.Where(pr => pr.Ranking.Country == country)
                            .OrderBy(pr => pr.Name.ToLowerInvariant())
                            .Select(p => new RankedResult(p.Id, p.Ranking.Rank))
                            .Take(maxCount - count);

                        count += resultsToAdd.Count();

                        result.AddRange(resultsToAdd);

                    }

                }


            }


            return result;
        }
    }
}
