using AngleSharp.Html.Parser;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    internal class LaRevueDessineeMagazineSearch : IMagazineSearch
    {
        private const string BaseUrl = "https://www.larevuedessinee.fr";
        private readonly HttpClient httpClient = new HttpClient();

        public bool CanSearch(MagazineName magazineName)
        {
            return magazineName == MagazineName.LaRevueDessinee;
        }

        public async Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters)
        {
            try
            {
                var parser = new HtmlParser();
                var magazinesHtmlDoc = parser.ParseDocument(await httpClient.GetStringAsync($"{BaseUrl}/boutique/?categorie=numeros"));
                var magazineDivs = magazinesHtmlDoc.QuerySelectorAll("div.product-card--shop");

                if (magazineDivs.Any())
                {
                    var foundMagazineDiv = magazineDivs.FirstOrDefault(magazineDiv =>
                    {
                        var title = magazineDiv.QuerySelector("a.product-card__details__titre")?.TextContent;
                        return !string.IsNullOrWhiteSpace(title) && title.Contains(magazineSearchParameters.Number);
                    });

                    if (foundMagazineDiv != null)
                    {
                        var magazineLink = foundMagazineDiv.QuerySelector("a.product-card__details__titre")?.GetAttribute("href");
                        var magazineHtmlDoc = parser.ParseDocument(await httpClient.GetStringAsync(magazineLink));

                        var summary = new StringBuilder();
                        // Couverture
                        summary.AppendLine($"Couverture signée {magazineHtmlDoc.QuerySelector(".cover-infos__author")?.TextContent?.Trim()}");
                        summary.AppendLine();

                        var enquetes = magazineHtmlDoc.QuerySelectorAll("div.enquete-card");
                        if (enquetes.Count > 0)
                        {
                            summary.AppendLine("**Enquêtes :**  ");
                            foreach (var enquete in enquetes)
                            {
                                // Titre = thème - titre
                                summary.Append($"**{enquete.QuerySelector(".enquete-card__theme")?.TextContent?.Trim()} : {enquete.QuerySelector(".enquete-card__titre")?.TextContent?.Trim()}** ");
                                // auteurs
                                summary.Append($"par _");
                                summary.AppendJoin(", ", enquete.QuerySelectorAll(".enquete-card__auteur__nom").Select(auteurCard => auteurCard?.TextContent?.Trim()));
                                summary.AppendLine("_  ");
                                // résumé
                                summary.AppendLine(enquete.QuerySelector(".enquete-card__content")?.TextContent?.Trim());
                                summary.AppendLine();
                            }
                        }

                        summary.AppendLine();

                        var chroniques = magazineHtmlDoc.QuerySelectorAll("div.chronique-card");
                        if (chroniques.Count > 0)
                        {
                            summary.AppendLine("**Chroniques :**");
                            foreach (var chronique in chroniques)
                            {
                                summary.AppendLine($"- {chronique.QuerySelector(".card__surtitre").TextContent.Trim()} : {chronique.QuerySelector(".chronique-card__title").TextContent.Trim()} par _{string.Join(", ", chronique.QuerySelectorAll(".card__auteur__nom").Select(auteurCard => auteurCard?.TextContent?.Trim()))}_");
                            }
                        }

                        // FIXME prendre en compte le cas où le numéro n'est pas dans la liste et qu'il faut chercher la suite
                        return Result<Magazine>.Success(new Magazine
                        {
                            Title = $"{magazineHtmlDoc.QuerySelector("p.numero-header__saison-name")?.TextContent?.Trim()} - {magazineHtmlDoc.QuerySelector("h1")?.TextContent?.Trim()}",
                            CoverUrl = magazineHtmlDoc.QuerySelector("figure.numero-hero__cover-image>img")?.GetAttribute("src"),
                            Summary = summary.ToString()
                        });
                    }
                    return Result<Magazine>.Fail($"Magazine {magazineSearchParameters.Name} - {magazineSearchParameters.Number} not found.");
                }

                return Result<Magazine>.Fail($"Magazines not found.");
            }
            catch (Exception ex)
            {
                return Result<Magazine>.Fail(ex.ToString());
            }
        }
    }
}
