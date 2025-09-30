namespace PolyChess.Core
{
    internal static class Extensions
    {
        public static TValue Random<TValue>(this IEnumerable<TValue> values, Random? random = default)
        {
            random ??= System.Random.Shared;
            return values.ElementAt(random.Next(values.Count()));
        }
    }
}
