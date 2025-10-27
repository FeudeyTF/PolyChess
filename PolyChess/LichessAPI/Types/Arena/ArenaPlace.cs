using System.Text.Json.Serialization;

namespace PolyChess.LichessAPI.Types.Arena
{
    public class ArenaPlace
    {
        public string Name = string.Empty;

        public int Rank;

        public int Rating;

        public int Score;

        [JsonPropertyName("nb")]
        public ArenaNumbers Numbers = new();

        public int Performance;

        public string Flair = string.Empty;
    }
}
