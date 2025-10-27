namespace PolyChess.LichessAPI.Types.Arena
{
    public class SheetEntry
    {
        public int Rank;

        public int Score;

        public int Rating;

        public string Username = string.Empty;

        public string? Team;

        public int Performance;

        public Sheet? Sheet;
    }
}