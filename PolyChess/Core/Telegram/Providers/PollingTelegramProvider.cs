using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChess.Core.Telegram.Providers
{
    internal class PollingTelegramProvider : ITelegramProvider
    {
        public event OnUpdateDelegate OnUpdate;

        public event OnMessageDelegate OnMessage;

        public event OnCallbackDelegate OnCallback;

        public ITelegramBotClient Client { get; }

        private readonly ReceiverOptions _receiverOptions;

        private readonly CancellationToken _token;

        private readonly IUpdateHandler _updateHandler;

        public PollingTelegramProvider(ITelegramBotClient client, ReceiverOptions receiverOptions, CancellationToken token)
        {
            OnUpdate = (client, update, token) => Task.CompletedTask;
            OnMessage = (client, update, token) => Task.CompletedTask;
            OnCallback = (client, update, token) => Task.CompletedTask;
            Client = client;
            _token = token;
            _updateHandler = new DefaultUpdateHandler(HandleUpdate, HandleError);
            _receiverOptions = receiverOptions;
        }

        public Task StartAsync()
        {
            Client.StartReceiving(_updateHandler, _receiverOptions, _token);
            return Task.CompletedTask;
        }

        private async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken token)
        {
            await OnUpdate(client, update, token);
            if (update.Type == UpdateType.Message && update.Message != null)
                await OnMessage(client, update.Message, token);
            else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                await OnCallback(client, update.CallbackQuery, token);
        }

        private async Task HandleError(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {
            await Task.CompletedTask;
        }

        public async Task SendMessageAsync(ITelegramMessage message, ChatId chatId)
        {
            await message.SendAsync(Client, chatId, _token);
        }

        public async Task EditMessageAsync(Message message, ITelegramMessage newMessage)
        {
            await newMessage.EditAsync(Client, message, _token);
        }
    }
}
