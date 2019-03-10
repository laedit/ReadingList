using AddBook.Business.Database;
using AddBook.Business.Generation;
using AddBook.Business.GitHub;
using AddBook.ViewModels;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Nancy.Validation;
using System;

namespace AddBook.Modules
{
    public class IndexModule : NancyModule
    {
        public IndexModule(GitHubHelper gitHubHelper, PostGenerator postGenerator, BooksRepository booksRepository)
        {
            this.RequiresAuthentication();

            Get["/"] = _ =>
            {
                return View["index", new BookPost { StartDate = DateTime.Now.ToString("yyyy-MM-dd") }];
            };

            Post["/", true] = async (_, ctx) =>
            {
                var book = this.Bind<BookPost>();
                var validationResult = this.Validate(book);

                if (!validationResult.IsValid)
                {
                    ViewBag["Errors"] = validationResult.Errors;
                    return Negotiate.WithView("index").WithModel(book).WithStatusCode(HttpStatusCode.BadGateway);
                }

                // Generates post
                var generatedBookPost = await postGenerator.GeneratePost(book);

                // Add book to books.yml
                var books = await booksRepository.GetBooks();
                books.Add(book.Isbn, book.StartDate, true);
                var booksContentUpdated = books.Export();

                // Commit it on github
                var filesToCommit = new[]
                {
                    CommitItem.Create(BooksRepository.Path, booksContentUpdated),
                    CommitItem.Create(generatedBookPost.PostPath, generatedBookPost.PostContent)
                };
                if(!string.IsNullOrEmpty(generatedBookPost.ImagePath))
                {
                    CommitItem.Create(generatedBookPost.ImagePath, generatedBookPost.ImageContent);
                }

                gitHubHelper.Commit($"Add new book '{generatedBookPost.BookTitle}'", filesToCommit);
                ViewBag["success"] = true;
                return View["index", new BookPost { StartDate = book.StartDate }];
            };
        }
    }
}