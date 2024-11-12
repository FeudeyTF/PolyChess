using Telegram.Bot.Types;
using Telegram.Bot;
using PolyChessTGBot.Extensions;

namespace PolyChessTGBot.Bot.Messages.Discrete
{
    public class DiscreteMessageNextRecievedArgs
    {
        public int Index;

        public TelegramMessageBuilder Query;

        public Message Response;

        public TelegramBotClient Bot;

        public ChatId ChatID;

        public User User;

        public List<object> Data;

        public DiscreteMessageNextRecievedArgs(int index, TelegramMessageBuilder query, Message response, TelegramBotClient bot, ChatId chat, User user, List<object> data)
        {
            Index = index;
            Query = query;
            Response = response;
            Bot = bot;
            ChatID = chat;
            User = user;
            Data = data;
        }

        public async Task Reply(TelegramMessageBuilder message)
        {
            await Bot.SendMessage(message, ChatID);
        }
    }
}
