namespace PolyChess.Components.Tournaments
{
    internal struct Division(int min, int max)
    {
        public int Min { get; set; } = min;

        public int Max { get; set; } = max;

        public bool InDivision(int rating)
            => rating >= Min && rating <= Max;
    }
}
