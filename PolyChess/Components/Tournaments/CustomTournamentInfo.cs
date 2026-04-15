using PolyChess.Components.Data.Tables;
using PolyChess.LichessAPI.Types.Arena;

namespace PolyChess.Components.Tournaments
{
    internal class CustomTournamentInfo
    {
        public CustomTournament Tournament { get; set; }

        public TournamentRating<CustomSheetEntry> Rating { get; set; }

        public CustomTournamentInfo(CustomTournament tournament, TournamentRating<CustomSheetEntry> rating)
        {
            Tournament = tournament;
            Rating = rating;
        }
    }
}
