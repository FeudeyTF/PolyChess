namespace PolyChessTGBot
{
    public static class Utils
    {
        public static string RemoveBadSymbols(this string message) =>
            message.Replace(".", @"\.")
                   .Replace("!", @"\!")
                   .Replace("-", @"\-");
    }
}