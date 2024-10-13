namespace PolyChessTGBot.Lichess.Types
{
    public class LightUser
    {
        public string ID = string.Empty;

        public string Name = string.Empty;

        public string Flair = string.Empty;

        public async Task<LichessUser?> GetFullUser(LichessApiClient client)
            => await client.GetUserAsync(Name);
    }
}
