using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess
{
    public partial class LichessApiClient
    {
        public const string LICHESS_API_URL = "https://lichess.org/api/";

        private HttpClient HttpClient { get; }

        public LichessApiClient()
        {
            HttpClient = new();
        }

        public async Task<TValue?> GetJsonObject<TValue>(params string[] path)
            => JsonConvert.DeserializeObject<TValue>(await SendRequestAsync(path));

        public async Task<string> SendRequestAsync(params string[] path)
            => await HttpClient.GetStringAsync(LICHESS_API_URL + string.Join("/", path));
    }
}