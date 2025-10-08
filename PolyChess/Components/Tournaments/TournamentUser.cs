using PolyChess.Components.Data.Tables;

namespace PolyChess.Components.Tournaments
{
    internal class TournamentUser<TValue>(Student? user, int score, TValue entry)
    {
        public Student? Student { get; set; } = user;

        public int Score { get; set; } = score;

        public TValue TournamentEntry { get; set; } = entry;
    }
}
