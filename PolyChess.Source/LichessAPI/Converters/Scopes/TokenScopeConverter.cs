using LichessAPI.Types.Tokens;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LichessAPI.Converters.Scopes
{
    internal class TokenScopeConverter : JsonConverter<TokenScope>
    {
        public override TokenScope Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            if (!string.IsNullOrEmpty(str) && TokenScope.TryParse(str, out var tokenScope))
                return tokenScope;
            return default;
        }

        public override void Write(Utf8JsonWriter writer, TokenScope value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
