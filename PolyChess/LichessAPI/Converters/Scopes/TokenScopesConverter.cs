using PolyChess.LichessAPI.Types.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolyChess.LichessAPI.Converters.Scopes
{
    internal class TokenScopesConverter : JsonConverter<List<TokenScope>>
    {
        public override List<TokenScope>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (!string.IsNullOrEmpty(str))
                {
                    var tokens = str.Split(',');
                    List<TokenScope> result = [];
                    foreach (var token in tokens)
                        if (TokenScope.TryParse(token, out var scope))
                            result.Add(scope);
                    return result;
                }
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, List<TokenScope> value, JsonSerializerOptions options)
        {
            if (value.Count > 0)
                writer.WriteStringValue(string.Join(',', value));
            else
                writer.WriteStringValue("");
        }
    }
}
