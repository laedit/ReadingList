using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AddBook.Business.Database
{
    internal sealed class Books
    {
        private readonly IList<BooksItem> booksList;

        public Books(IList<BooksItem> booksList)
        {
            this.booksList = booksList;
        }

        internal void Add(string isbn, DateTime startDate, bool generated)
        {
            booksList.Add(new BooksItem { Date = startDate.ToString("yyyy-MM-dd"), Generated = generated, Isbn = isbn });
        }

        internal Option<BooksItem> Find(string isbn)
        {
            var bookItem = booksList.FirstOrDefault(bi => bi.Isbn == isbn);
            return bookItem == null ? Option<BooksItem>.None : Option<BooksItem>.Some(bookItem);
        }

        internal string Export()
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            return serializer.Serialize(booksList);
        }
    }
}