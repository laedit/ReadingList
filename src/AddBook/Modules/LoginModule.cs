using AddBook.ViewModels;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.ModelBinding;
using System;

namespace AddBook.Modules
{
    public class LoginModule : NancyModule
    {
        public LoginModule() : base("/login")
        {
            Get["/"] = _ =>
            {
                return View["login"];
            };

            Post["/"] = _ =>
            {
                var loginData = this.Bind<LoginData>();

                if (Configuration.SoleUser == null || loginData.Username != Configuration.SoleUser.Username || loginData.Password != Configuration.SoleUser.Password)
                {
                    return "username and/or password was incorrect";
                }

                return this.LoginAndRedirect(Guid.NewGuid());
            };
        }
    }
}