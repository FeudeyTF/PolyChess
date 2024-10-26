namespace LichessAPI.Types.Puzzles
{
    public class PuzzleDashboard
    {
        public int Days;

        public PuzzlePerformance Global = new();

        public Dictionary<ThemeType, PuzzleThemeResult> Themes = [];
    }
}