using System.Text.Json;

namespace LichessAPI
{
    internal static class Utils
    {
        internal static List<TValue> ParseNDJsonObject<TValue>(string value)
        {
            List<TValue> result = [];
            foreach (var entry in value.Split('\n'))
            {
                if (!string.IsNullOrEmpty(entry))
                {
                    var obj = JsonSerializer.Deserialize<TValue>(entry, LichessApiClient.SerializerOptions);
                    if (obj != null)
                        result.Add(obj);
                }
            }
            return result;
        }
    }
}
