using LichessAPI.Types.Arena;

namespace PolyChessTGBot.Managers.Tournaments
{
    public class ArenaTournamentInfo
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
