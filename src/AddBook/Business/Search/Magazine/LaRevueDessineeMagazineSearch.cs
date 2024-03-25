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
                string requestBody = "datas%5Boauth%5D%5Bscope%5D=public&datas%5Boauth%5D%5Beditor%5D=740&datas%5Boauth%5D%5Bcompany%5D=1&datas%5Bservice%5D=productsbycategories%2Ffr%2F6%2F0&datas%5Btarget%5D=prod";

                var magazineResponse = await httpClient.PostAsync($"{urlBase}/core/getapi.php",
                    new StringContent(requestBody, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));

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
