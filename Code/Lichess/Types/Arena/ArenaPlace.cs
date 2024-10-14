using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Types.Arena
{
    public class ArenaPlace
    {
        public string Name = string.Empty;

        public int Rank;

        public int Rating;

        public int Score;

        [JsonProperty("nb")]
        public ArenaNumbers Numbers = new();

        public int Performance;

        public string Flair = string.Empty;
    }
}
