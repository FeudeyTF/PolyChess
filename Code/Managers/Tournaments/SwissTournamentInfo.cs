using LichessAPI.Types.Swiss;

namespace PolyChessTGBot.Managers.Tournaments
{
    public class SwissTournamentInfo
    {
        public SwissTournament Tournament;

        public TournamentRating<SwissSheetEntry> Rating;

        public SwissTournamentInfo(SwissTournament tournament, TournamentRating<SwissSheetEntry> rating)
        {
            Tournament = tournament;
            Rating = rating;
        }
    }
}
