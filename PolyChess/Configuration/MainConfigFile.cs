using PolyChess.Core.Configuration;
using System.Numerics;

namespace PolyChess.Configuration
{
    internal class MainConfigFile : ConfigFile, IMainConfig
    {
        public override string Path => System.IO.Path.Combine("config.json");

        public string TelegramToken { get; set; } = string.Empty;

        public string DatabaseConnectionString { get; set; } = "Data Source=data.sqlite";

        public List<long> TelegramAdmins { get; set; } = [];

        public DateTime SemesterStartDate { get; set; }

        public DateTime SemesterEndDate { get; set; }

        public List<string> ClubTeamPlayers { get; set; } = [];

        public List<string> InstitutesTeams { get; set; } = [];

        public TestSettings Test { get; set; } = new();

        public Dictionary<string, TournamentScoreRule> TournamentRules { get; set; } = [];

        public Vector2 SchoolLocation { get; set; } = new(59.965128f, 30.398474f);

        public long QuestionChannelId { get; set; }

        public long CreativeTaskChannelId { get; set; }

        public List<string> LichessFlairs { get; set; } = [];

        public List<string> TeamsWithTournaments { get; set; } = [];
    }
}
