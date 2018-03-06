﻿using AddBook.ViewModels;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AddBook.Modules
{
    public class IndexModule : NancyModule
    {
        public IndexModule()
        {
            this.RequiresAuthentication();

            Get["/"] = _ =>
            {
                return View["index", new Book { StartDate = DateTime.Now }];
            };

            Post["/", true] = async (_, ctx) =>
            {
                var book = this.Bind<Book>();
                await StartAppVeyorBuild(book);
                return View["index", book];
            };
        }

        private async Task StartAppVeyorBuild(Book book)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Configuration.AppVeyorApiKey);

                // get the list of roles
                using (var response = await client.PostAsync("https://ci.appveyor.com/api/builds", new StringContent(
   new JavaScriptSerializer().Serialize(new {
                    accountName = "laedit",
                    projectSlug = "readinglist",
                    branch = "master",
                    environmentVariables = new { isbn = book.ISBN.Trim(), startDate = book.StartDate.ToString("yyyy-MM-dd") }
                }), System.Text.Encoding.UTF8, "application/json")))
                {
                    response.EnsureSuccessStatusCode();
                    ViewBag["success"] = true;
                }
            }
        }
    }
}