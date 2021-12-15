using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    internal interface IMagazineSearch
    {
        Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters);
    }
}