using PolyChess.Components.Telegram.Callback;
using PolyChess.Core.Commands;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using Telegram.Bot.Types;

namespace PolyChess.Components.Telegram.Buttons
{
    internal class TelegramButtonExecutionContext : ICommandExecutionContext
    {
        public string ButtonId;

        public CallbackQuery Query;

        public ITelegramProvider Provider;

        public List<string> Arguments { get; }

        public readonly CancellationToken Token;

        private readonly TelegramCallbackQueryData _data;

        public TelegramButtonExecutionContext(string buttonId, CallbackQuery query, TelegramCallbackQueryData data, CancellationToken token, ITelegramProvider provider, List<string> arguments)
        {
            ButtonId = buttonId;
            Query = query;
            Token = token;
            Arguments = arguments;
            Provider = provider;
            _data = data;
        }

        public int GetNumber(string parameter)
            => _data.GetNumber(parameter);

        public long GetLongNumber(string parameter)
            => _data.GetLongNumber(parameter);

        public float GetFloat(string parameter)
            => _data.GetFloat(parameter);

        public string GetString(string parameter)
            => _data.GetString(parameter);

        public async Task SendMessageAsync(ITelegramMessage message, ChatId chatId)
            => await Provider.SendMessageAsync(message, chatId);

        public async Task ReplyAsync(TelegramMessageBuilder message)
        {
            if (Query.Message == null)
                return;
            message.ReplyParameters ??= new();
            message.ReplyParameters.MessageId = Query.Message.Id;
            await SendMessageAsync(message, Query.Message.Chat.Id);
        }
    }
}
