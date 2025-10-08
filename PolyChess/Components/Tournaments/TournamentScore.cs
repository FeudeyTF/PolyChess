namespace PolyChess.Components.Tournaments
{
    internal class TournamentsScore
    {
        public int Ones { get; set; }

        public int Zeros { get; set; }

        public TournamentsScore(int ones, int zeros)
        {
            Ones = ones;
            Zeros = zeros;
        }
    }
}
