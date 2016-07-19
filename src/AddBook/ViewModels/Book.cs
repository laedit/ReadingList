using System;

namespace AddBook.ViewModels
{
    public class Book
    {
        public string ISBN { get; set; }

        public DateTime StartDate { get; set; }

        public bool? BuildStartSuccess { get; set; }
    }
}