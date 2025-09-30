using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChess.Core.Telegram
{
    internal delegate Task OnUpdateDelegate(ITelegramBotClient client, Update update, CancellationToken token);

    internal delegate Task OnMessageDelegate(ITelegramBotClient client, Message message, CancellationToken token);

    internal delegate Task OnCallbackDelegate(ITelegramBotClient client, CallbackQuery query, CancellationToken token);

    internal interface ITelegramProvider
    {
        public event OnUpdateDelegate OnUpdate;

        public event OnMessageDelegate OnMessage;

        public event OnCallbackDelegate OnCallback;

        public ITelegramBotClient Client { get; }

        public Task StartAsync();

        public Task SendMessageAsync(ITelegramMessage message, ChatId chatId);

        public Task EditMessageAsync(Message message, ITelegramMessage newMessage);
    }
}
