using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PolyChessTGBot.Extensions;
using PolyChessTGBot.Lichess.Types.Arena;

namespace PolyChessTGBot.Lichess.Converters
{
    internal class TeamBattleConverter : JsonConverter<TeamBattle>
    {
        public override TeamBattle? ReadJson(JsonReader reader, Type objectType, TeamBattle? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var array = JsonConvert.DeserializeObject<Dictionary<string, object>>(JObject.ReadFrom(reader).ToString());

            if (array != null)
            {
                var teams = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(array["teams"].Stringify());
                var leaderNumber = array["nbLeaders"].ToString();
                return new()
                {
                    LeadersNumber = leaderNumber == null ? 0 : int.Parse(leaderNumber),
                    Teams = teams ?? ([])
                };
            }
            else
                return null;
        }

        public override void WriteJson(JsonWriter writer, TeamBattle? value, JsonSerializer serializer)
        {
        }
    }
}
