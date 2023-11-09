using AddBook.Models;
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

        private static readonly HttpClient httpClient;

        static PostGenerator()
        {
            httpClient = new();
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/*"));
        }

        protected abstract string FormatContent(Post bookPost, string imageFileName);

        internal async Task<GeneratedPost> GeneratePost(Post bookPost)
        {
            string imageFileName;
            string imagePath = null;
            byte[] imageContent = null;

            if (bookPost.CoverUrl.IsAbsoluteUri)
            {
                var imageResponse = await httpClient.GetAsync(bookPost.CoverUrl);
                imageContent = await imageResponse.Content.ReadAsByteArrayAsync();

                var extension = imageResponse.Content.Headers.ContentType.MediaType?.Replace("image/", "");
                if(string.IsNullOrEmpty(extension))
                {
                    extension = Path.GetExtension(bookPost.CoverUrl.AbsolutePath);
                }
                else
                {
                    extension = $".{extension}";
                }
                imageFileName = $"{bookPost.GetKey().Slugify()}{extension}";
                imagePath = $"{ImagesFolder}{imageFileName}";
            }
            else
            {
                imageFileName = bookPost.CoverUrl.OriginalString.Replace("/img/", "");
            }

            var postPath = $"{PostsFolder}{bookPost.StartDate:yyyy-MM-dd}-{bookPost.Title.Slugify()}.md";
            var postContent = FormatContent(bookPost, imageFileName);
            return new GeneratedPost { ImagePath = imagePath, ImageContent = imageContent, PostPath = postPath, PostContent = postContent, PostTitle = bookPost.Title };
        }
    }
}