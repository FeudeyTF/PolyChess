using Newtonsoft.Json;
using PolyChessTGBot.Lichess.Converters;

namespace PolyChessTGBot.Lichess.Types.Arena
{
    public class ArenaTournament
    {
        [JsonProperty("nbPlayers")]
        public int PlayersNumber;

        public object[] Duels = [];

        public bool IsFinished;

        public ArenaPlace[] Podium = [];

        public bool PairingsClosed;

        public Stats Stats = new();

        public Standing Standing = new();

        public string ID = string.Empty;

        public string CreatedBy = string.Empty;

        [JsonProperty("startsAt")]
        public DateTime Started;

        public string System = string.Empty;

        public string FullName = string.Empty;

        public int Minutes;

        [JsonProperty("perf")]
        public ArenaPerfomance Perfomance = new();

        public Clock Clock = new();

        public string Variant = string.Empty;

        public bool Rated;

        public bool Berserkable;

        public string Description = string.Empty;

        public string TeamMember = string.Empty;
    }
}