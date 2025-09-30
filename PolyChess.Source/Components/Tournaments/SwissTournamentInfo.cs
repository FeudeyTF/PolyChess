using LichessAPI.Types.Swiss;

namespace PolyChess.Components.Tournaments
{
    internal class SwissTournamentInfo
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
