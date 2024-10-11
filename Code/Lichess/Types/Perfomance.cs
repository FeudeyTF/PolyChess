using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Types
{
    public class Perfomance
    {
        [JsonProperty("games")]
        public int Games;

        [JsonProperty("rating")]
        public int Rating;

        [JsonProperty("rd")]
        public int Rd;

        [JsonProperty("prog")]
        public int Prog;

        [JsonProperty("prov")]
        public bool Prov;
    }
}
