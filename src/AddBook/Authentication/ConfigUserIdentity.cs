using Nancy.Security;
using System.Collections.Generic;

namespace AddBook.Authentication
{
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