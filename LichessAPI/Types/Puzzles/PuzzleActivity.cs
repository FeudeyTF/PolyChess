namespace LichessAPI.Types.Puzzles
{
    public class PuzzleActivity
    {
        public DateTime Date;

        public bool Win;

        public Puzzle Puzzle = new();
    }
}
