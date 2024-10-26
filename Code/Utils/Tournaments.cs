using LichessAPI.Types.Arena;
using LichessAPI.Types.Swiss;

namespace PolyChessTGBot
{
    public static partial class Utils
    {
        public static bool IsSemesterTournament(this ArenaTournament tournament)
            => !Program.MainConfig.UnnecessaryTournaments.Contains(tournament.ID) && tournament.StartDate > Program.SemesterStartDate && !tournament.Description.Contains("баллы за этот турнир не начисляются", StringComparison.CurrentCultureIgnoreCase);

        public static bool IsSemesterTournament(this SwissTournament tournament)
            => !Program.MainConfig.UnnecessaryTournaments.Contains(tournament.ID) && tournament.Started > Program.SemesterStartDate;
    }
}
