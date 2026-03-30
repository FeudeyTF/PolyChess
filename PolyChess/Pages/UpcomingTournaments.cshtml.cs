using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PolyChess.Components.Data;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.Pages.Services;

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

		public int RoundsCount { get; set; }

		public bool IsStartingSoon => StartDate > DateTime.Now && StartDate <= DateTime.Now.AddHours(2);

		public bool IsStartingToday => StartDate.Date == DateTime.Today;
	}

	internal class UpcomingTournamentsModel : PageModel
	{
		public List<UpcomingTournamentInfo> Tournaments { get; private set; } = [];

		public bool IsAuthenticated { get; private set; }

		public string ErrorMessage { get; private set; } = string.Empty;

		public string? InitData { get; private set; }

		private readonly PolyContext _context;

		private readonly TournamentsComponent _tournaments;

		private readonly TelegramWebAppValidator _validator;

		public UpcomingTournamentsModel(PolyContext context, IMainConfig config, TournamentsComponent tournaments)
		{
			_context = context;
			_tournaments = tournaments;
			_validator = new TelegramWebAppValidator(config);
		}

		public async Task<IActionResult> OnGetAsync()
		{
			var initDataString = Request.Query["initData"].ToString();
			InitData = initDataString;

			var validationResult = _validator.Validate(initDataString);

			if (!validationResult.IsValid)
			{
				ErrorMessage = validationResult.ErrorMessage;
				IsAuthenticated = false;
				return Page();
			}

			IsAuthenticated = true;
			var student = await _context.Students.FirstOrDefaultAsync(s => s.TelegramId == validationResult.UserId);

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
					TimeControl = $"{tournament.Clock.Limit / 60}+{tournament.Clock.Increment}",
					RoundsCount = tournament.RoundsNumber
				});
			}

			Tournaments = [.. Tournaments.OrderBy(t => t.StartDate)];

			return Page();
		}
	}
}
