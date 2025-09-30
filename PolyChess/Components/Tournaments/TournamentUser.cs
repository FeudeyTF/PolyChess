using PolyChess.Components.Data.Tables;

namespace PolyChess.Components.Tournaments
{
    internal class TournamentUser<TValue>(Student? user, int score, TValue entry)
    {
        public Student? Student = user;

        public int Score = score;

        public TValue TournamentEntry = entry;
    }
}
