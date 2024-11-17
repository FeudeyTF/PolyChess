using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Commands
{
    public class CommandArgs<TParameter>
    {
        public TelegramBotClient Bot;

        public User User;

        public Message Message;

        public List<TParameter> Parameters;

        public CancellationToken Token;

        public CommandArgs(Message message, TelegramBotClient bot, User user, List<TParameter> args, CancellationToken token)
        {
            Message = message;
            User = user;
            Parameters = args;
            Bot = bot;
            Token = token;
        }

        public async Task Reply(TelegramMessageBuilder message)
        {
            await Bot.SendMessage(message.ReplyTo(Message.MessageId).WithToken(Token), Message.Chat.Id);
        }
    }

    public class CommandArgs(Message message, TelegramBotClient bot, User user, List<string> args, CancellationToken token) : CommandArgs<string>(message, bot, user, args, token)
    {
    }
}