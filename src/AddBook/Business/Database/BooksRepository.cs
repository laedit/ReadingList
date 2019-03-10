using AddBook.Business.GitHub;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AddBook.Business.Database
{
    public sealed class BooksRepository
    {
        internal const string Path = "books.yml";

        private readonly Lazy<Task<Books>> books;

        internal async Task<Books> GetBooks()
        {
            return await books.Value;
        }

        public BooksRepository(GitHubHelper gitHubHelper)
        {
            books = new Lazy<Task<Books>>(async () =>
            {
                var booksList = await gitHubHelper.GetFileContent(Path);
                var deserializer = new DeserializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).Build();
                return new Books(deserializer.Deserialize<List<BooksItem>>(booksList));
            });
        }
    }
}