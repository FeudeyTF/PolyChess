using LichessAPI.Converters;
using System.Text.Json;

namespace LichessAPI.Client
{
    public partial class LichessApiClient
    {
        public const string LICHESS_API_URL = "https://lichess.org/api/";

        private HttpClient HttpClient { get; }

        private readonly JsonSerializerOptions SerializerOptions;

        public LichessApiClient()
        {
            HttpClient = new();
            SerializerOptions = new()
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true,
            };
            SerializerOptions.Converters.Add(new LichessDateTimeConverter());
            SerializerOptions.Converters.Add(new TeamBattleConverter());
        }

        private async Task<string?> SendRequestAsync(params object[] path)
        {
            try
            {
                return await HttpClient.GetStringAsync(LICHESS_API_URL + string.Join("/", path));
            }
            catch
            {
                return null;
            }
        }

        internal async Task<Stream> GetFileAsync(params object[] path)
            => await HttpClient.GetStreamAsync(LICHESS_API_URL + string.Join("/", path));

        internal async Task<string> GetFileStringAsync(params object[] path)
            => await new StreamReader(await HttpClient.GetStreamAsync(LICHESS_API_URL + string.Join("/", path))).ReadToEndAsync();

        internal static List<TValue> ParseNDJsonObject<TValue>(string str)
        {
            List<TValue> result = [];
            foreach (var entry in str.Split('\n'))
            {
                var obj = JsonSerializer.Deserialize<TValue>(entry);
                if (obj != null)
                    result.Add(obj);
            }
            return result;
        }

        internal async Task<List<TValue>> GetNDJsonObject<TValue>(StreamReader reader)
            => ParseNDJsonObject<TValue>(await reader.ReadToEndAsync());

        internal async Task<List<TValue>> GetNDJsonObject<TValue>(params object[] path)
            => ParseNDJsonObject<TValue>(await GetFileStringAsync(path));

        internal async Task<TValue?> GetJsonObject<TValue>(params object[] path)
        {
            var response = await SendRequestAsync(path);
            if (response != null)
                return JsonSerializer.Deserialize<TValue>(response, SerializerOptions);
            else
                return default;
        }

        internal async Task<TValue?> GetJsonObjectFromFile<TValue>(params object[] path)
            => JsonSerializer.Deserialize<TValue>(await GetFileStringAsync(path));
    }
}