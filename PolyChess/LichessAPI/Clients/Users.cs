using PolyChess.LichessAPI.Types;
using PolyChess.LichessAPI.Types.Streams;

namespace PolyChess.LichessAPI.Clients
{
    public partial class LichessClient : LichessApiClient
    {
        public async Task<User?> GetUserAsync(string username)
        {
            var user = await GetJsonObject<User>(HttpMethod.Get, "user", username);
            if (user != null && !string.IsNullOrEmpty(user.URL))
                return user;
            return null;
        }

        public async Task<List<RatingHistoryEntry>?> GetRatingHistory(string name)
        {
            return await GetJsonObject<List<RatingHistoryEntry>>(HttpMethod.Get, "user", name, "rating-history");
        }

        public async Task<List<Status>?> GetPlayersStatus(params string[] names)
        {
            return await GetJsonObject<List<Status>>(HttpMethod.Get, "users", "status?ids=", string.Join(",", names));
        }

        public async Task<List<StreamInfo>?> GetLiveStreams()
        {
            return await GetJsonObject<List<StreamInfo>>(HttpMethod.Get, "streamer", "live");
        }
    }
}
