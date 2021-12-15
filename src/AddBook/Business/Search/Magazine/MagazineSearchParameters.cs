namespace AddBook.Business.Search.Magazine
{
    public class MagazineSearchParameters
    {
        public MagazineName Name { get; set; }

        public string Number { get; set; }

        public string GetKey()
        {
            return $"{Name}-{Number.Replace(" ", "")}".ToLowerInvariant();
        }
    }
}
