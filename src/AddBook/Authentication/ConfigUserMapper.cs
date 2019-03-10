using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System;

namespace AddBook.Authentication
{
    public class ConfigUserMapper : IUserMapper
    {
        private readonly Configuration configuration;

        public ConfigUserMapper(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            return new ConfigUserIdentity(configuration.SoleUser.Username);
        }
    }
}