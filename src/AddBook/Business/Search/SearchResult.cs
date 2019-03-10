namespace AddBook.Business.Search
{
    internal sealed class SearchResult
    {
        public Option<Book> Book { get; set; }

        public string Logs { get; set; }
    }
}