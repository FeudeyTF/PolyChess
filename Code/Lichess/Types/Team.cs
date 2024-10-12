using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Types
{
    public class Team
    {
        [JsonProperty("id")]
        public string ID = string.Empty;

        [JsonProperty("name")]
        public string Name = string.Empty;

        [JsonProperty("description")]
        public string Description = string.Empty;

        [JsonProperty("open")]
        public bool Open;

        [JsonProperty("leader")]
        public LightUser Leader = new();

        [JsonProperty("nbMembers")]
        public int MembersCount;

        [JsonProperty("flair")]
        public string Flair = string.Empty;

        [JsonProperty("leaders")]
        public List<LightUser> Leaders = [];

        [JsonProperty("joined")]
        public bool Joined;

        [JsonProperty("requested")]
        public bool Requested;
    }
}