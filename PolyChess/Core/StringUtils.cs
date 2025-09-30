namespace PolyChess.Core
{
    internal static class StringUtils
    {
        public static string CreateSimpleBar(double now, double max, char empty = '□', char solid = '■', int bars = 10)
        {
            var result = "";
            double solidBarsCount = now / max * bars;
            for (int i = 0; i < bars; i++)
                if (i <= solidBarsCount - 1)
                    result += solid;
                else
                    result += empty;
            return result;
        }
    }
}
