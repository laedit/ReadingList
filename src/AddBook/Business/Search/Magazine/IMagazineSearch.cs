using System.Threading.Tasks;

namespace AddBook.Business.Search.Magazine
{
    internal interface IMagazineSearch
    {
        bool CanSearch(MagazineName magazineName);

        Task<Result<Magazine>> Search(MagazineSearchParameters magazineSearchParameters);
    }
}