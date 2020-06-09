using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AddBook.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using AddBook.Business.Generation;
using System.Threading.Tasks;
using AddBook.Business.GitHub;
using AddBook.Business.Database;
using System.Collections.Generic;

namespace AddBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly GitHubHelper gitHubHelper;
        private readonly PostGenerator postGenerator;
        private readonly BooksRepository booksRepository;

        public HomeController(GitHubHelper gitHubHelper, PostGenerator postGenerator, BooksRepository booksRepository)
        {
            this.gitHubHelper = gitHubHelper;
            this.postGenerator = postGenerator;
            this.booksRepository = booksRepository;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View(new BookPost { StartDate = DateTime.Now });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Index(BookPost book)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage));

                var view = View(book);
                view.StatusCode = 400;
                return view;
            }

            // Generates post
            var generatedBookPost = await postGenerator.GeneratePost(book);

            // Add book to books.yml
            var books = await booksRepository.GetBooks();
            books.Add(book.Isbn, book.StartDate, true);
            var booksContentUpdated = books.Export();

            // Commit it on github
            var filesToCommit = new List<CommitItem>
                {
                    CommitItem.Create(BooksRepository.Path, booksContentUpdated),
                    CommitItem.Create(generatedBookPost.PostPath, generatedBookPost.PostContent)
                };
            if (!string.IsNullOrEmpty(generatedBookPost.ImagePath))
            {
                filesToCommit.Add(CommitItem.Create(generatedBookPost.ImagePath, generatedBookPost.ImageContent));
            }

            gitHubHelper.Commit($"Add new book '{generatedBookPost.BookTitle}'", filesToCommit);
            ViewBag.Success = true;
            return View(new BookPost { StartDate = book.StartDate });
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
