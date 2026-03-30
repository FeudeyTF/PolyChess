using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PolyChess.Components.Data;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.LichessAPI.Clients.Authorized;
using PolyChess.Pages.Services;

namespace PolyChess.Pages
{
	internal class StudentProfile
	{
		public string FullName { get; set; } = string.Empty;

		public string Institute { get; set; } = string.Empty;

		public int Year { get; set; }

		public string Group { get; set; } = string.Empty;

		public string? RecordBookId { get; set; }

		public string? LichessId { get; set; }

		public bool CreativeTaskCompleted { get; set; }

		public int TournamentScore { get; set; }

		public int ParticipatedTournamentsCount { get; set; }

		public bool HasLichessToken { get; set; }
	}

	internal class LessonInfo
	{
		public int Id { get; set; }

		public DateTime Date { get; set; }

		public bool IsRequired { get; set; }

		public bool IsAttended { get; set; }
	}

	internal class PuzzlesInfo
	{
		public bool IsLoaded { get; set; }

		public string? ErrorMessage { get; set; }

		public int FirstWins { get; set; }

		public int ReplayWins { get; set; }

		public int PuzzleRating { get; set; }

		public int Performance { get; set; }

		public int DaysPeriod { get; set; }
	}

	internal class IndexModel : PageModel
	{
		public StudentProfile? CurrentStudent { get; private set; }

		public PuzzlesInfo Puzzles { get; private set; } = new();

		public List<LessonInfo> LessonsList { get; private set; } = [];

		public int TotalLessons { get; private set; }

		public int AttendedLessons { get; private set; }

		public int RequiredLessons { get; private set; }

		public int RequiredTournaments { get; private set; }

		public int RequiredPuzzles { get; private set; }

		public bool IsAuthenticated { get; private set; }

		public string ErrorMessage { get; private set; } = string.Empty;

		public string? InitData { get; private set; }

		private readonly PolyContext _context;

		private readonly IMainConfig _config;

		private readonly TournamentsComponent _tournaments;

		private readonly TelegramWebAppValidator _validator;

		public IndexModel(PolyContext context, IMainConfig config, TournamentsComponent tournaments)
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

			var participatedTournaments = 0;

			var lichessId = student.LichessId?.ToLower();

			if (!string.IsNullOrEmpty(lichessId))
			{
				participatedTournaments += _tournaments.TournamentsList
					.Count(t => t.Rating.Players.Any(p =>
						p.Student?.LichessId?.ToLower() == lichessId && p.Score >= 0));

				participatedTournaments += _tournaments.SwissTournamentsList
					.Count(t => t.Rating.Players.Any(p =>
						p.Student?.LichessId?.ToLower() == lichessId && p.Score >= 0));
			}

			CurrentStudent = new StudentProfile
			{
				FullName = $"{student.Surname} {student.Name} {student.Patronymic}".Trim(),
				Institute = student.Institute,
				Year = (int)student.Year,
				Group = student.Group,
				RecordBookId = student.RecordBookId,
				LichessId = student.LichessId,
				CreativeTaskCompleted = student.CreativeTaskCompleted,
				TournamentScore = participatedTournaments + student.AdditionalTournamentsScore,
				ParticipatedTournamentsCount = participatedTournaments,
				HasLichessToken = !string.IsNullOrEmpty(student.LichessToken)
			};

			TotalLessons = await _context.Lessons
				.CountAsync(l => l.StartDate < DateTime.Now);

			RequiredLessons = _config.Test.RequiredVisitedLessonsPercent;

			AttendedLessons = await _context.Attendances
				.CountAsync(a => a.Student.Id == student.Id && a.Lesson.StartDate < DateTime.Now);

			var attendedLessonIds = await _context.Attendances
				.Where(a => a.Student.Id == student.Id)
				.Select(a => a.Lesson.Id)
				.ToListAsync();

			LessonsList = await _context.Lessons
				.Where(l => l.StartDate < DateTime.Now)
				.OrderByDescending(l => l.StartDate)
				.Select(l => new LessonInfo
				{
					Id = l.Id,
					Date = l.StartDate,
					IsRequired = l.IsRequired,
					IsAttended = attendedLessonIds.Contains(l.Id)
				})
				.ToListAsync();

			RequiredTournaments = _config.Test.RequiredTournamentsCount;
			RequiredPuzzles = _config.Test.RequiredPuzzlesSolved;

			Puzzles = await LoadPuzzlesDataAsync(student.LichessId, student.LichessToken);

			return Page();
		}

		private async Task<PuzzlesInfo> LoadPuzzlesDataAsync(string? lichessId, string? lichessToken)
		{
			PuzzlesInfo info = new() { DaysPeriod = (int)(DateTime.Now - _config.SemesterStartDate).TotalDays };

			if (string.IsNullOrEmpty(lichessId))
			{
				info.ErrorMessage = "Аккаунт Lichess не привязан к профилю";
				return info;
			}

			if (string.IsNullOrEmpty(lichessToken))
			{
				info.ErrorMessage = "Токен Lichess не настроен. Обратитесь к преподавателю для привязки";
				return info;
			}

			try
			{
				LichessAuthorizedClient client = new(lichessToken);

				var dashboard = await client.GetPuzzleDashboard(info.DaysPeriod);

				if (dashboard == null)
				{
					info.ErrorMessage = "Не удалось загрузить данные. Возможно, токен недействителен";
					return info;
				}

				info.IsLoaded = true;
				info.FirstWins = dashboard.Global.FirstWins;
				info.ReplayWins = dashboard.Global.ReplayWins;
				info.PuzzleRating = dashboard.Global.PuzzleRatingAvg;
				info.Performance = dashboard.Global.Performance;

				return info;
			}
			catch (HttpRequestException ex)
			{
				if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					info.ErrorMessage = "Токен Lichess недействителен или отозван";
				}
				else if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
				{
					info.ErrorMessage = "Доступ запрещён. Проверьте права токена";
				}
				else if (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
				{
					info.ErrorMessage = "Превышен лимит запросов к Lichess. Попробуйте позже";
				}
				else
				{
					info.ErrorMessage = "Ошибка соединения с Lichess. Попробуйте позже";
				}
				return info;
			}
			catch (Exception)
			{
				info.ErrorMessage = "Произошла ошибка при загрузке данных";
				return info;
			}
		}
	}
}
