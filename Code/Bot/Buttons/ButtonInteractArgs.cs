using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Externsions;
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

        public TValue? Get<TValue>(string parameter)
            => Data.Get<TValue>(parameter);

        public async Task Reply(TelegramMessageBuilder message)
        {
            if (Query.Message != null)
                await Bot.SendMessage(message.ReplyTo(Query.Message.MessageId), Query.Message.Chat.Id);
        }
    }
}
