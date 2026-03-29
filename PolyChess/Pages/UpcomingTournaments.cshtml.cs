using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PolyChess.Components.Data;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace PolyChess.Pages
{
	internal class UpcomingTournamentInfo
	{
		public string Id { get; set; } = string.Empty;

		public string Name { get; set; } = string.Empty;

		public DateTime StartDate { get; set; }

		public string Type { get; set; } = string.Empty;

		public int Duration { get; set; }

		public string TimeControl { get; set; } = string.Empty;

		public bool IsStartingSoon => StartDate > DateTime.Now && StartDate <= DateTime.Now.AddHours(2);
	}

	internal class UpcomingTournamentsModel : PageModel
	{
		public List<UpcomingTournamentInfo> Tournaments { get; private set; } = [];

		public bool IsAuthenticated { get; private set; }

		public string ErrorMessage { get; private set; } = string.Empty;

		public string? InitData { get; private set; }

		private readonly PolyContext _context;

		private readonly IMainConfig _config;

		private readonly TournamentsComponent _tournaments;

		public UpcomingTournamentsModel(PolyContext context, IMainConfig config, TournamentsComponent tournaments)
		{
			_context = context;
			_config = config;
			_tournaments = tournaments;
		}

		public async Task<IActionResult> OnGetAsync()
		{
			var initDataString = Request.Query["initData"].ToString();
			InitData = initDataString;

			if (string.IsNullOrEmpty(initDataString))
			{
				ErrorMessage = "Эта страница доступна только через Telegram";
				IsAuthenticated = false;
				return Page();
			}

			var initData = HttpUtility.ParseQueryString(initDataString);
			if (!ValidateTelegramWebAppData(initData))
			{
				ErrorMessage = "Неверная подпись данных";
				IsAuthenticated = false;
				return Page();
			}

			var userId = ExtractUserId(initData);
			if (userId == 0)
			{
				ErrorMessage = "Не удалось определить пользователя";
				IsAuthenticated = false;
				return Page();
			}

			IsAuthenticated = true;
			var student = await _context.Students.FirstOrDefaultAsync(s => s.TelegramId == userId);

			if (student == null)
			{
				ErrorMessage = "Вы не зарегистрированы как студент";
				return Page();
			}

			foreach (var tournamentInfo in _tournaments.TournamentsList.Where(t => !t.Tournament.IsFinished && t.Tournament.StartDate > DateTime.Now))
			{
				var tournament = tournamentInfo.Tournament;
				Tournaments.Add(new UpcomingTournamentInfo
				{
					Id = tournament.ID,
					Name = tournament.FullName,
					StartDate = tournament.StartDate,
					Type = "Arena",
					Duration = tournament.Minutes,
					TimeControl = $"{tournament.Clock.Limit / 60}+{tournament.Clock.Increment}"
				});
			}

			foreach (var tournamentInfo in _tournaments.SwissTournamentsList.Where(t => t.Tournament.Status != "finished" && t.Tournament.Started > DateTime.Now))
			{
				var tournament = tournamentInfo.Tournament;
				Tournaments.Add(new UpcomingTournamentInfo
				{
					Id = tournament.ID,
					Name = tournament.Name,
					StartDate = tournament.Started,
					Type = "Swiss",
					Duration = 0,
					TimeControl = $"{tournament.Clock.Limit / 60}+{tournament.Clock.Increment}"
				});
			}

			Tournaments = [.. Tournaments.OrderBy(t => t.StartDate)];

			return Page();
		}

		private bool ValidateTelegramWebAppData(NameValueCollection initData)
		{
			var hash = initData["hash"];
			if (string.IsNullOrEmpty(hash))
				return false;

			initData.Remove("hash");

			var dataCheckString = string.Join("\n",
				initData.AllKeys.OrderBy(k => k).Select(k => $"{k}={initData[k]}")
			);

			var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"),
				Encoding.UTF8.GetBytes(_config.Telegram.TelegramToken));

			var computedHash = HMACSHA256.HashData(secretKey,
				Encoding.UTF8.GetBytes(dataCheckString));

			var hashString = Convert.ToHexString(computedHash).ToLower();
			return hashString == hash;
		}

		private long ExtractUserId(NameValueCollection initData)
		{
			var userJson = initData["user"];
			if (string.IsNullOrEmpty(userJson))
				return 0;

			var userObj = JsonDocument.Parse(userJson);
			if (userObj.RootElement.TryGetProperty("id", out var idProp))
				return idProp.GetInt64();

			return 0;
		}
	}
}
