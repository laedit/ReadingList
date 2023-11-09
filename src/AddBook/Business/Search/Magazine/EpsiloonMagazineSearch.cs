using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    public class EpsiloonMagazineSearch : IMagazineSearch
    {
        private const string BaseUrl = "https://www.epsiloon.com";
        private readonly HttpClient httpClient = new HttpClient();

        public bool CanSearch(MagazineName magazineName)
        {
            return magazineName == MagazineName.Epsiloon;
        }

        public async Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters)
        {
            var parser = new HtmlParser();
            var magazinesHtmlDoc = parser.ParseDocument(await httpClient.GetStringAsync($"{BaseUrl}/les-numeros.html"));
            var magazineDivs = magazinesHtmlDoc.QuerySelectorAll("div.magazine__item");

            if (magazineDivs.Any())
            {
                var foundMagazineDiv = magazineDivs.FirstOrDefault(magazineDiv =>
                {
                    var title = magazineDiv.QuerySelector("div.magazine__item__content>h2")?.TextContent;
                    return !string.IsNullOrWhiteSpace(title) && title.Contains(magazineSearchParameters.Number);
                });

                if (foundMagazineDiv != null)
                {
                    var magazineLink = foundMagazineDiv.QuerySelector("a")?.GetAttribute("href");
                    var magazineHtmlDoc = parser.ParseDocument(await httpClient.GetStringAsync($"{BaseUrl}{magazineLink}"));
                    return Result<Magazine>.Success(new Magazine
                    {
                        Title = $"Epsiloon {string.Join(' ', foundMagazineDiv.QuerySelectorAll("h2 > div > span").Select(span => span.TextContent.Trim()))}",
                        CoverUrl = foundMagazineDiv.QuerySelector("img")?.GetAttribute("src"),
                        Summary = magazineHtmlDoc.QuerySelectorAll("article")
                                    .Select(article =>
                                    {
                                        var title = $"**{article.QuerySelector("h2").TextContent}**  ";
                                        var desc1 = article.QuerySelector(".card__desc > p")?.InnerHtml?.Trim();
                                        var desc2 = article.QuerySelector("h2 + p")?.InnerHtml?.Trim();
                                        return title + Environment.NewLine + (desc1 ?? desc2);
                                    })
                                    .Aggregate((summary, article) => summary += Environment.NewLine + Environment.NewLine + article)
                    });
                }
                return Result<Magazine>.Fail($"Magazine {magazineSearchParameters.Name}-{magazineSearchParameters.Number} not found.");
            }

            return Result<Magazine>.Fail($"Magazines not found.");
        }
    }
}
