using LichessAPI.Converters;
using System.Text.Json.Serialization;

namespace LichessAPI.Types.Arena
{
    public class ArenaTournament
    {
        [JsonPropertyName("nbPlayers")]
        public int PlayersNumber;

        public object[] Duels = [];

        public bool IsFinished;

        public ArenaPlace[] Podium = [];

        public bool PairingsClosed;

        public ArenaStats Stats = new();

        public Standing Standing = new();

        public string ID = string.Empty;

        public string CreatedBy = string.Empty;

        [JsonPropertyName("startsAt")]
        [JsonConverter(typeof(LichessDateTimeConverter))]
        public DateTime StartDate;

        [JsonPropertyName("finishesAt")]
        [JsonConverter(typeof(LichessDateTimeConverter))]
        public DateTime FinishDate;

        public string System = string.Empty;

        public string FullName = string.Empty;

        public int Minutes;

        [JsonPropertyName("perf")]
        public ArenaPerfomance Perfomance = new();

        public object? Variant;

        public Clock Clock = new();

        public bool Rated;

        public bool Berserkable;

        public string Description = string.Empty;

        public string TeamMember = string.Empty;

        public TeamBattle TeamBattle = new();
    }
}