using AddBook.Business.Database;
using AddBook.Business.GitHub;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    public class MagazineFinder
    {
        private static readonly ConcurrentDictionary<string, SearchResult<Magazine>> _cache = new ConcurrentDictionary<string, SearchResult<Magazine>>();

        private static List<IMagazineSearch> _magazineSearches;

        public MagazineFinder(BooksRepository booksRepository, GitHubHelper gitHubHelper)
        {
            _magazineSearches = new List<IMagazineSearch>
            {
                new PostsMagazineSearch(booksRepository, gitHubHelper),
                new MagazineSearch(),
                new WooCommerceMagazineSearch()
            };
        }

        internal async Task<SearchResult<Magazine>> Find(MagazineSearchParameters magazineSearchParameters)
        {
            SearchResult<Magazine> searchResult = null;

            string cacheKey = magazineSearchParameters.GetKey();

            if (!_cache.ContainsKey(cacheKey))
            {
                searchResult = await FindMagazine(magazineSearchParameters);
            }
#if !DEBUG
            return _cache.GetOrAdd(cacheKey, searchResult);
#else
            return searchResult;
#endif
        }

        private static async Task<SearchResult<Magazine>> FindMagazine(MagazineSearchParameters magazineSearchParameters)
        {
            var searchResult = new SearchResult<Magazine> { Result = Option<Magazine>.None };
            var logs = new List<string>();

            foreach (var magazineSearch in _magazineSearches)
            {
                if (magazineSearch.CanSearch(magazineSearchParameters.Name))
                {
                    var result = await magazineSearch.Search(magazineSearchParameters);

                    searchResult = result.Match(magazine =>
                    {
                        searchResult.Result = Option<Magazine>.Some(magazine);
                        logs.Add($"{magazineSearch.GetType().Name} have found the magazine.");
                        return searchResult;
                    },
                    failReason =>
                    {
                        logs.Add($"{magazineSearch.GetType().Name}: {failReason}");
                        return searchResult;
                    });

                    if (searchResult.Result.HasSome())
                    {
                        break;
                    }
                }
            }
            searchResult.Logs = logs;
            return searchResult;
        }
    }
}