using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PolyChess.Components.Data;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.LichessAPI.Types.Arena;
using PolyChess.LichessAPI.Types.Swiss;
using PolyChess.Pages.Services;

namespace PolyChess.Pages
{
	internal class TournamentDisplayInfo
	{
		public string Id { get; set; } = string.Empty;

		public string Name { get; set; } = string.Empty;

		public DateTime Date { get; set; }

		public string Type { get; set; } = string.Empty;

		public int PlayersCount { get; set; }

		public int? StudentScore { get; set; }

		public int? StudentRank { get; set; }

		public int? StudentPerformance { get; set; }

		public int? StudentRating { get; set; }

		public int? GamesPlayed { get; set; }

		public bool Participated { get; set; }

		public int ClubPoints { get; set; }
	}

	internal class TournamentsModel : PageModel
	{
		public List<TournamentDisplayInfo> Tournaments { get; private set; } = [];

		public bool IsAuthenticated { get; private set; }

		public string ErrorMessage { get; private set; } = string.Empty;

		public string? InitData { get; private set; }

		public string? StudentLichessId { get; private set; }

		public int TotalClubPoints { get; private set; }

		private readonly PolyContext _context;

		private readonly TournamentsComponent _tournaments;

		private readonly TelegramWebAppValidator _validator;

		private readonly IMainConfig _config;

		public TournamentsModel(PolyContext context, IMainConfig config, TournamentsComponent tournaments)
		{
			_context = context;
			_config = config;
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

			StudentLichessId = student.LichessId?.ToLower();

			foreach (var tournamentInfo in _tournaments.TournamentsList.Where(t => t.Tournament.IsFinished))
			{
				var tournament = tournamentInfo.Tournament;
				var displayInfo = new TournamentDisplayInfo
				{
					Id = tournament.ID,
					Name = tournament.FullName,
					Date = tournament.StartDate,
					Type = "Arena",
					PlayersCount = tournament.PlayersNumber
				};

				if (!string.IsNullOrEmpty(StudentLichessId))
				{
					var playerEntry = tournamentInfo.Rating.Players
						.FirstOrDefault(p => p.Student?.LichessId?.ToLower() == StudentLichessId);

					if (playerEntry != null)
					{
						displayInfo.Participated = true;
						if (_config.TournamentRules.TryGetValue(tournamentInfo.Tournament.ID, out var rule))
						{
							if (playerEntry.Score == 1)
								displayInfo.ClubPoints += rule.PointsForWinning;
							else if (playerEntry.Score == 0)
								displayInfo.ClubPoints += rule.PointsForBeing;
						}
						else
						{
							if (playerEntry.Score == 1)
								displayInfo.ClubPoints += TournamentScoreRule.DefaultWinningPoints;
							else if (playerEntry.Score == 0)
								displayInfo.ClubPoints += TournamentScoreRule.DefaultBeingPoints;
						}

						if (playerEntry.TournamentEntry is SheetEntry arenaEntry)
						{
							displayInfo.StudentScore = arenaEntry.Score;
							displayInfo.StudentPerformance = arenaEntry.Performance;
							displayInfo.StudentRating = arenaEntry.Rating;
							displayInfo.StudentRank = arenaEntry.Rank;

							if (arenaEntry.Sheet != null)
							{
								displayInfo.GamesPlayed = arenaEntry.Sheet.Scores.Length;
							}
						}

						if (!displayInfo.StudentRank.HasValue)
						{
							var standing = tournament.Standing.Players
								.FirstOrDefault(p => p.Name.Equals(StudentLichessId, StringComparison.OrdinalIgnoreCase));
							displayInfo.StudentRank = standing?.Rank;
						}
					}
				}

				Tournaments.Add(displayInfo);
			}

			foreach (var tournamentInfo in _tournaments.SwissTournamentsList.Where(t => t.Tournament.Status == "finished"))
			{
				var tournament = tournamentInfo.Tournament;
				var displayInfo = new TournamentDisplayInfo
				{
					Id = tournament.ID,
					Name = tournament.Name,
					Date = tournament.Started,
					Type = "Swiss",
					PlayersCount = tournament.PlayersNumber
				};

				if (!string.IsNullOrEmpty(StudentLichessId))
				{
					var playerEntry = tournamentInfo.Rating.Players
						.FirstOrDefault(p => p.Student?.LichessId?.ToLower() == StudentLichessId);

					if (playerEntry != null)
					{
						displayInfo.Participated = true;
						if (_config.TournamentRules.TryGetValue(tournamentInfo.Tournament.ID, out var rule))
						{
							if (playerEntry.Score == 1)
								displayInfo.ClubPoints += rule.PointsForWinning;
							else if (playerEntry.Score == 0)
								displayInfo.ClubPoints += rule.PointsForBeing;
						}
						else
						{
							if (playerEntry.Score == 1)
								displayInfo.ClubPoints += TournamentScoreRule.DefaultWinningPoints;
							else if (playerEntry.Score == 0)
								displayInfo.ClubPoints += TournamentScoreRule.DefaultBeingPoints;
						}

						if (playerEntry.TournamentEntry is SwissSheetEntry swissEntry)
						{
							displayInfo.StudentScore = (int)swissEntry.Points;
							displayInfo.StudentPerformance = swissEntry.Performance;
							displayInfo.StudentRating = swissEntry.Rating;
							displayInfo.StudentRank = swissEntry.Rank;
							displayInfo.GamesPlayed = tournament.RoundsNumber;
						}
					}
				}

				Tournaments.Add(displayInfo);
			}

			Tournaments = [.. Tournaments.OrderByDescending(t => t.Date)];
			TotalClubPoints = Tournaments.Where(t => t.Participated).Sum(t => t.ClubPoints);

			return Page();
		}
	}
}
