using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AddBook.Business.Search.Book
{
    internal sealed class KleberLibraryBookSearch : IBookSearch
    {
        private readonly HttpClient httpClient;

        public string Name => "Librairie Kléber";

        public KleberLibraryBookSearch()
        {
            httpClient = new HttpClient();
        }

        public async Task<Result<Book>> Search(string isbn)
        {
            try
            {
                var response = await httpClient.GetAsync($"https://www.librairie-kleber.com/json/product/index/search_simple/{isbn}");

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception($"Error during kleber library search: {await response.Content.ReadAsStringAsync()}");
                }

                var searchResult = await response.Content.ReadFromJsonAsync<KleberLibrarySearchResult>();

                if (searchResult.Products.Count == 0)
                {
                    return Result<Book>.Fail("Data not found.");
                }

                return Result<Book>.Success(new Book
                {
                    Isbn = isbn,
                    Title = searchResult.Products[0].Name,
                    Author = searchResult.Products[0].Authors,
                    Editor = searchResult.Products[0].Publisher,
                    CoverUrl = searchResult.Products[0].Photo_Recto ?? searchResult.Products[0].Photo_Moyen ?? searchResult.Products[0].Photo_Vign,
                    Summary = searchResult.Products[0].Description
                });
            }
            catch (Exception ex)
            {
                return Result<Book>.Fail(ex.ToString());
            }
        }
    }
}
