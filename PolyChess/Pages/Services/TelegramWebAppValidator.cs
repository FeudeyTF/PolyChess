using PolyChess.Configuration;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace PolyChess.Pages.Services
{
	internal class TelegramWebAppValidator
	{
		private readonly IMainConfig _config;

		public int MaxAuthDateAgeSeconds { get; set; } = 86400;

		public TelegramWebAppValidator(IMainConfig config)
		{
			_config = config;
		}

		public class ValidationResult
		{
			public bool IsValid { get; set; }

			public string ErrorMessage { get; set; } = string.Empty;

			public long UserId { get; set; }

			public string? Username { get; set; }

			public string? FirstName { get; set; }

			public string? LastName { get; set; }

			public DateTime? AuthDate { get; set; }
		}

		public ValidationResult Validate(string? initDataString)
		{
			var result = new ValidationResult();

			if (string.IsNullOrEmpty(initDataString))
			{
				result.ErrorMessage = "Эта страница доступна только через Telegram";
				return result;
			}

			NameValueCollection initData;
			try
			{
				initData = HttpUtility.ParseQueryString(initDataString);
			}
			catch
			{
				result.ErrorMessage = "Некорректный формат данных";
				return result;
			}

			var hash = initData["hash"];
			if (string.IsNullOrEmpty(hash))
			{
				result.ErrorMessage = "Отсутствует подпись данных";
				return result;
			}

			if (MaxAuthDateAgeSeconds > 0)
			{
				var authDateStr = initData["auth_date"];
				if (!string.IsNullOrEmpty(authDateStr) && long.TryParse(authDateStr, out var authDateUnix))
				{
					var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateUnix).UtcDateTime;
					result.AuthDate = authDate;

					var age = DateTime.UtcNow - authDate;
					if (age.TotalSeconds > MaxAuthDateAgeSeconds)
					{
						result.ErrorMessage = "Сессия устарела. Пожалуйста, откройте приложение заново";
						return result;
					}

					if (age.TotalSeconds < -60)
					{
						result.ErrorMessage = "Некорректное время авторизации";
						return result;
					}
				}
			}

			var dataForValidation = HttpUtility.ParseQueryString(initDataString);
			dataForValidation.Remove("hash");

			var dataCheckString = string.Join("\n",
				dataForValidation.AllKeys
					.Where(k => k != null)
					.OrderBy(k => k)
					.Select(k => $"{k}={dataForValidation[k]}")
			);

			var secretKey = HMACSHA256.HashData(
				Encoding.UTF8.GetBytes("WebAppData"),
				Encoding.UTF8.GetBytes(_config.Telegram.TelegramToken)
			);

			var computedHash = HMACSHA256.HashData(
				secretKey,
				Encoding.UTF8.GetBytes(dataCheckString)
			);

			var hashString = Convert.ToHexString(computedHash).ToLower();

			if (!CryptographicOperations.FixedTimeEquals(
				Encoding.UTF8.GetBytes(hashString),
				Encoding.UTF8.GetBytes(hash.ToLower())
			))
			{
				result.ErrorMessage = "Неверная подпись данных";
				return result;
			}

			var userId = ExtractUserId(initData);
			if (userId == 0)
			{
				result.ErrorMessage = "Не удалось определить пользователя";
				return result;
			}

			result.IsValid = true;
			result.UserId = userId;
			ExtractUserDetails(initData, result);

			return result;
		}

		private static long ExtractUserId(NameValueCollection initData)
		{
			var userJson = initData["user"];
			if (string.IsNullOrEmpty(userJson))
				return 0;

			using var userObj = JsonDocument.Parse(userJson);
			if (userObj.RootElement.TryGetProperty("id", out var idProp))
				return idProp.GetInt64();

			return 0;
		}

		private static void ExtractUserDetails(NameValueCollection initData, ValidationResult result)
		{
			var userJson = initData["user"];
			if (string.IsNullOrEmpty(userJson))
				return;

			using var userObj = JsonDocument.Parse(userJson);
			var root = userObj.RootElement;

			if (root.TryGetProperty("username", out var usernameProp))
				result.Username = usernameProp.GetString();

			if (root.TryGetProperty("first_name", out var firstNameProp))
				result.FirstName = firstNameProp.GetString();

			if (root.TryGetProperty("last_name", out var lastNameProp))
				result.LastName = lastNameProp.GetString();
		}
	}
}
