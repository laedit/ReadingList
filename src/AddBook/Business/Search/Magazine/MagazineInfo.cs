namespace AddBook.Business.Search.Magazine
{
    public class MagazineInfo
    {
        public string ImageGrandFormat { get; set; }
        public string Libelle { get; set; }
        public Tarif Tarif { get; set; }
    }

    public class Tarif
    {
        public string Description { get; set; }
    }
}
