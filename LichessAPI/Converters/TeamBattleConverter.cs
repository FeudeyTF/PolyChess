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
                if (parsedObject.TryGetValue("teams", out var value))
                {
                    result.Teams = [];
                    try
                    {
                        result.Teams = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(value, options);
                    }
                    catch
                    {
                        var list = JsonSerializer.Deserialize<List<string>>(value, options);
                        if (result.Teams != null && list != null)
                            if(list.Count == 1)
                                result.Teams.Add(list[0], []);
                            else if(list.Count > 1)
                                result.Teams.Add(list[0], list[1..]);
                    }
                }
                result.LeadersNumber = JsonSerializer.Deserialize<int>(parsedObject["nbLeaders"], options);
            }
            return result;
        }

        public override void Write(Utf8JsonWriter writer, TeamBattle value, JsonSerializerOptions options)
        {

        }
    }
}