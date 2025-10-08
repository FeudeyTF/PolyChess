using LichessAPI.Types.Swiss;

namespace PolyChess.Components.Tournaments
{
    internal class SwissTournamentInfo
    {
        public SwissTournament Tournament { get; set; }

        public TournamentRating<SwissSheetEntry> Rating { get; set; }

        public SwissTournamentInfo(SwissTournament tournament, TournamentRating<SwissSheetEntry> rating)
        {
            Tournament = tournament;
            Rating = rating;
        }
    }
}
