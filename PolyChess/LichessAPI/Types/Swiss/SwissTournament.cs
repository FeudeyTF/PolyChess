using System.Text.Json.Serialization;

namespace PolyChess.LichessAPI.Types.Swiss
{
    public class SwissTournament
    {
        public Clock Clock = new();

        public string CreatedBy = string.Empty;

        public string ID = string.Empty;

        public string Name = string.Empty;

        [JsonPropertyName("nbOngoing")]
        public int OngoingNumber;

        [JsonPropertyName("nbPlayers")]
        public int PlayersNumber;

        [JsonPropertyName("nbRounds")]
        public int RoundsNumber;

        public bool Rated;

        public int Round;

        [JsonPropertyName("startsAt")]
        public DateTime Started;

        public SwissStats Stats = new();

        public string Status = string.Empty;

        public string Variant = string.Empty;

        public Position Position = new();
    }
}