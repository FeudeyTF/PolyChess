using Newtonsoft.Json;

namespace PolyChessTGBot
{
    public class ConfigFile
    {
        public string BotToken = string.Empty;

        public long QuestionChannel;

        public string LogsFolder = "Logs";

        public string DatabasePath = "data.sqlite";

        public string BotAuthor = string.Empty;

        public List<int> DebugChats = [];

        public List<string> TopPlayers = [];

        public List<string> PolytechTeams = [];

        public string TournamentScoresDate = string.Empty;

        public bool ShowApiResponseLogs = false;

        public SocketSettings Socket = new();

        public int[] SkippingApiRequestErrors = [];

        public long[] Admins = [];

        public static ConfigFile Load(string name)
        {
            ConfigFile emptyConfig = new();
            var path = emptyConfig.Save(name);
            var parsedConfig = JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(path));
            if (parsedConfig != null)
                return parsedConfig;
            return emptyConfig;
        }

        public string Save(string name, bool rewrite = false)
        {
            string configFolder = Path.Combine(Environment.CurrentDirectory, "Configs");
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);
            string path = Path.Combine(configFolder, name + ".json");
            if (!File.Exists(path) || rewrite)
                File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
            return path;
        }
    }

    public class SocketSettings
    {
        public bool StartSocketServer;

        public int Port = 8081;
    }
}