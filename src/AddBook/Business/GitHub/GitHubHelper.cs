using Microsoft.Extensions.Configuration;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AddBook.Business.GitHub
{
    public class GitHubHelper
    {
        private const string Owner = "laedit";
        private const string Repository = "readinglist";
        private const string HeadMainRef = "heads/main";

        private readonly IConfiguration configuration;

        public GitHubHelper(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Create image tree item
        /// </summary>
        private static NewTreeItem CreateImageTreeItem(string imagePath, byte[] imageContent, IGitHubClient githubClient, string owner, string repo)
        {
            var newImageBlob = new NewBlob
            {
                Encoding = EncodingType.Base64,
                Content = Convert.ToBase64String(imageContent)
            };

            var imageBlob = githubClient.Git.Blob.Create(owner, repo, newImageBlob).Result;

            return new NewTreeItem
            {
                Mode = "100644", // new file
                Type = TreeType.Blob,
                Path = imagePath,
                Sha = imageBlob.Sha
            };
        }

        /// <summary>
        /// Create text tree item
        /// </summary>
        private static NewTreeItem CreateTextTreeItem(string textPath, string textContent)
        {
            return new NewTreeItem
            {
                Mode = "100644",
                Type = TreeType.Blob,
                Path = textPath,
                Content = textContent
            };
        }

        /// <summary>
        /// Create a commit and push it to the branch
        /// </summary>
        internal async void Commit(string message, IEnumerable<CommitItem> commitItems)
        {
            var githubClient = InstanciateGitHubClient();

            var main = await githubClient.Git.Reference.Get(Owner, Repository, HeadMainRef);
            var baseTree = await githubClient.Git.Commit.Get(Owner, Repository, main.Object.Sha);

            var nt = new NewTree
            {
                BaseTree = baseTree.Sha
            };

            foreach (var treeItem in commitItems.Select(ci => ci.Match(
               CreateTextTreeItem,
               (path, content) => CreateImageTreeItem(path, content, githubClient, Owner, Repository))))
            {
                nt.Tree.Add(treeItem);
            }

            var newTree = await githubClient.Git.Tree.Create(Owner, Repository, nt);
            var newCommit = new NewCommit(message, newTree.Sha, main.Object.Sha);

            var commit = await githubClient.Git.Commit.Create(Owner, Repository, newCommit);

            await githubClient.Git.Reference.Update(Owner, Repository, HeadMainRef, new ReferenceUpdate(commit.Sha));
        }

        /// <summary>
        /// Get content from a file of the repository
        /// </summary>
        /// <param name="filePath">File path in the repository</param>
        /// <returns>Content of the file</returns>
        internal async Task<string> GetFileContent(string filePath)
        {
            var githubClient = InstanciateGitHubClient();
            var fileContent = await githubClient.Repository.Content.GetAllContents(Owner, Repository, filePath);
            return fileContent.First().Content;
        }

        /// <summary>
        /// Search a post in the repository
        /// </summary>
        /// <param name="postDate">Post date in the path</param>
        /// <returns>Path of the post</returns>
        internal async Task<Option<string>> SearchPost(string postDate)
        {
            var githubClient = InstanciateGitHubClient();
            var searchRequest = new SearchCodeRequest(postDate, Owner, Repository)
            {
                In = new[] { CodeInQualifier.Path },
                Path = "site/_posts",
                Extensions = new[] { "md" }
            };

            var searchResult = await githubClient.Search.SearchCode(searchRequest);

            if (searchResult.TotalCount == 0)
            {
                return Option<string>.None;
            }

            return Option<string>.Some(searchResult.Items[0].Path);
        }

        private IGitHubClient InstanciateGitHubClient()
        {
            return new GitHubClient(new ProductHeaderValue("Laedit-ReadingList"))
            {
                Credentials = new Credentials(configuration["AccessToken"])
            };
        }
    }
}
