using System;
using System.IO;
using System.Linq;

namespace AddBook
{
    public sealed class Configuration
    {
        public string AccessToken { get; }

        public SoleUser SoleUser { get; }

        public Configuration(string accessToken, SoleUser soleUser)
        {
            AccessToken = accessToken;
            SoleUser = soleUser;
        }

        /// <summary>
        /// Populates a configuration object from a text file.
        /// Each line in the file is of the format "PropertyName:Value" (without the quotes)
        /// and ignores any lines starting with #
        /// </summary>
        /// <param name="filePath">File path to load from, if the file does not exist no work is done</param>
        /// <param name="configurationObject">Object to populate</param>
        public static Configuration Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                var configEntries = File.ReadAllLines(filePath)
                                        .Where(l => !l.StartsWith("#"))
                                        .Select(l => l.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries))
                                        .Where(c => c.Length == 2);

                var accessToken = configEntries.FirstOrDefault(c => c[0] == "AccessToken")?[1];

                var soleUserEntry = configEntries.FirstOrDefault(c => c[0] == "SoleUser");
                var soleUser = soleUserEntry == null ? null : new SoleUser(soleUserEntry[1]);

                return new Configuration(accessToken, soleUser);
            }
            throw new Exception($"Configuration file not found at '{filePath}'");
        }
    }

    public sealed class SoleUser
    {
        public string Username { get; }

        public string Password { get; }

        public SoleUser(string rawInfos)
        {
            var infos = rawInfos.Split('/');
            Username = infos[0];
            Password = infos[1];
        }
    }
}