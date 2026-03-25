using AngleSharp.Html.Parser;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    public class JVMagazineSearch : IMagazineSearch
    {
        private const string BaseUrl = "https://www.jvlemag.com";
        private readonly HttpClient httpClient = new();

        public bool CanSearch(MagazineName magazineName)
        {
            Console.WriteLine($"check if {magazineName} is JVLeMag");
            return magazineName == MagazineName.JVLeMag;
        }

        public async Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters)
        {
            Console.WriteLine($"Searching JVLeMag {magazineSearchParameters.Number}");
            var parser = new HtmlParser();
            var magazinesHtmlDoc = parser.ParseDocument(await httpClient.GetStringAsync($"{BaseUrl}/magazines/"));
            var magazinesDivs = magazinesHtmlDoc.QuerySelectorAll("div.product-item");

            if (magazinesDivs.Count > 0)
            {
                var foundMagazineDiv = magazinesDivs.FirstOrDefault(magazinesDiv =>
                {
                    var title = magazinesDiv.QuerySelector("p.title>a")?.TextContent?.Trim();
                    return !string.IsNullOrWhiteSpace(title) && title.Contains(magazineSearchParameters.Number);
                });

                if (foundMagazineDiv != null)
                {
                    var magazineLink = foundMagazineDiv.QuerySelector("a")?.GetAttribute("href");

                    var magazineHtmlDoc = parser.ParseDocument(await httpClient.GetStringAsync($"{BaseUrl}/{magazineLink}"));
                    return Result<Magazine>.Success(new Magazine
                    {
                        Title = magazineHtmlDoc.QuerySelector("h2")?.TextContent?.Trim(),
                        CoverUrl = magazineHtmlDoc.QuerySelector(".field-field-image img")?.GetAttribute("src"),
                        Summary = HtmlToMarkdown.Convert(magazineHtmlDoc.QuerySelector(".field-field-short-description")) //?.TextContent?.Trim()
                    });
                }
                return Result<Magazine>.Fail($"Magazine {magazineSearchParameters.Name}-{magazineSearchParameters.Number} not found.");
            }

            return Result<Magazine>.Fail($"No magazines found.");
        }
    }
}
