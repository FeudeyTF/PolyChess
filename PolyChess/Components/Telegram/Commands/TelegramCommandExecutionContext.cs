using PolyChess.Configuration;
using PolyChess.Core.Commands;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using Telegram.Bot.Types;

namespace PolyChess.Components.Telegram.Commands
{
    internal class TelegramCommandExecutionContext : ICommandExecutionContext
    {
        public User User { get; }

        public List<string> Arguments { get; }

        public ITelegramProvider Provider { get; }

        public Message Message { get; }

        public IMainConfig Config { get; }

        public CancellationToken Token { get; }

        public TelegramCommandExecutionContext(User user, List<string> args, Message message, IMainConfig config, ITelegramProvider provider, CancellationToken token)
        {
            User = user;
            Arguments = args;
            Provider = provider;
            Config = config;
            Message = message;
            Token = token;
        }

        public async Task SendMessageAsync(ITelegramMessage message, ChatId chatId)
            => await Provider.SendMessageAsync(message, chatId);

        public async Task ReplyAsync(TelegramMessageBuilder message)
        {
            message.ReplyParameters ??= new();
            message.ReplyParameters.MessageId = Message.Id;
            await SendMessageAsync(message, Message.Chat.Id);
        }
    }
}
