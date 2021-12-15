using System.Collections.Generic;

namespace AddBook.Business.Search
{
    internal sealed class SearchResult<T>
    {
        public Option<T> Result { get; set; }

        public IEnumerable<string> Logs { get; set; }

    }
}