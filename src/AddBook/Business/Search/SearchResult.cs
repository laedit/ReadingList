using System.Collections.Generic;

namespace AddBook.Business.Search
{
    internal sealed class SearchResult
    {
        public Option<Book> Book { get; set; }

        public IEnumerable<string> Logs { get; set; }
    }
}