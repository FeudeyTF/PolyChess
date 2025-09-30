using Newtonsoft.Json;

namespace PolyChess.Core.Configuration
{
    internal abstract class ConfigFile : IConfig
    {
        [JsonIgnore]
        public abstract string Path { get; }

        public static TValue Load<TValue>() where TValue : ConfigFile, new()
        {
            TValue config = new();
            if (!File.Exists(config.Path))
                config.Save();
            var parsedConfig = JsonConvert.DeserializeObject<TValue>(File.ReadAllText(config.Path));
            if (parsedConfig != null)
                return parsedConfig;
            return config;
        }

        public void Save()
        {
            File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
