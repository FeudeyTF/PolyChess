using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChess.Core.Telegram.Messages.Discrete.Messages
{
    public class DiscreteMessageEnteredArgs
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

        public async Task ReplyAsync(string message)
        {
            TelegramMessageBuilder builder = message;
            builder.ReplyParameters = new()
            {
                MessageId = Responses.Last().MessageId
            };
            await builder.SendAsync(Client, ChatId, Token);
        }
    }
}
