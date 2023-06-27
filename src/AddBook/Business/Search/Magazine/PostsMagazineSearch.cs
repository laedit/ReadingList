using AddBook.Business.Database;
using AddBook.Business.GitHub;
using System;
using System.Text;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    internal sealed class PostsMagazineSearch : IMagazineSearch
    {
        private readonly BooksRepository booksRepository;
        private readonly GitHubHelper gitHubHelper;

        public PostsMagazineSearch(BooksRepository booksRepository, GitHubHelper gitHubHelper)
        {
            this.booksRepository = booksRepository;
            this.gitHubHelper = gitHubHelper;
        }

        public bool CanSearch(MagazineName magazineName)
        {
            return true;
        }

        public async Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters)
        {
            var reference = magazineSearchParameters.GetKey();

            // Vérifier depuis books.yml et récupérer les infos depuis le post
            return await (await booksRepository.GetBooks()).Find(reference).Match(async bi =>
            {
                var searchResult = await gitHubHelper.SearchPost(bi.Date);

                return await searchResult.Match(async postPathes =>
                {
                    foreach (var postPath in postPathes)
                    {
                        var goodMagazine = false;
                        var magazine = new Magazine();

                        var postContent = (await gitHubHelper.GetFileContent(postPath)).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        // Parsing post content
                        var summary = new StringBuilder();
                        foreach (var postLine in postContent)
                        {
                            if (postLine.StartsWith("title:"))
                            {
                                var postTitle = postLine.Substring(8).Trim('"');
                                if (postTitle.ToLowerInvariant().Contains(magazineSearchParameters.Name.ToString().ToLowerInvariant())
                                && postTitle.Contains(magazineSearchParameters.Number))
                                {
                                    goodMagazine = true;
                                    magazine.Title = postTitle;
                                    continue;
                                }
                                break;
                            }

                            if (postLine.StartsWith("![Couverture]"))
                            {
                                magazine.CoverUrl = postLine.Substring(14, postLine.LastIndexOf(')') - 14);
                                summary.AppendLine(postLine.Substring(postLine.IndexOf(')') + 1));
                                continue;
                            }

                            if (postLine.StartsWith("---") || postLine.StartsWith("layout:"))
                            {
                                continue;
                            }

                            summary.AppendLine(postLine);
                        }
                        if (goodMagazine)
                        {
                            magazine.Summary = summary.ToString().Trim();
                            return Result<Magazine>.Success(magazine);
                        }
                    }
                    return Result<Magazine>.Fail("Post not found in repository.");
                }, () => Task.FromResult(Result<Magazine>.Fail("Post not found in repository.")));
            }, () => Task.FromResult(Result<Magazine>.Fail("Not in the base yet.")));
        }
    }
}