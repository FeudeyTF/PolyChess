using Newtonsoft.Json;

namespace PolyChessTGBot
{
    public class ConfigFile
    {
        public string BotToken = "";

         public static ConfigFile Load(string name)
        {
            string path = Path.Combine(Environment.CurrentDirectory, "Configs", name + ".json");

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