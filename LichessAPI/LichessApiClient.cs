using LichessAPI.Converters;
using System.Text.Json;

namespace LichessAPI
{
    public abstract class LichessApiClient
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

        protected async Task<string?> SendRequestAsync(params object[] path)
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

        protected async Task<string?> CreatePostRequestAsync(HttpContent? content, params object[] path)
        {
            Console.WriteLine(LICHESS_API_URL + string.Join("/", path));
            Console.WriteLine(content);
            using var response = await HttpClient.PostAsync(LICHESS_API_URL + string.Join("/", path), content);
            return await response.Content.ReadAsStringAsync();
        }

        protected async Task<string?> CreateGetRequestAsync(params object[] path)
        {
            using var response = await HttpClient.GetAsync(LICHESS_API_URL + string.Join("/", path));
            return await response.Content.ReadAsStringAsync();
        }

        internal async Task<Stream> GetFileAsync(params object[] path)
            => await HttpClient.GetStreamAsync(LICHESS_API_URL + string.Join("/", path));

        internal async Task<string> GetFileStringAsync(params object[] path)
            => await new StreamReader(await HttpClient.GetStreamAsync(LICHESS_API_URL + string.Join("/", path))).ReadToEndAsync();

        internal async Task<List<TValue>> GetNDJsonObject<TValue>(StreamReader reader)
            => Utils.ParseNDJsonObject<TValue>(await reader.ReadToEndAsync());

        internal async Task<List<TValue>> GetNDJsonObject<TValue>(params object[] path)
            => Utils.ParseNDJsonObject<TValue>(await GetFileStringAsync(path));

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