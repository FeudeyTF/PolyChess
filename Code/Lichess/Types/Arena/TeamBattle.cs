using Newtonsoft.Json;
using PolyChessTGBot.Lichess.Converters;

namespace PolyChessTGBot.Lichess.Types.Arena
{
    [JsonConverter(typeof(TeamBattleConverter))]
    public class TeamBattle
    {
        public Dictionary<string, List<string>> Teams = [];

        public int LeadersNumber;
    }
}
