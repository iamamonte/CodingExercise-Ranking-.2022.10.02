using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;

namespace FilterAndRank
{
    public static class Ranking
    {
        private record PeopleRankModel(long Id, string Name, CountryRanking Ranking)
        {
            public string OrderHash { get; set; }
        }

        private static string CreateOrderHash(
            this PeopleRankModel model,
            Dictionary<string, int> countryRanks,
            Dictionary<string, Dictionary<string, int>> peopleCountryRanks)
        {
            var countryKey = model.Ranking.Country.ToLowerInvariant();
            var nameKey = model.Name.ToLowerInvariant();

            model.OrderHash =
                $"{model.Ranking.Rank}|{countryRanks[countryKey]}|{peopleCountryRanks[countryKey][nameKey]}";

            return model.OrderHash;
        }

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
            var validRankModels = people.Select(p =>
                new PeopleRankModel(p.Id, p.Name, rankingData.FirstOrDefault(r => r.PersonId == p.Id)))
                .Where(pr =>
                    countryFilter.Any(c =>
                        pr.Ranking?.Country.Equals(c, System.StringComparison.OrdinalIgnoreCase) ?? false));

            // enforce min/max ranking bounds
            validRankModels = validRankModels.Where(pr => pr.Ranking.Rank >= minRank && pr.Ranking.Rank <= maxRank).ToList();

            // rank countries
            var countryRanks = new Dictionary<string, int>();
            var count = 0;
            foreach (var country in countryFilter.Select(x => x.ToLowerInvariant()))
            {
                if (!countryRanks.ContainsKey(country)) countryRanks.Add(country, count++);
            }

            // rank names
            var peopleCountryRanks = new Dictionary<string, Dictionary<string, int>>();
            foreach (var group in validRankModels.GroupBy(x => x.Ranking.Country.ToLowerInvariant()))
            {
                count = 0;
                var country = group.Key.ToLowerInvariant();
                var dictionary = new Dictionary<string, int>();
                foreach (var ranking in group.OrderBy(x => x.Name.ToLowerInvariant()))
                {
                    var name = ranking.Name.ToLowerInvariant();
                    if (!dictionary.ContainsKey(name))
                    {
                        dictionary.Add(name, count++);
                    }
                }
                peopleCountryRanks.Add(country, dictionary);
            }


            // order
            var orderedRecords = validRankModels.OrderBy(pr => pr.CreateOrderHash(countryRanks, peopleCountryRanks));

            if (!orderedRecords.Any()) return result;

            maxCount = maxCount > orderedRecords.Count() ? orderedRecords.Count() : maxCount;

            // consider max count
            var remainder = maxCount;

            while (remainder >  0)
            {

                foreach (var grouping in orderedRecords.GroupBy(pr => pr.Ranking.Rank))
                {
                    if (remainder <= 0) break;

                    var take = grouping.Count() > remainder ? grouping.Count() : remainder;

                    var resultsToAdd = grouping
                           .Select(p => new RankedResult(p.Id, p.Ranking.Rank))
                           .Take(take);

                    remainder -= resultsToAdd.Count();

                    result.AddRange(resultsToAdd);
                }


            }
            return result;
        }
    }
}
