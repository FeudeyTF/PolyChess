using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PolyChess.Components.Data;
using PolyChess.Configuration;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

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
	}

	internal class IndexModel : PageModel
	{
		public StudentProfile? CurrentStudent { get; private set; }

		public int TotalLessons { get; private set; }

		public int AttendedLessons { get; private set; }

		public int RequiredLessons { get; private set; }

		public int RequiredTournaments { get; private set; }

		public bool IsAuthenticated { get; private set; }

		public string ErrorMessage { get; private set; } = string.Empty;

		private readonly PolyContext _context;

		private readonly IMainConfig _config;

		public IndexModel(PolyContext context, IMainConfig config)
		{
			_context = context;
			_config = config;
		}

		public async Task<IActionResult> OnGetAsync()
		{
			var initDataString = Request.Query["initData"].ToString();

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

			CurrentStudent = new StudentProfile
			{
				FullName = $"{student.Surname} {student.Name} {student.Patronymic}",
				Institute = student.Institute,
				Year = (int)student.Year,
				Group = student.Group,
				RecordBookId = student.RecordBookId,
				LichessId = student.LichessId,
				CreativeTaskCompleted = student.CreativeTaskCompleted,
				TournamentScore = student.AdditionalTournamentsScore
			};

			TotalLessons = await _context.Lessons
				.CountAsync(l => l.StartDate < DateTime.Now);

			RequiredLessons = await _context.Lessons
				.CountAsync(l => l.IsRequired && l.StartDate < DateTime.Now);

			AttendedLessons = await _context.Attendances
				.CountAsync(a => a.Student.Id == student.Id && a.Lesson.StartDate < DateTime.Now);

			RequiredTournaments = _config.Test.RequiredTournamentsCount;

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
