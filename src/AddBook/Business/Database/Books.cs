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

        internal void Add(string reference, DateTime startDate, bool generated)
        {
            booksList.Add(new BooksItem { Date = startDate.ToString("yyyy-MM-dd"), Generated = generated, Reference = reference });
        }

        internal Option<BooksItem> Find(string reference)
        {
            var bookItem = booksList.FirstOrDefault(bi => bi.Reference == reference);
            return bookItem == null ? Option<BooksItem>.None : Option<BooksItem>.Some(bookItem);
        }

        internal string Export()
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            return serializer.Serialize(booksList);
        }
    }
}