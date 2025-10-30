namespace PolyChess.Components.Tournaments
{
    internal struct TournamentRating<TValue>
    {
        public Dictionary<DivisionType, List<TValue>> Divisions { get; set; }

        public List<TournamentUser<TValue>> Players { get; set; }

        public TournamentRating(Dictionary<DivisionType, List<TValue>> divisions, List<TournamentUser<TValue>> players)
        {
            Divisions = divisions;
            Players = players;
        }
    }
}
