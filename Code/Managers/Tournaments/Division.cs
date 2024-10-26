namespace PolyChessTGBot.Managers.Tournaments
{
    public struct Division(int min, int max)
    {
        public int Min = min;

        public int Max = max;

        public bool InDivision(int rating)
            => rating >= Min && rating <= Max;
    }
}
