using PolyChess.LichessAPI.Types.Puzzles;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PolyChess.LichessAPI.Converters
{
    internal class ThemeTypeConverter : JsonConverter<ThemeType>
    {
        public override ThemeType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (!string.IsNullOrEmpty(value) && Enum.TryParse<ThemeType>(value, true, out var result))
                return result;
            return ThemeType.Default;
        }

        public override void Write(Utf8JsonWriter writer, ThemeType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString().ToLower());
        }

        public override ThemeType ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Read(ref reader, typeToConvert, options);

        public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] ThemeType value, JsonSerializerOptions options)
            => Write(writer, value, options);
    }
}
