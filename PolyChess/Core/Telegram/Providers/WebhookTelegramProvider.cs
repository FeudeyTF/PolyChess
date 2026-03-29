using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChess.Core.Telegram.Providers
{
	internal class WebhookTelegramProvider : ITelegramProvider
	{
		public event OnUpdateDelegate OnUpdate;

		public event OnMessageDelegate OnMessage;

		public event OnCallbackDelegate OnCallback;

		public event OnExceptionDelegate OnException;

		public ITelegramBotClient Client { get; }

		private readonly string _webhookUrl;

		private readonly WebApplication _app;

		private readonly string _certificatePath;

		private readonly string _telegramSecret;

		private readonly CancellationToken _token;

		public WebhookTelegramProvider(ITelegramBotClient client, string webhookUrl, string telegramSecret, string certificatePath, CancellationToken token, WebApplication app)
		{
			OnUpdate = (client, update, token) => Task.CompletedTask;
			OnMessage = (client, message, token) => Task.CompletedTask;
			OnCallback = (client, callback, token) => Task.CompletedTask;
			OnException = (client, exception, source, token) => Task.CompletedTask;
			Client = client;
			_certificatePath = certificatePath;
			_telegramSecret = telegramSecret;
			if (string.IsNullOrEmpty(_telegramSecret))
				throw new Exception("Telegram secret is empty or null!");
			_webhookUrl = webhookUrl;
			_token = token;
			_app = app;
		}

		public async Task StartAsync()
		{
			_app.MapGet($"/bot/{_telegramSecret}/setWebhook", SetWebhook);
			_app.MapGet($"/bot/{_telegramSecret}/getWebhook", GetWebhook);
			_app.MapGet($"/bot/{_telegramSecret}/delWebhook", DeleteWebhook);
			_app.MapPost("/bot", HandleUpdate);
		}

		private async Task<string> SetWebhook(ITelegramBotClient client)
		{
			await client.SetWebhook(_webhookUrl, new InputFileStream(File.OpenRead(_certificatePath), _certificatePath), secretToken: _telegramSecret, cancellationToken: _token);
			return $"Webhook set to {_webhookUrl}";
		}

		private async Task<string> DeleteWebhook(ITelegramBotClient client)
		{
			await client.DeleteWebhook(cancellationToken: _token);
			return $"Webhook {_webhookUrl} deleted";
		}

		private async Task<string> GetWebhook(ITelegramBotClient client)
		{
			var info = await client.GetWebhookInfo(_token);
			return JsonConvert.SerializeObject(info, Formatting.Indented);
		}

		private async Task HandleUpdate(HttpContext context, ITelegramBotClient client, Update update, CancellationToken token)
		{
			if (context.Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != _telegramSecret)
				return;

			await OnUpdate(client, update, token);
			if (update.Type == UpdateType.Message && update.Message != null)
				await OnMessage(client, update.Message, token);
			else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
				await OnCallback(client, update.CallbackQuery, token);
		}

		private async Task HandleError(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken token)
		{
			await OnException(client, exception, source, token);
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
