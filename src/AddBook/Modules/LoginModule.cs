using AddBook.ViewModels;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.ModelBinding;
using System;

namespace AddBook.Modules
{
    public class LoginModule : NancyModule
    {
        private readonly Configuration configuration;

        public LoginModule(Configuration configuration) : base("/login")
        {
            Get["/"] = _ =>
            {
                return View["login"];
            };

            Post["/"] = _ =>
            {
                var loginData = this.Bind<LoginData>();

                if (loginData.Username != configuration.SoleUser.Username
                 || loginData.Password != configuration.SoleUser.Password)
                {
                    return "username and/or password was incorrect";
                }

                return this.LoginAndRedirect(Guid.NewGuid());
            };
            this.configuration = configuration;
        }
    }
}