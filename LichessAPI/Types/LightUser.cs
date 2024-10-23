using LichessAPI.Client;

namespace LichessAPI.Types
{
    public class LightUser
    {
        public string ID = string.Empty;

        public string Name = string.Empty;

        public string Flair = string.Empty;

        public async Task<User?> GetFullUser(LichessApiClient client)
            => await client.GetUserAsync(Name);
    }
}
