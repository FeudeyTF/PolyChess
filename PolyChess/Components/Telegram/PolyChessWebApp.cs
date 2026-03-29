using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PolyChess.Configuration;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Components.Telegram
{
	internal class PolyChessWebApp
	{
		private readonly WebApplication _app;

		private readonly IMainConfig _configuration;

		public PolyChessWebApp(WebApplication app, IMainConfig config)
		{
			_app = app;
			_configuration = config;
		}

		public void Start()
		{
			_app.UseStaticFiles();
			_app.UseRouting();

			_app.MapRazorPages();
			_app.MapPost("/polychess/api", OnPolyChessApi).DisableAntiforgery();
		}

		async Task<object> OnPolyChessApi(ITelegramBotClient bot, [FromForm] string method, IFormCollection form)
		{
			var query = AuthHelpers.ParseValidateData(form["_auth"], _configuration.Telegram.TelegramToken);
			switch (method)
			{
				case "checkInitData":
					return new { ok = true };
				case "sendMessage":
					string? msg_id = form["msg_id"], with_webview = form["with_webview"];
					var user = JsonSerializer.Deserialize<User>(query["user"], JsonBotAPI.Options)!;
					await bot.SendMessage(user.Id, "Hello, World!",
						replyMarkup: with_webview == "0" ? new ReplyKeyboardRemove() :
							new ReplyKeyboardMarkup(true).AddButton(KeyboardButton.WithWebApp("Open WebApp", _configuration.Telegram.TelegramWebhookUrl + "/demo")));
					return new { response = new { ok = true } };
				default:
					return new { ok = false, error = "Unsupported method: " + method };
			}
		}
	}
}
