namespace AddBook
{
    public static class Configuration
    {
        public static string AppVeyorApiKey { get; set; }

        public static SoleUser SoleUser { get; set; }
    }

    public class SoleUser
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public SoleUser(string rawInfos)
        {
            var infos = rawInfos.Split('/');
            Username = infos[0];
            Password = infos[1];
        }
    }
}