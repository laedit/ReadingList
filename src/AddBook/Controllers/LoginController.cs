using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AddBook.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AddBook.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration configuration;

        public LoginController(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Index(LoginData loginData, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                if (loginData.Username == configuration["Username"] && loginData.Password == configuration["Password"])
                {
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, loginData.Username),
                        new Claim(ClaimTypes.Role, "User")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                    return LocalRedirect(returnUrl ?? "/");
                }
                else
                {
                    ModelState.AddModelError("", "Login failed. Please check username and/or password");
                }
            }
            return View();
        }
    }
}
