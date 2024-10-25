using LichessAPI.Types;
using LichessAPI.Types.Streams;

namespace LichessAPI.Clients
{
    public partial class LichessClient : LichessApiClient
    {
        public async Task<User?> GetUserAsync(string username)
        {
            return await GetJsonObject<User>(HttpMethod.Get, "user", username);
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
