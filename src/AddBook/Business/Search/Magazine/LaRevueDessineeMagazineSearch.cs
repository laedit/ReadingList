using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    internal class LaRevueDessineeMagazineSearch : IMagazineSearch
    {
        private readonly HttpClient httpClient;

        public LaRevueDessineeMagazineSearch()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json; charset=utf-8");
        }

        public bool CanSearch(MagazineName magazineName)
        {
            return magazineName == MagazineName.LaRevueDessinee;
        }

        public async Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters)
        {
            try
            {
                string urlBase = "https://boutique.4revues.fr";
                var magazineResponse = await httpClient.PostAsync($"{urlBase}/core/getapi.php",
                    new FormUrlEncodedContent(new Dictionary<string, string>()
                        {
                            { "datas[oauth][scope]", "public" },
                            { "datas[oauth][editor]", "740" },
                            { "datas[oauth][company]", "1" },
                            { "datas[service]", "productsbycategories/fr/6/0" },
                            { "datas[target]", "prod" },
                            { "datas[scope]", "aboweb" }
                        }));

                if (magazineResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Error during magazine search: {await magazineResponse.Content.ReadAsStringAsync()}");
                }

                var magazinesInfo = await magazineResponse.Content.ReadFromJsonAsync<List<MagazineInfo>>();
                var magazine = magazinesInfo.FirstOrDefault(mi => mi.Libelle.ToLowerInvariant().Contains(magazineSearchParameters.Number.ToLowerInvariant()));

                if (magazine == null)
                {
                    return Result<Magazine>.Fail($"No magazine found for search '{magazineSearchParameters.Number}' in {magazineSearchParameters.Name}.");
                }

                // FIXME prendre en compte le cas où le numéro n'est pas dans la liste et qu'il faut chercher la suite
                return Result<Magazine>.Success(new Magazine
                {
                    Title = (!magazine.Libelle.Trim().StartsWith("La Revue Dessinée ") ? "La Revue Dessinée " : "") + magazine.Libelle.Trim(),
                    CoverUrl = magazine.ImageGrandFormat,
                    Summary = magazine.Tarif == null ? "" : HtmlToMarkdown.Convert(new HtmlParser().ParseDocument(WebUtility.HtmlDecode(magazine.Tarif.Description)))
                });
            }
            catch (Exception ex)
            {
                return Result<Magazine>.Fail(ex.ToString());
            }
        }
    }
}
