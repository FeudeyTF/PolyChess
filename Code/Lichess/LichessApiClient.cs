using Newtonsoft.Json;
using PolyChessTGBot.Lichess.Types;

namespace PolyChessTGBot.Lichess
{
    public class LichessApiClient
    {
        public const string LICHESS_API_URL = "https://lichess.org/api/";

        private HttpClient HttpClient { get; }

        public LichessApiClient()
        {
            HttpClient = new();
        }

        public async Task<LichessUser?> GetUserAsync(string username)
        {
            return await ResolveJSONObject<LichessUser>("user", username);
        }

        public async Task<List<Team>> GetUserTeamsAsync(string username)
        {
            var teams = await ResolveJSONObject<List<Team>>("team", "of", username);
            if (teams == null)
                return [];
            return teams;
        }

        public async Task<Team?> GetTeamAsync(string id)
        {
            return await ResolveJSONObject<Team>("team", id);
        }

        public async Task<List<RatingHistoryEntry>?> GetRatingHistory(string name)
        {
            return await ResolveJSONObject<List<RatingHistoryEntry>>("user", name, "rating-history");
        }

        public async Task<TValue?> ResolveJSONObject<TValue>(params string[] path)
        {
            return JsonConvert.DeserializeObject<TValue>(await SendRequestAsync(path));
        }

        public async Task<string> SendRequestAsync(params string[] path)
            => await HttpClient.GetStringAsync(LICHESS_API_URL + string.Join("/", path));
    }
}
