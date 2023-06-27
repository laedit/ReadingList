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
    internal class WooCommerceMagazineSearch : IMagazineSearch
    {
        private readonly HttpClient httpClient;

        public WooCommerceMagazineSearch()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json; charset=utf-8");
        }

        public bool CanSearch(MagazineName magazineName)
        {
            return magazineName == MagazineName.Sick;
        }

        public async Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters)
        {
            try
            {
                var paged = 1;
                var magazineResponse = await httpClient.GetAsync($"https://admin.sick-magazine.com/api/core/v2/products/?cat=numeros-uniques&numberposts=1000&paged={paged}");

                if (magazineResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Error during magazine search: {await magazineResponse.Content.ReadAsStringAsync()}");
                }

                var magazinesInfo = await magazineResponse.Content.ReadFromJsonAsync<List<WooCommerceMagazineInfo>>();

                var magazine = magazinesInfo.FirstOrDefault(mi => mi.Name.ToUpperInvariant() == $"S!CK #{int.Parse(magazineSearchParameters.Number):000}");

                if (magazine == null)
                {
                    return Result<Magazine>.Fail($"No magazine found for search '{magazineSearchParameters.Number}' in {magazineSearchParameters.Name}.");
                }

                // FIXME prendre en compte le cas où le numéro n'est pas dans la liste et qu'il faut chercher la suite
                return Result<Magazine>.Success(new Magazine
                {
                    Title = $"{magazine.Name} - {magazine.Subtitle}",
                    CoverUrl = magazine.Image,
                    Summary = magazine.Description == null ? "" : HtmlToMarkdown.Convert(new HtmlParser().ParseDocument(WebUtility.HtmlDecode(magazine.Description)))
                });
            }
            catch (Exception ex)
            {
                return Result<Magazine>.Fail(ex.ToString());
            }
        }
    }
}