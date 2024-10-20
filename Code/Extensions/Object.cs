using Newtonsoft.Json;

namespace PolyChessTGBot.Extensions
{
    public static partial class Extensions
    {
        public static string Stringify(this object? obj, Formatting formatting = Formatting.Indented)
        {
            return obj == null ? string.Empty : JsonConvert.SerializeObject(obj, formatting);
        }
    }
}
