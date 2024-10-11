using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Converters
{
    internal class LichessDateTimeConverter : JsonConverter<DateTime>
    {
        public static DateTime UnixStart
            = new DateTime(1970, 1, 1) + TimeZoneInfo.Local.BaseUtcOffset;

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Second * 1000);
        }

        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return default;
            return UnixStart.AddSeconds((long)reader.Value / 1000);
        }
    }
}

