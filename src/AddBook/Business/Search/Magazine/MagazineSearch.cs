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
    internal class MagazineSearch : IMagazineSearch
    {
        private readonly HttpClient httpClient;

        public MagazineSearch()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json; charset=utf-8");
        }

        public async Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters)
        {
            try
            {
                string urlBase;
                string requestBody;
                switch (magazineSearchParameters.Name)
                {
                    case MagazineName.Epsiloon:
                        urlBase = "https://www.epsiloon.com";
                        requestBody = "datas%5Boauth%5D%5Bscope%5D=public&datas%5Boauth%5D%5Beditor%5D=813&datas%5Boauth%5D%5Bcompany%5D=1&datas%5Bservice%5D=productsbycategories%2Ffr%2F2%2F0&datas%5Btarget%5D=prod";
                        break;
                    case MagazineName.LaRevueDessinee:
                        urlBase = "https://boutique.4revues.fr";
                        requestBody = "datas%5Boauth%5D%5Bscope%5D=public&datas%5Boauth%5D%5Beditor%5D=740&datas%5Boauth%5D%5Bcompany%5D=1&datas%5Bservice%5D=productsbycategories%2Ffr%2F6%2F0&datas%5Btarget%5D=prod";
                        break;
                    default: return Result<Magazine>.Fail($"Magazine '{magazineSearchParameters.Name}' not handled.");
                }

                var magazineResponse = await httpClient.PostAsync($"{urlBase}/core/getapi.php", 
                    new StringContent(requestBody, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded"));

                if(magazineResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Error during magazine search: {await magazineResponse.Content.ReadAsStringAsync()}");
                }

                var magazinesInfo = await magazineResponse.Content.ReadFromJsonAsync<List<MagazineInfo>>();

                var magazine = magazinesInfo.First(mi => mi.Libelle.Contains(magazineSearchParameters.Number));
                // FIXME prendre en compte le cas où le numéro n'est pas dans la liste et qu'il faut chercher la suite
                return Result<Magazine>.Success(new Magazine {
                    Title = (magazineSearchParameters.Name == MagazineName.LaRevueDessinee ? "La Revue Dessinée" : "") + magazine.Libelle,
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