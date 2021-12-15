using System.Threading.Tasks;

namespace AddBook.Business.Search.Book
{
    public interface IBookSearch
    {
        Task<Result<Book>> Search(string isbn);
    }
}