using LichessAPI.Types;

namespace LichessAPI.Clients.Authorized
{
    public partial class LichessAuthorizedClient : LichessApiClient
    {
        public readonly string QAuthToken;

        public readonly TokenType TokenType;

        public LichessAuthorizedClient(string token, TokenType tokenType = TokenType.Bearer)
        {
            QAuthToken = token;
            TokenType = tokenType;
        }

        private async Task<string?> GetQAuthPostRequestContent(params string[] path)
            => await (await SendQAuthRequest(HttpMethod.Post, path, [])).Content.ReadAsStringAsync();

        private async Task<string?> GetQAuthGetRequestContent(params string[] path)
            => await (await SendQAuthRequest(HttpMethod.Get, path, [])).Content.ReadAsStringAsync();

        private async Task<HttpResponseMessage> SendQAuthPostRequest(params string[] path)
            => await SendQAuthRequest(HttpMethod.Post, path, []);

        private async Task<HttpResponseMessage> SendQAuthGetRequest(params string[] path)
            => await SendQAuthRequest(HttpMethod.Get, path, []);

        private async Task<HttpResponseMessage> SendQAuthPostRequest(string[] path, params (string name, string value)[] headers)
            => await SendQAuthRequest(HttpMethod.Post, path, headers);

        private async Task<HttpResponseMessage> SendQAuthGetRequest(string[] path, params (string name, string value)[] headers)
            => await SendQAuthRequest(HttpMethod.Get, path, headers);

        private async Task<HttpResponseMessage> SendQAuthRequest(HttpMethod method, string[] path, params (string name, string value)[] headers)
        {
            HttpRequestMessage msg = new(method, LICHESS_API_URL + string.Join("/", path));
            msg.Headers.Add("Authorization", $"{TokenType} {QAuthToken}");
            foreach (var (name, value) in headers)
                msg.Headers.Add(name, value);
            return await SendRequestMessage(msg);
        }

        private async Task<TValue?> GetAuthJsonObject<TValue>(HttpMethod method, params string[] path)
            => await GetAuthJsonObject<TValue>(method, path, []);

        private async Task<TValue?> GetAuthJsonObject<TValue>(HttpMethod method, string[] path, params (string name, string value)[] headers)
        {
            var respone = await SendQAuthRequest(method, path, headers);
            return await GetJsonObject<TValue>(respone);
        }

        public async Task<User?> GetUserInfo()
        {
            return await GetAuthJsonObject<User>(HttpMethod.Get, "account");
        }
    }
}
