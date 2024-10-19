using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Types.Arena
{
    public class Variant
    {
        public string Key = string.Empty;

        [JsonProperty("_short")]
        public string Short = string.Empty;

        public string Name = string.Empty;
    }
}
