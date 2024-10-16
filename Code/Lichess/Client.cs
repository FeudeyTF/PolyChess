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

        public async Task<string> SendRequestAsync(params object[] path)
            => await HttpClient.GetStringAsync(LICHESS_API_URL + string.Join("/", path));

        public async Task<Stream> GetFileAsync(params object[] path)
            => await HttpClient.GetStreamAsync(LICHESS_API_URL + string.Join("/", path));

        public async Task<string> GetFileStringAsync(params object[] path)
            => await new StreamReader(await HttpClient.GetStreamAsync(LICHESS_API_URL + string.Join("/", path))).ReadToEndAsync();

        public List<TValue> ParseNDJsonObject<TValue>(string str)
        {
            List<TValue> result = [];
            foreach (var entry in str.Split('\n'))
            {
                var obj = JsonConvert.DeserializeObject<TValue>(entry);
                if (obj != null)
                    result.Add(obj);
            }
            return result;
        }

        public async Task<List<TValue>> GetNDJsonObject<TValue>(StreamReader reader)
            => ParseNDJsonObject<TValue>(await reader.ReadToEndAsync());

        public async Task<List<TValue>> GetNDJsonObject<TValue>(params object[] path)
            => ParseNDJsonObject<TValue>(await GetFileStringAsync(path));

        public async Task<TValue?> GetJsonObject<TValue>(params object[] path)
            => JsonConvert.DeserializeObject<TValue>(await SendRequestAsync(path));

        public async Task<TValue?> GetJsonObjectFromFile<TValue>(params object[] path)
            => JsonConvert.DeserializeObject<TValue>(await GetFileStringAsync(path));
    }
}