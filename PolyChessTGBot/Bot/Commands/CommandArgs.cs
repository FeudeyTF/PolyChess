using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot.Commands
{
    public class CommandArgs
    {
        public TelegramBotClient Bot;

        public User User;

        public Message Message;

        public List<string> Parameters;

        public CommandArgs(Message message, TelegramBotClient bot, User user, List<string> args)
        {
            Message = message;
            User = user;
            Parameters = args;
            Bot = bot;
        }

        public async Task Reply(
            string message,
            int? messageThreadId = default,
            ParseMode parseMode = default,
            IEnumerable<MessageEntity>? entities = default,
            bool disableWebPagePreview = default,
            bool disableNotification = default,
            bool protectContent = default,
            bool allowSendingWithoutReply = default,
            IReplyMarkup? replyMarkup = default,
            CancellationToken cancellationToken = default
            )
        {
            await Bot.SendTextMessageAsync(Message.Chat.Id, message, messageThreadId, parseMode, entities, disableWebPagePreview, disableNotification, protectContent, Message.MessageId, allowSendingWithoutReply, replyMarkup, cancellationToken);
        }
    }
}