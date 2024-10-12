using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Types
{
    public class LightUser
    {
        [JsonProperty("id")]
        public string ID = string.Empty;

        [JsonProperty("name")]
        public string Name = string.Empty;

        [JsonProperty("flair")]
        public string Flair = string.Empty;

        public async Task<LichessUser?> GetFullUser(LichessApiClient client)
        {
            return await client.GetUserAsync(ID);
        }
    }
}
