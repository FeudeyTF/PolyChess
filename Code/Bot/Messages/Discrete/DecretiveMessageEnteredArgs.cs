using PolyChessTGBot.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Messages.Discrete
{
    public class DecretiveMessageEnteredArgs
    {
        public Message[] Answears;

        public TelegramBotClient Bot;

        public ChatId ChatID;

        public User User;

        public DecretiveMessageEnteredArgs(Message[] answears, TelegramBotClient bot, ChatId chat, User user)
        {
            Answears = answears;
            Bot = bot;
            ChatID = chat;
            User = user;
        }

        public async Task Reply(TelegramMessageBuilder message)
        {
            await Bot.SendMessage(message, ChatID);
        }
    }
}
