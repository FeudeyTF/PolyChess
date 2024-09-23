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

		public CommandArgs(Message message, TelegramBotClient bot, User user, List<string> args)
		{
			Message = message;
			User = user;
			Parameters = args;
            Bot = bot;
		}

        public async Task Reply(string message)
        {
            await Bot.SendTextMessageAsync(Message.Chat.Id, message, replyToMessageId: Message.MessageId);
        }
    }
}