using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using System.Collections.Concurrent;

namespace PolyChessTGBot
{
    public static partial class Utils
    {
        public const string DataSeparator = "|||";

        public static BotCommandScope GetScopeByType(BotCommandScopeType type)
        {
            return type switch
            {
                BotCommandScopeType.AllChatAdministrators => BotCommandScope.AllChatAdministrators(),
                BotCommandScopeType.AllGroupChats => BotCommandScope.AllGroupChats(),
                BotCommandScopeType.AllPrivateChats => BotCommandScope.AllPrivateChats(),
                _ => BotCommandScope.Default()
            };
        }

        public static string GetDataString(string buttonID, params (string name, object value)[] values)
        {
            var result = buttonID + DataSeparator + string.Join(DataSeparator, values.Select(v => v.name + DataSeparator + v.value));
            //Console.WriteLine("DS: " + result);
            return result;
        }

        public static TelegramButtonData? ParseDataString(string data)
        {
            string[] sliced = data.Split(DataSeparator);
            if(sliced.Any())
            {
                TelegramButtonData result = new(sliced[0]);
                if(sliced.Length > 2)
                {
                    for(int i = 1; i < sliced.Length -1; i += 2)
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
               // Console.WriteLine("RS DATAS: " + data);
               // Console.WriteLine("RS Name: " + result.ButtonID);
              //  foreach(var val in result.Parameters)
               //     Console.WriteLine("RS Data: " + val.Key + " " + val.Value.ToString() + " " + val.Value.GetType().Name);
                return result;
            }
            return null;
        }

        public class TelegramButtonData
        {
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
        }
    }
}
