using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Buttons
{
    internal class ButtonInteractArgs
    {
        public TelegramBotClient Bot => Program.Bot.Telegram;

        public string ButtonID;

        public CallbackQuery Query;

        private readonly TelegramButtonData Data;

        public ButtonInteractArgs(string id, CallbackQuery query, TelegramButtonData data)
        {
            ButtonID = id;
            Data = data;
            Query = query;
        }

        public int GetNumber(string parameter)
            => Data.GetNumber(parameter);

        public long GetLongNumber(string parameter)
            => Data.GetNumber(parameter);

        public float GetFloat(string parameter)
            => Data.GetFloat(parameter);

        public string GetString(string parameter)
            => Data.GetString(parameter);

        public async Task Reply(TelegramMessageBuilder message)
        {
            if (Query.Message != null)
                await Bot.SendMessage(message.ReplyTo(Query.Message.MessageId), Query.Message.Chat.Id);
        }

        public async Task Reply(IEnumerable<string> text, string separator = "\n")
            => await Reply(string.Join(separator, text));
    }
}
