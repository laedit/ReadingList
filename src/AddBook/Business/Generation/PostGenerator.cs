﻿using AddBook.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AddBook.Business.Generation
{
    public abstract class PostGenerator
    {
        private const string PostsFolder = "site/_posts/";
        private const string ImagesFolder = "site/img/";

        private static readonly HttpClient httpClient = new ();

        protected abstract string FormatContent(Post bookPost, string imageFileName);

        internal async Task<GeneratedPost> GeneratePost(Post bookPost)
        {
            string imageFileName;
            string imagePath = null;
            byte[] imageContent = null;
            if (bookPost.CoverUrl.IsAbsoluteUri)
            {
                imageFileName = $"{bookPost.GetKey().Slugify()}{Path.GetExtension(bookPost.CoverUrl.AbsolutePath)}";
                imagePath = $"{ImagesFolder}{imageFileName}";
                imageContent = await DownloadImage(bookPost.CoverUrl);
            }
            else
            {
                imageFileName = bookPost.CoverUrl.OriginalString.Replace("/img/", "");
            }

            var postPath = $"{PostsFolder}{bookPost.StartDate:yyyy-MM-dd}-{bookPost.Title.Slugify()}.md";
            var postContent = FormatContent(bookPost, imageFileName);
            return new GeneratedPost { ImagePath = imagePath, ImageContent = imageContent, PostPath = postPath, PostContent = postContent, PostTitle = bookPost.Title };
        }

        protected static async Task<byte[]> DownloadImage(Uri imageUrl)
        {
            return await httpClient.GetByteArrayAsync(imageUrl);
        }
    }
}