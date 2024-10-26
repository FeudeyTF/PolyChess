namespace PolyChessTGBot.Managers.Tournaments
{
    public struct TournamentRating<TValue>(Dictionary<DivisionType, List<TValue>> divisions, List<TournamentUser<TValue>> players)
    {
        public Dictionary<DivisionType, List<TValue>> Divisions = divisions;

        public List<TournamentUser<TValue>> Players = players;
    }
}
