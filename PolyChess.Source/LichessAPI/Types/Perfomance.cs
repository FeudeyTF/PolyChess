using System.Text.Json.Serialization;

namespace LichessAPI.Types
{
    public class Perfomance
    {
        public int Games;

        public int Rating;

        [JsonPropertyName("rd")]
        public int RatingsDeviation;

        [JsonPropertyName("prog")]
        public int Prog;

        [JsonPropertyName("prov")]
        public bool Prov;
    }
}
