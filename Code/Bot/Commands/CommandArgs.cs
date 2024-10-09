using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Externsions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Commands
{
    public class CommandArgs
    {
        public TelegramBotClient Bot;

        public User User;

        public Message Message;

        public List<string> Parameters;

        public CancellationToken Token;

        public CommandArgs(Message message, TelegramBotClient bot, User user, List<string> args, CancellationToken token)
        {
            Message = message;
            User = user;
            Parameters = args;
            Bot = bot;
            Token = token;
        }

        public async Task Reply(TelegramMessageBuilder message)
        {
            await Bot.SendMessage(message.ReplyTo(Message.MessageId), Message.Chat.Id);
        }
    }
}