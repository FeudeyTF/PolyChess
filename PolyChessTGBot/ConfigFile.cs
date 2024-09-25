using Newtonsoft.Json;

namespace PolyChessTGBot
{
    public class ConfigFile
    {
        public string BotToken = "";

        public long QuestionChannel;

        public string LogsFolder = "Logs";

        public string DatabasePath = "data.sqlite";

        public List<int> DebugChats = new();

        public static ConfigFile Load(string name)
        {
            string configFolder = Path.Combine(Environment.CurrentDirectory, "Configs");
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);
            string path = Path.Combine(configFolder, name + ".json");

            ConfigFile emptyConfig = new();

            if (!File.Exists(path))
                File.WriteAllText(path, JsonConvert.SerializeObject(emptyConfig, Formatting.Indented));

            var parsedConfig = JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(path));
            if(parsedConfig != null)
                return parsedConfig;
            return emptyConfig;
        }
    }
}