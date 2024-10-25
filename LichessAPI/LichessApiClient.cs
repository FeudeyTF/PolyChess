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

        protected async Task<string> GetGetRequestContentAsync(params object[] path)
            => await (await SendGetRequestAsync(path, [])).Content.ReadAsStringAsync();

        protected async Task<string> GetPostRequestContentAsync(params object[] path)
            => await (await SendPostRequestAsync(path, [])).Content.ReadAsStringAsync();

        protected async Task<string> GetGetRequestContentAsync(object[] path, (string name, string value)[] headers)
            => await (await SendGetRequestAsync(path, headers)).Content.ReadAsStringAsync();

        protected async Task<string> GetPostRequestContentAsync(object[] path, (string name, string value)[] headers)
            => await (await SendPostRequestAsync(path, headers)).Content.ReadAsStringAsync();

        protected async Task<HttpResponseMessage> SendPostRequestAsync(object[] path) =>
            await SendPostRequestAsync(path, []);

        protected async Task<HttpResponseMessage> SendPostRequestAsync(object[] path, (string name, string value)[] headers)
            => await SendRequestMessage(HttpMethod.Post, path, headers);

        protected async Task<HttpResponseMessage> SendGetRequestAsync(params object[] path) =>
            await SendGetRequestAsync(path, []);

        protected async Task<HttpResponseMessage> SendGetRequestAsync(object[] path, (string name, string value)[] headers)
            => await SendRequestMessage(HttpMethod.Get, path, headers);

        protected async Task<HttpResponseMessage> SendRequestMessage(HttpMethod method, object[] path, (string name, string value)[] headers)
        {
            HttpRequestMessage msg = new(method, LICHESS_API_URL + string.Join("/", path));
            foreach (var (name, value) in headers)
                msg.Headers.Add(name, value);
            return await SendRequestMessage(msg);
        }

        protected async Task<HttpResponseMessage> SendRequestMessage(HttpRequestMessage message)
            => await HttpClient.SendAsync(message);

        internal async Task<Stream> GetFileAsync(params object[] path)
            => await HttpClient.GetStreamAsync(LICHESS_API_URL + string.Join("/", path));

        internal async Task<string> GetFileStringAsync(params object[] path)
            => await new StreamReader(await HttpClient.GetStreamAsync(LICHESS_API_URL + string.Join("/", path))).ReadToEndAsync();

        internal async Task<List<TValue>> GetNDJsonObject<TValue>(StreamReader reader)
            => Utils.ParseNDJsonObject<TValue>(await reader.ReadToEndAsync());

        internal async Task<List<TValue>> GetNDJsonObject<TValue>(params object[] path)
            => Utils.ParseNDJsonObject<TValue>(await GetFileStringAsync(path));

        internal async Task<TValue?> GetJsonObject<TValue>(HttpMethod method, params object[] path)
            => await GetJsonObject<TValue>(method, path, []);

        internal async Task<TValue?> GetJsonObject<TValue>(HttpMethod method, object[] path, (string name, string value)[] headers)
        {
            HttpRequestMessage msg = new(method, LICHESS_API_URL + string.Join("/", path));
            foreach (var (name, value) in headers)
                msg.Headers.Add(name, value);
            return await GetJsonObject<TValue>(msg);
        }

        internal async Task<TValue?> GetJsonObject<TValue>(HttpRequestMessage message)
            => await GetJsonObject<TValue>(await SendRequestMessage(message));

        internal async Task<TValue?> GetJsonObject<TValue>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return JsonSerializer.Deserialize<TValue>(await response.Content.ReadAsStringAsync(), SerializerOptions);
            else
                return default;
        }

        internal async Task<TValue?> GetJsonObjectFromFile<TValue>(params object[] path)
            => JsonSerializer.Deserialize<TValue>(await GetFileStringAsync(path));
    }
}