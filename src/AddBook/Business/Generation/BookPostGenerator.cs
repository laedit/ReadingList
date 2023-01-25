using AddBook.Models;

namespace AddBook.Business.Generation
{
    public sealed class BookPostGenerator : PostGenerator
    {
        private const string PostTemplate = @"---
layout: book
date: ""{6}""
title: ""{0}""
author: ""{1}""
isbn: ""{2}""
editor: ""{3}""
---
![Couverture](/img/{4}){5}";

        protected override string FormatContent(Post bookPost, string imageFileName)
        {
            return string.Format(PostTemplate, bookPost.Title, bookPost.Author, bookPost.Isbn, bookPost.Editor, imageFileName, bookPost.Summary, bookPost.StartDate.ToString("yyyy-MM-dd HH:mm"));
        }
    }
}