using System.Net;
using HttpServer;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using HttpListener = HttpServer.HttpListener;

namespace PolyChess.Core.Telegram.Providers
{
	internal class WebhookTelegramProvider : ITelegramProvider
	{
        public event OnUpdateDelegate OnUpdate;

        public event OnMessageDelegate OnMessage;

        public event OnCallbackDelegate OnCallback;

        public event OnExceptionDelegate OnException;

        public ITelegramBotClient Client { get; }

        private readonly CancellationToken _token;

		private readonly string _webhookUrl;

		private readonly HttpListener _listener;

        public WebhookTelegramProvider(ITelegramBotClient client, string webhookUrl, CancellationToken token)
        {
            OnUpdate = (client, update, token) => Task.CompletedTask;
            OnMessage = (client, message, token) => Task.CompletedTask;
            OnCallback = (client, callback, token) => Task.CompletedTask;
            OnException = (client, exception, source, token) => Task.CompletedTask;
            Client = client;
            _token = token;
			_webhookUrl = webhookUrl;
			_listener = HttpListener.Create(IPAddress.Parse("127.0.0.1"), 88);
        }

        public async Task StartAsync()
        {
			_listener.RequestReceived += HandleRequestReceived;
			_listener.Start(int.MaxValue);
			Console.WriteLine("Web server started");
			await Client.SetWebhook(_webhookUrl);	
        }

		private void HandleRequestReceived(object? sender, RequestEventArgs args)
		{
			Console.WriteLine(JsonConvert.SerializeObject(args.Request));
		}

        private async Task HandleUpdate(ITelegramBotClient client, Update update, CancellationToken token)
        {
        }

        private async Task HandleError(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
        {

        }

        public async Task SendMessageAsync(ITelegramMessage message, ChatId chatId)
        {
        }

        public async Task EditMessageAsync(Message message, ITelegramMessage newMessage)
        {
        }
	}
}
