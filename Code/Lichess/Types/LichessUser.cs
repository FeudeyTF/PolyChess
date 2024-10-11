using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PolyChessTGBot.Lichess.Converters;

namespace PolyChessTGBot.Lichess.Types
{
    public class LichessUser
    {
        [JsonProperty("id")]
        public string ID = string.Empty;

        [JsonProperty("username")]
        public string Username = string.Empty;

        [JsonProperty("perfs")]
        public Dictionary<string, Perfomance> Perfomance = [];

        [JsonProperty("createdAt")]
        [JsonConverter(typeof(LichessDateTimeConverter))]
        public DateTime CreatedDate;

        [JsonProperty("seenAt")]
        [JsonConverter(typeof(LichessDateTimeConverter))]
        public DateTime LastSeenDate;

        [JsonProperty("playtime")]
        public Dictionary<string, int> Playtime = [];

        [JsonProperty("url")]
        public string URL = string.Empty;

        [JsonProperty("count")]
        public Dictionary<string, int> Count = [];

        [JsonProperty("followable")]
        public bool Followable;

        [JsonProperty("following")]
        public bool Following;

        [JsonProperty("blocking")]
        public bool Blocking;

        [JsonProperty("followsYou")]
        public bool FollowsYou;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}