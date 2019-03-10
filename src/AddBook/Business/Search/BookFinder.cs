using AddBook.Business.Database;
using AddBook.Business.GitHub;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AddBook.Business.Search
{
    public class BookFinder
    {
        private static ConcurrentDictionary<string, SearchResult> _cache = new ConcurrentDictionary<string, SearchResult>();

        private static List<IBookSearch> _bookSearches;

        public BookFinder(BooksRepository booksRepository, GitHubHelper gitHubHelper)
        {
            _bookSearches = new List<IBookSearch>
            {
                new PostsBookSearch(booksRepository, gitHubHelper),
                new FnacSiteBookSearch()
            };
        }

        internal async Task<SearchResult> Find(string isbn)
        {
            SearchResult searchResult = null;
            if (!_cache.ContainsKey(isbn))
            {
                searchResult = await FindBook(isbn);
            }
            return _cache.GetOrAdd(isbn, searchResult);
        }

        private static async Task<SearchResult> FindBook(string isbn)
        {
            var searchResult = new SearchResult { Book = Option<Book>.None };
            var logs = new StringBuilder();

            foreach (var bookSearch in _bookSearches)
            {
                var result = await bookSearch.Search(isbn);

                searchResult = result.Match(book =>
                {
                    searchResult.Book = Option<Book>.Some(book);
                    logs.AppendLine($"{bookSearch.GetType().Name} have found the book.");
                    return searchResult;
                },
                failReason =>
                {
                    logs.AppendLine($"{bookSearch.GetType().Name}: {failReason}");
                    return searchResult;
                });

                if (searchResult.Book.HasSome())
                {
                    break;
                }
            }
            searchResult.Logs = logs.ToString();
            return searchResult;
        }
    }
}