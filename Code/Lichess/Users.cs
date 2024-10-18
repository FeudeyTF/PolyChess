using PolyChessTGBot.Lichess.Types;
using PolyChessTGBot.Lichess.Types.Streams;

namespace PolyChessTGBot.Lichess
{
    public partial class LichessApiClient
    {
        public async Task<User?> GetUserAsync(string username)
        {
            return await GetJsonObject<User>("user", username);
        }

        public async Task<List<RatingHistoryEntry>?> GetRatingHistory(string name)
        {
            return await GetJsonObject<List<RatingHistoryEntry>>("user", name, "rating-history");
        }

        public async Task<List<Status>?> GetPlayersStatus(params string[] names)
        {
            return await GetJsonObject<List<Status>>("users", "status?ids=", string.Join(",", names));
        }

        public async Task<List<StreamInfo>?> GetLiveStreams()
        {
            return await GetJsonObject<List<StreamInfo>>("streamer", "live");
        }
    }
}
