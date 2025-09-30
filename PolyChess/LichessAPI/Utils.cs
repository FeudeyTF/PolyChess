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

        internal static string GetQueryParametersString(params (string name, object? val)[] parameters)
        {
            List<string> paramList = [];
            foreach (var (name, val) in parameters)
                if (val != default)
                {
                    string? paramValue = val.ToString();
                    if (val is DateTime date)
                        paramValue = (date.Ticks * 1000).ToString();
                    paramList.Add(name + "=" + paramValue);
                }
            return paramList.Count != 0 ? "?" + string.Join("&", parameters) : "";
        }
    }
}
