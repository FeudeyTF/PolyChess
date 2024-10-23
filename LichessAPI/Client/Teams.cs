using LichessAPI.Types;

namespace LichessAPI.Client
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

        public async Task<Team?> GetTeamAsync(string id)
        {
            return await GetJsonObject<Team>("team", id);
        }
    }
}
