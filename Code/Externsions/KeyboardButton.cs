using PolyChessTGBot.Bot.Buttons;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Externsions
{
    public static partial class Extensions
    {
        public static void SetData(this InlineKeyboardButton button, string id, params (string name, object value)[] values)
            => button.CallbackData = TelegramButtonData.GetDataString(id, values);
    }
}
