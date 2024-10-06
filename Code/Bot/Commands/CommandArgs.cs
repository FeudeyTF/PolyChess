using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Externsions;
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

        public async Task Reply(TelegramMessageBuilder message)
        {
            await Bot.SendMessage(message.ReplyTo(Message.MessageId), Message.Chat.Id);
        }
    }
}