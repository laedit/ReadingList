using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System;
using System.Collections.Generic;

namespace AddBook
{
    public class ConfigUserMapper : IUserMapper
    {
        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            if(Configuration.SoleUser != null)
            {
                return new ConfigUserIdentity(Configuration.SoleUser.Username);
            }
            return null;
        }
    }

    public class ConfigUserIdentity : IUserIdentity
    {
        public IEnumerable<string> Claims { get; private set; }

        public string UserName { get; private set; }

        public ConfigUserIdentity(string userName)
        {
            UserName = userName;
        }
    }
}