using System.Numerics;

namespace PolyChess.Configuration
{
    internal interface IMainConfig
    {
        public string TelegramToken { get; set; }

        public string DatabaseConnectionString { get; set; }

        public List<long> TelegramAdmins { get; set; }

        public DateTime SemesterStartDate { get; set; }

        public long QuestionChannelId { get; set; }

        public long CreativeTaskChannel { get; set; }

        public DateTime SemesterEndDate { get; set; }

        public Vector2 SchoolLocation { get; set; }

        public List<string> ClubTeamPlayers { get; set; }

        public List<string> InstitutesTeams { get; set; }

        public List<string> LichessFlairs { get; set; }

        public TestSettings Test { get; set; }

        public Dictionary<string, TournamentScoreRule> TournamentRules { get; set; }
    }

    internal class TestSettings
    {
        public int RequiredTournamentsCount { get; set; }

        public int RequiredVisitedLessonsPercent { get; set; }

        public int RequiredPuzzlesSolved { get; set; }
    }

    internal class TournamentScoreRule
    {
        public const int DefaultWinningPoints = 1;

        public const int DefaultBeingPoints = 1;

        public int PointsForWinning { get; set; } = DefaultWinningPoints;

        public int PointsForBeing { get; set; } = DefaultBeingPoints;
    }
}
