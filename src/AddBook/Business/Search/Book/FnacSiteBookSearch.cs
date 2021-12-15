using AngleSharp.Html.Parser;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Book
{
    internal sealed class FnacSiteBookSearch : IBookSearch
    {
        private readonly HttpClient httpClient;

        public FnacSiteBookSearch()
        {
            httpClient = InstanciateHttpClient();
        }

        public async Task<Result<Book>> Search(string isbn)
        {
            try
            {
                var parser = new HtmlParser();
                var htmlDoc = parser.ParseDocument(await httpClient.GetStringAsync($"https://recherche.fnac.com/SearchResult/ResultList.aspx?SCat=2!1&sft=1&Search={isbn}"));
                var noResultDivs = htmlDoc.QuerySelectorAll("div.txt_c.noResults.mrg_b_xlg");
                if (noResultDivs.Any())
                {
                    return Result<Book>.Fail("Data not found.");
                }

                var bookLink = htmlDoc.QuerySelector("a.js-minifa-title");
                if (bookLink == null)
                {
                    return Result<Book>.Fail("Book infos link not found.");
                }

                htmlDoc = parser.ParseDocument(await httpClient.GetStringAsync(bookLink.Attributes["href"].Value));

                var characteristics = htmlDoc.QuerySelector("section[id='Characteristics']");

                var author = WebUtility.HtmlDecode(string.Join(", ", characteristics?.QuerySelectorAll("dl.characteristicsStrate__item")
                    .Where(node => node.FirstElementChild.TextContent == "Auteur").SelectMany(n => n
                    .Children[1].Children.Select(c => c.TextContent.Trim()))));

                author = AddToAuthor(characteristics, author, "Scénario");
                author = AddToAuthor(characteristics, author, "Dessinateur");
                author = AddToAuthor(characteristics, author, "Illustration", illustrator => illustrator != "(donnée non spécifiée)" && illustrator != "Illustrations couleur" && illustrator != "Pas d'illustrations");

                var editor = WebUtility.HtmlDecode(characteristics?.QuerySelectorAll("dl.characteristicsStrate__item")
                    .First(node => node.FirstElementChild.TextContent == "Editeur")
                    .Children[1].TextContent).Trim();

                var coverUrl = htmlDoc.QuerySelector("img.js-ProductVisuals-imagePreview")?.Attributes["src"]?.Value;

                var title = WebUtility.HtmlDecode(htmlDoc.QuerySelector("h1.f-productHeader-Title")?.TextContent?.Trim());

                var summary = WebUtility.HtmlDecode(HtmlToMarkdown.Convert(htmlDoc.QuerySelector(".summaryStrate__raw")));

                return Result<Book>.Success(new Book { Isbn = isbn, Title = title, Author = author, Editor = editor, CoverUrl = coverUrl, Summary = summary });
            }
            catch (Exception ex)
            {
                return Result<Book>.Fail(ex.ToString());
            }
        }

        private static string AddToAuthor(AngleSharp.Dom.IElement characteristics, string author, string characteristicName, Func<string, bool> checkValidity = null)
        {
            var scenarioParentNodes = characteristics?.QuerySelectorAll("dl.characteristicsStrate__item")
                .Where(node => node.FirstElementChild.TextContent == characteristicName);
            if (scenarioParentNodes.Any())
            {
                foreach (var writer in scenarioParentNodes.SelectMany(n => n.Children[1].Children.Select(c => WebUtility.HtmlDecode(c.TextContent.Trim()))))
                {
                    if (!author.Contains(writer) && (checkValidity == null || checkValidity(writer)))
                    {
                        if (!string.IsNullOrWhiteSpace(author))
                        {
                            author += ", ";
                        }
                        author += writer;
                    }
                }
            }

            return author;
        }

        private HttpClient InstanciateHttpClient()
        {
            var httpClient = new HttpClient(new HttpClientHandler
                                    {
                                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                                    });
            httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "fr-FR");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Gecko/20100101 Firefox/89.0");
            // FIXME add dynamic referer?
            return httpClient;
        }
    }
}