using AddBook.Business.Database;
using AddBook.Business.Generation;
using AddBook.Business.GitHub;
using AddBook.Business.Search.Book;
using AddBook.Business.Search.Magazine;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace AddBook
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            // FIXME
            // https://stackoverflow.com/questions/46243697/asp-net-core-persistent-authentication-custom-cookie-authentication
            // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-7.0
            // https://stackoverflow.com/questions/53533894/asp-net-core-cookie-authentication-sliding-expiration-not-working
            // IsPersistent = true ?
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.LoginPath = "/Login";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                    options.SlidingExpiration = true;
                });
            // TODO check if necessary
            services.AddAuthentication(options =>
                 {
                     options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                 });

            services.AddSingleton<BookPostGenerator>();
            services.AddSingleton<GitHubHelper>();
            services.AddSingleton<BooksRepository>();
            services.AddSingleton<BookFinder>();
            services.AddSingleton<MagazineFinder>();
            services.AddSingleton<MagazinePostGenerator>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            //    app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
            //}

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                HttpOnly = HttpOnlyPolicy.Always,
                MinimumSameSitePolicy = SameSiteMode.Strict,
                Secure = CookieSecurePolicy.Always
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet(".well-known/acme-challenge/{id}", async context =>
                {
                    var id = context.GetRouteValue("id") as string;
                    var file = Path.Combine(env.WebRootPath, ".well-known", "acme-challenge", id);
                    await context.Response.SendFileAsync(file);
                });

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
