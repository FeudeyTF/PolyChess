using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChess.Core.Telegram.Messages.Discrete.Messages
{
    internal class DiscreteMessageEnteredArgs
    {
        public Message[] Responses;

        public ITelegramBotClient Client;

        public ChatId ChatId;

        public User User;

        public CancellationToken Token;

        public DiscreteMessageEnteredArgs(Message[] responses, ITelegramBotClient bot, ChatId chat, User user, CancellationToken token)
        {
            Responses = responses;
            Client = bot;
            ChatId = chat;
            User = user;
            Token = token;
        }

        public async Task ReplyAsync(TelegramMessageBuilder message)
        {
            message.ReplyParameters = new()
            {
                MessageId = Responses.Last().MessageId
            };
            await message.SendAsync(Client, ChatId, Token);
        }
    }
}
