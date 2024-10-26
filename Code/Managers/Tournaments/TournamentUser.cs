using PolyChessTGBot.Database;

namespace PolyChessTGBot.Managers.Tournaments
{
    public class TournamentUser<TValue>(User? user, int score, TValue entry)
    {
        public User? User = user;

        public int Score = score;

        public TValue TournamentEntry = entry;
    }
}
