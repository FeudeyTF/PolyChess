using PolyChess.LichessAPI.Converters;
using System.Text.Json.Serialization;

namespace PolyChess.LichessAPI.Types.Arena
{
    [JsonConverter(typeof(TeamBattleConverter))]
    public class TeamBattle
    {
        public Dictionary<string, List<string>>? Teams;

        public int LeadersNumber;
    }
}
