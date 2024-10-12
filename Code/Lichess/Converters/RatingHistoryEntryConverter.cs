using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PolyChessTGBot.Lichess.Types;

namespace PolyChessTGBot.Lichess.Converters
{
    internal class RatingHistoryEntryConverter : JsonConverter<RatingHistoryEntryPoint>
    {
        public override RatingHistoryEntryPoint? ReadJson(JsonReader reader, Type objectType, RatingHistoryEntryPoint? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var array = JsonConvert.DeserializeObject<int[]>(JObject.ReadFrom(reader).ToString());
            if (array != null && array.Length == 4)
                return new()
                {
                    Date = new DateTime(array[0], array[1], array[2]),
                    Score = array[3]
                };
            else 
                return null;
        }

        public override void WriteJson(JsonWriter writer, RatingHistoryEntryPoint? value, JsonSerializer serializer)
        {
            if(value != null)
                writer.WriteValue($"[{value.Date.Year}, {value.Date.Month}, {value.Date.Day}, {value.Score}]");
        }
    }
}
