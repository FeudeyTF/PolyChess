using PolyChess.LichessAPI.Converters.Scopes;
using System.Text.Json.Serialization;

namespace PolyChess.LichessAPI.Types.Tokens
{
    public class TokenInfo
    {
        public string UserID = string.Empty;

        [JsonConverter(typeof(TokenScopesConverter))]
        public List<TokenScope> Scopes = [];

        public DateTime? Expires;
    }
}
