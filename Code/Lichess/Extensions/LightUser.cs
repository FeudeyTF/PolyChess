using PolyChessTGBot.Lichess.Types;

namespace PolyChessTGBot.Lichess.Extensions
{
    public static partial class Extensions
    {
        public static async Task<LichessUser?> GetFullUser(this LightUser user, LichessApiClient client)
        {
            return await client.GetUserAsync(user.Name);
        }
    }
}
