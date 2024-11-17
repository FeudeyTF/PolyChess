using PolyChessTGBot.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Messages.Discrete
{
    public class DiscreteMessageNextSendedArgs
    {
        public int Index;

        public TelegramMessageBuilder Message;

        public TelegramBotClient Bot;

        public ChatId ChatID;

        public List<object> Data;

        public DiscreteMessageNextSendedArgs(int index, TelegramMessageBuilder message, TelegramBotClient bot, ChatId chat, List<object> data)
        {
            Index = index;
            Message = message;
            Bot = bot;
            ChatID = chat;
            Data = data;
        }

        public async Task Reply(TelegramMessageBuilder message)
        {
            await Bot.SendMessage(message, ChatID);
        }
    }
}
