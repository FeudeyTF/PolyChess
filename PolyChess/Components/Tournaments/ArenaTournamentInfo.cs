using PolyChess.LichessAPI.Types.Arena;

namespace PolyChess.Components.Tournaments
{
    internal class ArenaTournamentInfo
    {
        public ArenaTournament Tournament { get; set; }

        public TournamentRating<SheetEntry> Rating { get; set; }

        public ArenaTournamentInfo(ArenaTournament tournament, TournamentRating<SheetEntry> rating)
        {
            Tournament = tournament;
            Rating = rating;
        }
    }
}
