using Newtonsoft.Json;
using PolyChessTGBot.Lichess.Converters;

namespace PolyChessTGBot.Lichess.Types.Arena
{
    [JsonConverter(typeof(ArenaTournamentsTeamBattleConverter))]
    public class TeamBattle
    {
        public Dictionary<string, List<string>> Teams = [];

        [JsonProperty("nbLeaders")]
        public long LeadersNumber;
    }
}
