using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace AddBook.Business.Search.Book
{
    internal sealed class YenPressBookSearch : IBookSearch
    {
        private readonly HttpClient httpClient;

        public string Name => "Yen Pres";

        public YenPressBookSearch(IHttpClientFactory httpClientFactory)
        {
            httpClient = httpClientFactory.CreateClient();
        }

        public async Task<Result<Book>> Search(string isbn)
        {
            try
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://enterprise-search.yenpress.com//api/as/v1/engines/yenpress/search.json"))
                {
                    request.Headers.TryAddWithoutValidation("authorization", "Bearer search-vhfh3tijxttuxhjjmzgajcd4");

                    // lang: json
                    request.Content = new StringContent($$$"""
                    {
                        "query":"{{{isbn}}}",
                        "filters":{"all":[{"none":[{"type":["books-digital","books-audio"]}]}]},
                        "precision":2,
                        "search_fields":{"isbn":{"weight":10}},
                        "result_fields":{
                            "isbn":{"raw":{}},
                            "url":{"raw":{}}
                        },
                        "page":{"size":10,"current":1}
                    }
                    """);
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(request);

                    var jsonResponse = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                    if (!response.IsSuccessStatusCode
                        || jsonResponse.RootElement.GetProperty("results").GetArrayLength() == 0)
                    {
                        return Result<Book>.Fail("No results.");
                    }

                    var firstResult = jsonResponse.RootElement.GetProperty("results").EnumerateArray()
                                        .FirstOrDefault(jsonElement => jsonElement.GetProperty("isbn").GetProperty("raw").GetString() == isbn);
                    System.Console.WriteLine(firstResult.ToString());
                    if (firstResult.ValueKind == JsonValueKind.Null
                        || firstResult.ValueKind == JsonValueKind.Undefined)
                    {
                        return Result<Book>.Fail("No book found in results.");
                    }

                    var parser = new HtmlParser();
                    var bookResponse = await httpClient.GetAsync($"https://yenpress.com{firstResult.GetProperty("url").GetProperty("raw").GetString()}");
                    if (!bookResponse.IsSuccessStatusCode)
                    {
                        return Result<Book>.Fail("Error during book data recovery.");
                    }
                    var htmlDoc = parser.ParseDocument(await bookResponse.Content.ReadAsStringAsync());

                    var book = new Book
                    {
                        Isbn = isbn,
                        Title = htmlDoc.QuerySelector("h1.heading")?.TextContent,
                        Author = string.Join(", ", htmlDoc.QuerySelector("div.story-details").Children.Select(element => element.ChildNodes[1].TextContent)),
                        Editor = htmlDoc.QuerySelectorAll(".type.paragraph").First(element => element.TextContent.ToLowerInvariant() == "imprint")
                                    .ParentElement.Children[1].TextContent,
                        CoverUrl = htmlDoc.QuerySelector("div.book-cover-img > img").GetAttribute("src")
                                    ?? HttpUtility.UrlDecode(htmlDoc.QuerySelector("div.book-cover-img > img").GetAttribute("data-src")),
                        Summary = htmlDoc.QuerySelector("div.content-heading-txt > p").TextContent
                    };

                    return Result<Book>.Success(book);
                }
            }
            catch (Exception ex)
            {
                return Result<Book>.Fail(ex.ToString());
            }
        }
    }
}
