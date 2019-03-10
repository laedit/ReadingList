using AddBook.Business.Search;
using Nancy;
using Nancy.Security;
using System.Threading.Tasks;

namespace AddBook.Modules
{
    public class BookInfoModule : NancyModule
    {
        private BookFinder bookFinder;

        public BookInfoModule(BookFinder bookFinder) : base("/bookinfo")
        {
            this.RequiresAuthentication();
            this.bookFinder = bookFinder;

            Get["/{isbn}", runAsync: true] = (parameters, cancellationToken) => GetBookInfo(parameters.isbn);
        }

        private async Task<dynamic> GetBookInfo(string isbn)
        {
            var searchResult = await bookFinder.Find(isbn);

            return searchResult.Book.Match(book =>
            {
                return Response.AsJson(new { Status = "ok", Book = book, Logs = searchResult.Logs });
            },
            () =>
            {
                return Response.AsJson(new { Status = "error", Logs = searchResult.Logs }, Nancy.HttpStatusCode.NotFound);
            });
        }
    }
}