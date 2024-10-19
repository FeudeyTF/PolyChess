using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Converters
{
    public class LichessDateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly DateTime UnixStart
            = new DateTime(1970, 1, 1) + TimeZoneInfo.Local.BaseUtcOffset;

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Second * 1000);
        }

        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
                return default;
            if (reader.Value is long ticks)
                return UnixStart.AddSeconds(ticks / 1000);
            else
                return (DateTime)reader.Value;
        }
    }
}