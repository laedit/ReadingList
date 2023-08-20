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
        private readonly BookPostGenerator bookPostGenerator;
        private readonly MagazinePostGenerator magazinePostGenerator;
        private readonly BooksRepository booksRepository;

        public HomeController(GitHubHelper gitHubHelper, BooksRepository booksRepository, BookPostGenerator bookPostGenerator, MagazinePostGenerator magazinePostGenerator)
        {
            this.gitHubHelper = gitHubHelper;
            this.booksRepository = booksRepository;
            this.bookPostGenerator = bookPostGenerator;
            this.magazinePostGenerator = magazinePostGenerator;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View(new Post { StartDate = DateTime.Now });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Index(Post post)
        {
            if (!ModelState.IsValid)
            {
                post.Errors = ModelState.Values.SelectMany(v => v.Errors.Select(b => b.ErrorMessage));

                var view = View(post);
                view.StatusCode = 400;
                return view;
            }

            // Generates post
            var generatedPost = post.Type switch
            {
                PostType.Book => await bookPostGenerator.GeneratePost(post),
                PostType.Magazine => await magazinePostGenerator.GeneratePost(post),
                _ => throw new Exception($"Type {post.Type} unknown")
            };

            // Add book to books.yml
            var books = await booksRepository.GetBooks();
            books.Add(post.GetKey(), post.StartDate, true);
            var booksContentUpdated = books.Export();

            // Commit it on github
            var filesToCommit = new List<CommitItem>
                {
                    CommitItem.Create(BooksRepository.Path, booksContentUpdated),
                    CommitItem.Create(generatedPost.PostPath, generatedPost.PostContent)
                };
            if (!string.IsNullOrEmpty(generatedPost.ImagePath))
            {
                filesToCommit.Add(CommitItem.Create(generatedPost.ImagePath, generatedPost.ImageContent));
            }

            gitHubHelper.Commit($"Add new {(post.Type == PostType.Book ? "book" : "magazine")} '{generatedPost.PostTitle}'", filesToCommit);

            return View(new Post { StartDate = DateTime.Now, IsBookAdded = true });
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
