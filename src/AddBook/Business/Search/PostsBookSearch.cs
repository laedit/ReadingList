using AddBook.Business.Database;
using AddBook.Business.GitHub;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AddBook.Business.Search
{
    internal sealed class PostsBookSearch : IBookSearch
    {
        private readonly BooksRepository booksRepository;
        private readonly GitHubHelper gitHubHelper;

        public PostsBookSearch(BooksRepository booksRepository, GitHubHelper gitHubHelper)
        {
            this.booksRepository = booksRepository;
            this.gitHubHelper = gitHubHelper;
        }

        public async Task<Result<Book>> Search(string isbn)
        {
            // Vérifier depuis books.yml et récupérer les infos depuis le post
            return await (await booksRepository.GetBooks()).Find(isbn).Match(async bi =>
            {
                var originalPostPathOption = await gitHubHelper.SearchPost(bi.Date);

                return await originalPostPathOption.Match(async originalPostPath =>
                {
                    var book = new Book { Isbn = isbn };

                    var postContent = (await gitHubHelper.GetFileContent(originalPostPath)).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    // Parsing post content
                    var summary = new StringBuilder();
                    foreach (var postLine in postContent)
                    {
                        if (postLine.StartsWith("title:"))
                        {
                            book.Title = postLine.Substring(8).Trim('"');
                            continue;
                        }
                        if (postLine.StartsWith("author:"))
                        {
                            book.Author = postLine.Substring(9).Trim('"');
                            continue;
                        }
                        if (postLine.StartsWith("editor:"))
                        {
                            book.Editor = postLine.Substring(9).Trim('"');
                            continue;
                        }

                        if (postLine.StartsWith("![Couverture]"))
                        {
                            book.CoverUrl = postLine.Substring(14, postLine.LastIndexOf(')') - 14);
                            summary.AppendLine(postLine.Substring(postLine.IndexOf(')') + 1));
                            continue;
                        }

                        if (postLine.StartsWith("---") || postLine.StartsWith("isbn:") || postLine.StartsWith("layout:"))
                        {
                            continue;
                        }

                        summary.AppendLine(postLine);
                    }
                    book.Summary = summary.ToString().Trim();
                    return Result<Book>.Success(book);
                }, () => Task.FromResult(Result<Book>.Fail("Post not found in repository.")));
            }, () => Task.FromResult(Result<Book>.Fail("Not in the base yet.")));
        }
    }
}