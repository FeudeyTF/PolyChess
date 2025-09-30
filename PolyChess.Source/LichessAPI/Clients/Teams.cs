using LichessAPI.Types;

namespace LichessAPI.Clients
{
    public partial class LichessClient : LichessApiClient
    {
        public async Task<List<Team>> GetUserTeamsAsync(string username)
        {
            var teams = await GetJsonObject<List<Team>>(HttpMethod.Get, "team", "of", username);
            if (teams == null)
                return [];
            return teams;
        }

        public async Task<Team?> GetTeamAsync(string id)
        {
            return await GetJsonObject<Team>(HttpMethod.Get, "team", id);
        }
    }
}
