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

		public PolyChessWebApp(WebApplication app)
		{
			_app = app;
		}

		public void Start()
		{
			_app.UseStaticFiles();
			_app.UseRouting();

			_app.MapRazorPages();
		}
	}
}
