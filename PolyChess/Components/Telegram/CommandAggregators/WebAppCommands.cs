using PolyChess.Components.Telegram.Commands;
using PolyChess.Core.Telegram.Messages;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Components.Telegram.CommandAggregators
{
	internal class WebAppCommands : TelegramCommandAggregator
	{
		[TelegramCommand("app", "Starts cool app")]
		public async Task StartCoolApp(TelegramCommandExecutionContext ctx)
		{
			TelegramMessageBuilder message = "Hello, here is your app!";
			message.ReplyMarkup = (InlineKeyboardMarkup)InlineKeyboardButton.WithWebApp("Launch Mini-App", "https://example.com");
			await ctx.ReplyAsync(message);
		}
	}
}
