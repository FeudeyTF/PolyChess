using PolyChessTGBot.Bot.Buttons;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Externsions
{
    public static partial class Extensions
    {
        public static void SetData(this InlineKeyboardButton button, string id, params (string name, object value)[] values)
            => button.CallbackData = TelegramButtonData.GetDataString(id, values);

        public static TValue? Get<TValue>(this InlineKeyboardButton button, string id)
        {
            if(button.CallbackData != null)
            {
                var data = TelegramButtonData.ParseDataString(button.CallbackData);
                if(data != null)
                    return data.Get<TValue>(id);
            }
            return default;
        }

        public static string Get(this InlineKeyboardButton button, string id)
        {
            var str = button.Get<string>(id);
            if (string.IsNullOrEmpty(str))
                return "";
            return str;
        }
    }
}
