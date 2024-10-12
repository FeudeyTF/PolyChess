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
            return JsonConvert.DeserializeObject<LichessUser>(await SendRequestAsync("user", username));
        }

        public async Task<List<Team>> GetUserTeamsAsync(string username)
        {
            var teams = JsonConvert.DeserializeObject<List<Team>>(await SendRequestAsync("team", "of", username));
            if (teams == null)
                return [];
            return teams;
        }

        public async Task<Team?> GetTeamAsync(string id)
        {
            return JsonConvert.DeserializeObject<Team>(await SendRequestAsync("team", id));
        }

        public async Task<string> SendRequestAsync(params string[] path)
            => await HttpClient.GetStringAsync(LICHESS_API_URL + string.Join("/", path));
    }
}
