using System.Threading.Tasks;

namespace AddBook.Business.Search.Book
{
    public interface IBookSearch
    {
        string Name { get; }

        Task<Result<Book>> Search(string isbn);
    }
}