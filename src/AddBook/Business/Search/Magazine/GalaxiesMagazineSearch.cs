using AngleSharp.Html.Parser;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    public class GalaxiesMagazineSearch : IMagazineSearch
    {
        private const string BaseUrl = "https://galaxiessf.com";
        private readonly HttpClient httpClient = new();

        public bool CanSearch(MagazineName magazineName)
        {
            return magazineName == MagazineName.Galaxies;
        }

        public async Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters)
        {
            var parser = new HtmlParser();
            var magazinesHtmlDoc = parser.ParseDocument(await httpClient.GetStringAsync($"{BaseUrl}/tous-les-numeros/"));
            var magazinesLis = magazinesHtmlDoc.QuerySelectorAll("ul.products>li.product");

            if (magazinesLis.Count > 0)
            {
                var foundMagazineLi = magazinesLis.FirstOrDefault(magazineLi =>
                {
                    var title = magazineLi.QuerySelector("h2")?.TextContent?.Trim();
                    return !string.IsNullOrWhiteSpace(title) && title.EndsWith(magazineSearchParameters.Number);
                });

                if (foundMagazineLi != null)
                {
                    var magazineLink = foundMagazineLi.QuerySelector("a")?.GetAttribute("href");

                    var magazineHtmlDoc = parser.ParseDocument(await httpClient.GetStringAsync(magazineLink));
                    return Result<Magazine>.Success(new Magazine
                    {
                        Title = $"Galaxies N°{magazineSearchParameters.Number}",
                        CoverUrl = magazineHtmlDoc.QuerySelector("#main-content img")?.GetAttribute("src"),
                        Summary = magazineHtmlDoc.QuerySelector("#main-content div.et_pb_module.et_pb_text_1")?.TextContent?.Trim()
                    });
                }
                return Result<Magazine>.Fail($"Magazine {magazineSearchParameters.Name}-{magazineSearchParameters.Number} not found.");
            }

            return Result<Magazine>.Fail($"No magazines found.");
        }
    }
}
