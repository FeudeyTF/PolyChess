using Newtonsoft.Json;
using PolyChessTGBot.Lichess.Converters;

namespace PolyChessTGBot.Lichess.Types
{
    public class User
    {
        public string ID = string.Empty;

        public string Username = string.Empty;

        public Dictionary<string, Perfomance> Perfomance = [];

        [JsonProperty("createdAt")]
        [JsonConverter(typeof(LichessDateTimeConverter))]
        public DateTime RegisterDate;

        [JsonProperty("seenAt")]
        [JsonConverter(typeof(LichessDateTimeConverter))]
        public DateTime LastSeenDate;

        public Dictionary<string, int> Playtime = [];

        public string URL = string.Empty;

        public Dictionary<string, int> Count = [];

        public bool Followable;

        public bool Following;

        public bool Blocking;

        public bool FollowsYou;

        public async Task<Status?> GetStatus(LichessApiClient client)
        {
            var statuses = await client.GetPlayersStatus(Username);
            if (statuses != null && statuses.Count > 0)
                return statuses.First();
            else
                return null;
        }
    }
}