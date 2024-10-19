using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Types.Arena
{

    public class TeamBattle
    {
        public string[] Teams = [];

        [JsonProperty("nbLeaders")]
        public int LeadersNumber;
    }
}
