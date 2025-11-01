using PolyChess.Components.Telegram.Callback;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Components.Telegram
{
    internal static class TelegramExtensions
    {
        public static void SetData(this InlineKeyboardButton button, string id, params (string name, object value)[] values)
            => button.CallbackData = TelegramCallbackQueryData.GetDataString(id, values);

        public static InlineKeyboardButton WithData(this InlineKeyboardButton button, string id, params (string name, object value)[] values)
        {
            button.SetData(id, values);
            return button;
        }

        public static async Task SendMessageAsync(this ITelegramBotClient client, ITelegramMessage message, ChatId chatId, CancellationToken token)
            => await message.SendAsync(client, chatId, token);

        public static async Task SendMessageAsync(this ITelegramBotClient client, string message, ChatId chatId, CancellationToken token)
            => await client.SendMessageAsync(new TelegramMessageBuilder(message), chatId, token);

        public static async Task EditMessageAsync(this ITelegramBotClient client, ITelegramMessage message, Message oldMessage, CancellationToken token)
            => await message.EditAsync(client, oldMessage, token);
    }
}
