﻿using AddBook.Authentication;
using AddBook.Business.Database;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Extensions;
using Nancy.TinyIoc;
using System.IO;

namespace AddBook
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        // The bootstrapper enables you to reconfigure the composition of the framework,
        // by overriding the various methods and properties.
        // For more information https://github.com/NancyFx/Nancy/wiki/Bootstrapper

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            
            var binDirectory = Path.GetDirectoryName(this.GetType().GetAssemblyPath());
            var configPath = Path.Combine(binDirectory ?? @".\", "app.config");

            container.Register(Configuration.Load(configPath));
        }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            container.Register<IUserMapper, ConfigUserMapper>();
            container.Register<BooksRepository>();
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            var formsAuthConfiguration = new FormsAuthenticationConfiguration
            {
                RedirectUrl = "~/login",
                UserMapper = container.Resolve<IUserMapper>(),
            };

            FormsAuthentication.Enable(pipelines, formsAuthConfiguration);
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory(".well-known", @".well-known"));
            base.ConfigureConventions(nancyConventions);
        }
    }
}