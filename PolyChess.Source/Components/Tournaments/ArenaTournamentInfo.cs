using LichessAPI.Types.Arena;

namespace PolyChess.Components.Tournaments
{
    internal class ArenaTournamentInfo
    {
        public ArenaTournament Tournament;

        public TournamentRating<SheetEntry> Rating;

        public ArenaTournamentInfo(ArenaTournament tournament, TournamentRating<SheetEntry> rating)
        {
            Tournament = tournament;
            Rating = rating;
        }
    }
}
