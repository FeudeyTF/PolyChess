using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PolyChess.Components.Telegram.Callback
{
    internal class TelegramCallbackQueryData : IParsable<TelegramCallbackQueryData>
    {
        public const char DataSeparator = '|';

        public string ButtonId;

        public ConcurrentDictionary<string, object> Parameters;

        public TelegramCallbackQueryData(string id)
        {
            ButtonId = id;
            Parameters = new();
        }

        public int GetNumber(string parameter)
        {
            if (Parameters.TryGetValue(parameter, out var value))
                if (value is long result)
                    return (int)result;
            return default;
        }

        public long GetLongNumber(string parameter)
        {
            if (Parameters.TryGetValue(parameter, out var value))
                if (value is long result)
                    return result;
            return default;
        }

        public float GetFloat(string parameter)
        {
            if (Parameters.TryGetValue(parameter, out var value))
                if (value is float result)
                    return result;
            return default;
        }

        public string GetString(string parameter)
        {
            if (Parameters.TryGetValue(parameter, out var value))
                return value.ToString() ?? "";
            return "";
        }

        public void Set<TValue>(string name, TValue value)
        {
            if (value != null && !Parameters.TryAdd(name, value))
            {
                Parameters.TryUpdate(name, value, Parameters[name]);
            }
        }

        public static string GetDataString(string buttonId, params (string name, object value)[] values)
        {
            return buttonId + (values.Length > 0 ? DataSeparator + string.Join(DataSeparator, values.Select(v => v.name + DataSeparator + v.value)) : "");
        }

        public static TelegramCallbackQueryData Parse(string str, IFormatProvider? provider = default)
        {
            var sliced = str.Split(DataSeparator);
            TelegramCallbackQueryData result = new(sliced[0]);
            FillData(result, sliced);
            return result;
        }

        public static bool TryParse([NotNullWhen(true)] string? str, IFormatProvider? provider, [MaybeNullWhen(false)] out TelegramCallbackQueryData result)
        {
            result = null;
            if (str == null)
                return false;

            var sliced = str.Split(DataSeparator);
            if (sliced.Length != 0)
            {
                result = new(sliced[0]);
                FillData(result, sliced);
                return true;
            }
            return false;
        }

        private static void FillData(TelegramCallbackQueryData data, string[] sliced)
        {
            if (sliced.Length > 2)
            {
                for (int i = 1; i < sliced.Length - 1; i += 2)
                {
                    var name = sliced[i];
                    var value = sliced[i + 1];
                    if (long.TryParse(value, out long longNumber))
                        data.Set(name, longNumber);
                    else if (float.TryParse(value, out float floatNumber))
                        data.Set(name, floatNumber);
                    else
                        data.Set(name, value);
                }
            }
        }
    }
}
