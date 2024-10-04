using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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

        public async Task Reply(
            string message,
            int? messageThreadId = default,
            ParseMode parseMode = ParseMode.Markdown,
            IEnumerable<MessageEntity>? entities = default,
            bool disableWebPagePreview = default,
            bool disableNotification = default,
            bool protectContent = default,
            bool allowSendingWithoutReply = default,
            IReplyMarkup? replyMarkup = default,
            CancellationToken cancellationToken = default
            )
        {
            if(Query.Message != null)
                await Bot.SendTextMessageAsync(Query.Message.Chat.Id, message, messageThreadId, parseMode, entities, disableWebPagePreview, disableNotification, protectContent, Query.Message.MessageId, allowSendingWithoutReply, replyMarkup, cancellationToken);
        }
    }
}
