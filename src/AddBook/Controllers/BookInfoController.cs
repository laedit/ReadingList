using AddBook.Business.Search;
using AddBook.Business.Search.Book;
using AddBook.Business.Search.Magazine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AddBook.Controllers
{
    public class BookInfoController : Controller
    {
        private readonly BookFinder bookFinder;
        private readonly MagazineFinder magazineFinder;

        public BookInfoController(BookFinder bookFinder, MagazineFinder magazineFinder)
        {
            this.bookFinder = bookFinder;
            this.magazineFinder = magazineFinder;
        }

        [Authorize]
        [HttpGet("/bookinfo/{isbn}")]
        public async Task<IActionResult> GetBookInfo(string isbn)
        {
            var searchResult = await bookFinder.Find(isbn);

            return HandleSearchResult(searchResult);
        }

        [Authorize]
        [HttpGet("/magazineinfo/{name}/{number}")]
        public async Task<IActionResult> GetMagazineInfo([FromRoute] MagazineSearchParameters magazineSearchParameters)
        {
            var searchResult = await magazineFinder.Find(magazineSearchParameters);

            return HandleSearchResult(searchResult);
        }

        private IActionResult HandleSearchResult<T>(SearchResult<T> searchResult)
        {
            return searchResult.Result.Match(result =>
            {
                return Json(new { Status = "ok", Result = result, Logs = searchResult.Logs });
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
