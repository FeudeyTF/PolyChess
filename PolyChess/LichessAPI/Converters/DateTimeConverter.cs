using System.Text.Json;
using System.Text.Json.Serialization;

namespace LichessAPI.Converters
{
    internal class LichessDateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly DateTime UnixStart
            = new DateTime(1970, 1, 1) + TimeZoneInfo.Local.BaseUtcOffset;

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String && reader.TryGetDateTime(out DateTime date))
                return date;
            else if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long longNumber))
                return UnixStart.AddSeconds(longNumber / 1000);
            else
                return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime date, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((date.ToUniversalTime() - UnixStart).TotalSeconds * 1000);
        }
    }
}