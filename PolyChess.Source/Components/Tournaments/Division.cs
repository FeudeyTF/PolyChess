namespace PolyChess.Components.Tournaments
{
    internal struct Division(int min, int max)
    {
        public int Min = min;

        public int Max = max;

        public bool InDivision(int rating)
            => rating >= Min && rating <= Max;
    }
}
