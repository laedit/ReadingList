using System;
using System.IO;
using System.Linq;

namespace AddBook
{
    /// <summary>
    /// Populates a confinguration object from a text file.
    /// Each line in the file is of the format "PropertyName:Value" (without the quotes)
    /// and ignores any lines starting with #
    /// </summary>
    public static class ConfigLoader
    {
        /// <summary>
        /// Populates a confinguration object from a text file.
        /// Each line in the file is of the format "PropertyName:Value" (without the quotes)
        /// and ignores any lines starting with #
        /// </summary>
        /// <param name="filename">Filename to load from, if the file does not exist no work is done</param>
        /// <param name="configurationObject">Object to populate</param>
        public static void Load(string filename)
        {
            if (File.Exists(filename))
            {
                var configEntries = File.ReadAllLines(filename)
                                        .Where(l => !l.StartsWith("#"))
                                        .Select(l => l.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries))
                                        .Where(c => c.Length == 2);

                var appVeyorApiKeyEntry = configEntries.FirstOrDefault(c => c[0] == "AppVeyorApiKey");
                Configuration.AppVeyorApiKey = appVeyorApiKeyEntry == null ? null : appVeyorApiKeyEntry[1];

                var soleUserEntry = configEntries.FirstOrDefault(c => c[0] == "SoleUser");
                Configuration.SoleUser = soleUserEntry == null ? null : new SoleUser(soleUserEntry[1]);
            }
        }
    }
}