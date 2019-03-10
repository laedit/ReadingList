using System.Threading.Tasks;

namespace AddBook.Business.Search
{
    public interface IBookSearch
    {
        Task<Result<Book>> Search(string isbn);
    }
}