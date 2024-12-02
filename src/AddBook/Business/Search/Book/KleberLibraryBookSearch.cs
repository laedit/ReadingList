using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace AddBook.Business.Search.Book
{
    internal sealed class KleberLibraryBookSearch : IBookSearch
    {
        private readonly HttpClient httpClient;

        public string Name => "Librairie Kléber";

        public KleberLibraryBookSearch(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient();
        }

        public async Task<Result<Book>> Search(string isbn)
        {
            try
            {
                var parser = new HtmlParser();
                IHtmlDocument htmlDoc;
                var response = await httpClient.GetAsync($"https://www.librairie-kleber.com/detailrewrite.php?ean={isbn}");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    htmlDoc = parser.ParseDocument(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    // fallback if the advanced search fails
                    response = await httpClient.GetAsync($"https://www.librairie-kleber.com/listeliv.php?base=paper&mots_recherche={isbn}");

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Error during kleber library search: {await response.Content.ReadAsStringAsync()}");
                    }

                    htmlDoc = parser.ParseDocument(await response.Content.ReadAsStringAsync());

                    var noResultDivs = htmlDoc.QuerySelectorAll("p.msg_no_result");
                    if (noResultDivs.Any())
                    {
                        return Result<Book>.Fail("Data not found.");
                    }

                    var bookLink = htmlDoc.QuerySelector("h2.livre_titre>a");
                    if (bookLink == null)
                    {
                        return Result<Book>.Fail("Book infos link not found.");
                    }

                    htmlDoc = parser.ParseDocument(await httpClient.GetStringAsync(bookLink.Attributes["href"].Value));
                }

                Book book = null;
                var jsonLD = htmlDoc.QuerySelector("script[type='application/ld+json']")?.TextContent;
                if (jsonLD != null)
                {
                    var jsonDoc = JsonDocument.Parse(jsonLD);
                    book = new Book
                    {
                        Isbn = isbn,
                        Title = jsonDoc.RootElement.GetProperty("name").GetString(),
                        Author = jsonDoc.RootElement.GetProperty("author").GetProperty("name").GetString(),
                        Editor = jsonDoc.RootElement.GetProperty("brand").GetProperty("name").GetString(),
                        CoverUrl = jsonDoc.RootElement.GetProperty("image").GetString(),
                        Summary = jsonDoc.RootElement.GetProperty("description").GetString()
                    };
                }
                if (book != null)
                {
                    return Result<Book>.Success(book);
                }
                else
                {
                    return Result<Book>.Fail("Json data not found at 'script[type='application/ld+json']'");
                }
            }
            catch (Exception ex)
            {
                return Result<Book>.Fail(ex.ToString());
            }
        }
    }
}
