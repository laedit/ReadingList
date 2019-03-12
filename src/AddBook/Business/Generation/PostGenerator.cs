using AddBook.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AddBook.Business.Generation
{
    public class PostGenerator
    {
        private const string PostTemplate = @"---
layout: post
title: ""{0}""
author: ""{1}""
isbn: ""{2}""
editor: ""{3}""
---
![Couverture](/img/{4}){5}";

        private const string PostsFolder = "site/_posts/";
        private const string ImagesFolder = "site/img/";


        internal async Task<GeneratedBookPost> GeneratePost(BookPost bookPost)
        {
            string imageFileName;
            string imagePath = null;
            byte[] imageContent = null;
            if (bookPost.CoverUrl.IsAbsoluteUri)
            {
                imageFileName = $"{bookPost.Isbn}{Path.GetExtension(bookPost.CoverUrl.AbsoluteUri)}";
                imagePath = $"{ImagesFolder}{imageFileName}";
                imageContent = await DownloadImage(bookPost.CoverUrl);
            }
            else
            {
                imageFileName = bookPost.CoverUrl.OriginalString.Replace("/img/", "");
            }

            var postPath = $"{PostsFolder}{bookPost.StartDate}-{bookPost.Title.Slugify()}.md";
            var postContent = string.Format(PostTemplate, bookPost.Title, bookPost.Author, bookPost.Isbn, bookPost.Editor, imageFileName, bookPost.Summary);
            return new GeneratedBookPost { ImagePath = imagePath, ImageContent = imageContent, PostPath = postPath, PostContent = postContent, BookTitle = bookPost.Title };
        }

        private static async Task<byte[]> DownloadImage(Uri imageUrl)
        {
            return await new System.Net.WebClient().DownloadDataTaskAsync(imageUrl);
        }
    }
}