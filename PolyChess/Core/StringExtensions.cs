namespace PolyChess.Core
{
    internal static class StringExtensions
    {
        public static string Upperize(this string str)
            => str[0].ToString().ToUpper() + str[1..];
    }
}
