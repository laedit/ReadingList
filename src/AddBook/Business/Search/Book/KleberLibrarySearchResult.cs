using System.Collections.Generic;

namespace AddBook.Business.Search.Book
{
    public class KleberLibrarySearchResult
    {
        public List<KleberLibraryProduct> Products { get; set; }
    }

    public class KleberLibraryProduct
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Authors { get; set; }
        public string Publisher { get; set; }
        public string Photo_Recto { get; set; }
        public string Photo_Vign { get; set; }
        public string Photo_Moyen { get; set; }
    }
}
