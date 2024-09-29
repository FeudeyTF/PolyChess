using System.Collections.Concurrent;

namespace PolyChessTGBot.Bot.Buttons
{
    public class TelegramButtonData
    {
        public const string DataSeparator = "|||";

        public string ButtonID;

        public ConcurrentDictionary<string, object> Parameters;

        public TelegramButtonData(string id)
        {
            ButtonID = id;
            Parameters = new();
        }

        public TValue? Get<TValue>(string parameter)
        {
            if (Parameters.TryGetValue(parameter, out var value))
                if (value is TValue result)
                    return result;
            return default;
        }

        public void Set<TValue>(string name, TValue value)
        {
            if (value != null && !Parameters.TryAdd(name, value))
            {
                Parameters.TryUpdate(name, value, Parameters[name]);
            }
        }

        public static string GetDataString(string buttonID, params (string name, object value)[] values)
        {
            return buttonID + (values.Length > 0 ? DataSeparator + string.Join(DataSeparator, values.Select(v => v.name + DataSeparator + v.value)) : "");
        }

        public static TelegramButtonData? ParseDataString(string data)
        {
            string[] sliced = data.Split(DataSeparator);
            if (sliced.Any())
            {
                TelegramButtonData result = new(sliced[0]);
                if (sliced.Length > 2)
                {
                    for (int i = 1; i < sliced.Length - 1; i += 2)
                    {
                        string name = sliced[i];
                        string value = sliced[i + 1];
                        if (int.TryParse(value, out int intNumber))
                            result.Set(name, intNumber);
                        else if (long.TryParse(value, out long longNumber))
                            result.Set(name, longNumber);
                        else if (float.TryParse(value, out float floatNumber))
                            result.Set(name, floatNumber);
                        else
                            result.Set(name, value);
                    }
                }
                return result;
            }
            return null;
        }
    }
}
