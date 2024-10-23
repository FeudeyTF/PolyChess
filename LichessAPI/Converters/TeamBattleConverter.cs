using LichessAPI.Types.Arena;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LichessAPI.Converters
{
    internal class TeamBattleConverter : JsonConverter<TeamBattle>
    {
        public override TeamBattle? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            TeamBattle result = new();

            var parsedObject = JsonDocument.ParseValue(ref reader).Deserialize<Dictionary<string, JsonElement>>(options);
            if (parsedObject != null)
            {
                result.Teams = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(parsedObject["teams"], options);
                result.LeadersNumber = JsonSerializer.Deserialize<int>(parsedObject["nbLeaders"], options);
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, TeamBattle value, JsonSerializerOptions options)
        {

        }
    }
}