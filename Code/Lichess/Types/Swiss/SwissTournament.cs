using Newtonsoft.Json;

namespace PolyChessTGBot.Lichess.Types.Swiss
{
    public class SwissTournament
    {
        public Clock Clock = new();

        public string CreatedBy = string.Empty;

        public string ID = string.Empty;

        public string Name = string.Empty;

        [JsonProperty("nbOngoing")]
        public int OngoingNumber;

        [JsonProperty("nbPlayers")]
        public int PlayersNumber;

        [JsonProperty("nbRounds")]
        public int RoundsNumber;

        public bool Rated;

        public int Round;

        [JsonProperty("startsAt")]
        public DateTime startsAt;

        public SwissStats Stats = new();

        public string Status = string.Empty;

        public string Variant = string.Empty;

        public Position Position = new();
    }
}