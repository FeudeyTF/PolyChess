namespace PolyChessTGBot.Lichess.Types.Arena
{
    public class ArenaPlayerPerformance
    {
        public string Name = string.Empty;

        public int Rank;

        public int Rating;

        public int Score;

        public Sheet Sheet = new();

        public string Flair = string.Empty;
    }
}
