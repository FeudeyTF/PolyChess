using System.Text.Json.Serialization;

namespace PolyChess.LichessAPI.Types.Arena
{
    public class Variant
    {
        public string Key = string.Empty;

        [JsonPropertyName("_short")]
        public string Short = string.Empty;

        public string Name = string.Empty;
    }
}
