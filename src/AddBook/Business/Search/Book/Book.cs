namespace AddBook.Business.Search.Book
{
    public sealed class Book
    {
        public string Isbn { get; set; }

        public string Title { get; set; }

        public string Author { get; set; }

        public string Editor { get; set; }

        public string CoverUrl { get; set; }

        public string Summary { get; set; }
    }
}