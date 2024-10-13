using PolyChessTGBot.Lichess.Types;

namespace PolyChessTGBot.Lichess
{
    public partial class LichessApiClient
    {

        public async Task<List<Team>> GetUserTeamsAsync(string username)
        {
            var teams = await GetJsonObject<List<Team>>("team", "of", username);
            if (teams == null)
                return [];
            return teams;
        }
    }
}
