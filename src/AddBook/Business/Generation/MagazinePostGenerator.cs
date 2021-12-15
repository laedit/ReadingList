using AddBook.Models;

namespace AddBook.Business.Generation
{
    public sealed class MagazinePostGenerator : PostGenerator
    {
        private const string PostTemplate = @"---
layout: magazine
title: ""{0}""
---
![Couverture](/img/{1}){2}";

        protected override string FormatContent(Post bookPost, string imageFileName)
        {
            return string.Format(PostTemplate, bookPost.Title, imageFileName, bookPost.Summary);
        }
    }
}