namespace PolyChess.LichessAPI.Types.Puzzles
{
    public class Puzzle
    {
        public string Fen = string.Empty;

        public string ID = string.Empty;

        public string LastMove = string.Empty;

        public int Plays;

        public int Rating;

        public string[] Solution = [];

        public string[] Themes = [];
    }
}
