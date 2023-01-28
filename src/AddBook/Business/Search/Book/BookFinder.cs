using AddBook.Business.Database;
using AddBook.Business.GitHub;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Book
{
    public class BookFinder
    {
        private static readonly ConcurrentDictionary<string, SearchResult<Book>> _cache = new();

        private static List<IBookSearch> _bookSearches;

        public BookFinder(BooksRepository booksRepository, GitHubHelper gitHubHelper)
        {
            _bookSearches = new List<IBookSearch>
            {
                new PostsBookSearch(booksRepository, gitHubHelper),
                new KleberLibraryBookSearch(),
                new FnacSiteBookSearch()
            };
        }

        internal async Task<SearchResult<Book>> Find(string isbn)
        {
            SearchResult<Book> searchResult = null;

            if (!_cache.ContainsKey(isbn))
            {
                searchResult = await FindBook(isbn);
            }
#if !DEBUG
            return _cache.GetOrAdd(isbn, searchResult);
#else
            return searchResult;
#endif
        }

        private static async Task<SearchResult<Book>> FindBook(string isbn)
        {
            var searchResult = new SearchResult<Book> { Result = Option<Book>.None };
            var logs = new List<string>();

            foreach (var bookSearch in _bookSearches)
            {
                var result = await bookSearch.Search(isbn);

                searchResult = result.Match(book =>
                {
                    searchResult.Result = Option<Book>.Some(book);
                    logs.Add($"{bookSearch.GetType().Name} have found the book.");
                    return searchResult;
                },
                failReason =>
                {
                    logs.Add($"{bookSearch.GetType().Name}: {failReason}");
                    return searchResult;
                });

                if (searchResult.Result.HasSome())
                {
                    break;
                }
            }
            searchResult.Logs = logs;
            return searchResult;
        }
    }
}