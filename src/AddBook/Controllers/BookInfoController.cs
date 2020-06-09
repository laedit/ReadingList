using AddBook.Business.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AddBook.Controllers
{
    public class BookInfoController : Controller
    {
        private readonly BookFinder bookFinder;

        public BookInfoController(BookFinder bookFinder)
        {
            this.bookFinder = bookFinder;
        }

        [Authorize]
        [HttpGet("/bookinfo/{isbn}")]
        public async Task<IActionResult> Index(string isbn)
        {
            var searchResult = await bookFinder.Find(isbn);

            return searchResult.Book.Match(book =>
            {
                return Json(new { Status = "ok", Book = book, Logs = searchResult.Logs });
            },
            () =>
            {
                var jsonResponse = Json(new { Status = "error", Logs = searchResult.Logs });
                jsonResponse.StatusCode = 404;
                return jsonResponse;
            });
        }
    }
}
