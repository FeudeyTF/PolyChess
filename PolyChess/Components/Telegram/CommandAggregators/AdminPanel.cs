using System.Text;
using PolyChess.Components.Data;
using PolyChess.Components.Data.Tables;
using PolyChess.Components.Telegram.Buttons;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.Core.Logging;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using PolyChess.Core.Telegram.Messages.Discrete;
using PolyChess.Core.Telegram.Messages.Discrete.Messages;
using PolyChess.LichessAPI.Clients;
using PolyChess.LichessAPI.Types.Arena;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Components.Telegram.CommandAggregators
{
	internal class AdminPanel : TelegramCommandAggregator
	{
		public string TempPath { get; private set; }

		private readonly PolyContext _polyContext;

		private readonly LichessClient _lichessClient;

		private readonly ILogger _logger;

		private readonly TournamentsComponent _tournaments;

		private readonly IMainConfig _mainConfig;

		private readonly DiscreteMessagesProvider _discreteMessagesProvider;

		public AdminPanel(ITelegramProvider telegramProvider, TournamentsComponent tournaments, IMainConfig config, PolyContext polyContext, ILogger logger, LichessClient client)
		{
			_polyContext = polyContext;
			_logger = logger;
			_lichessClient = client;
			_discreteMessagesProvider = new(telegramProvider);
			_mainConfig = config;
			_tournaments = tournaments;
			TempPath = Path.Combine(Environment.CurrentDirectory, "tmp");
		}

		[TelegramCommand("panel", "Выводит панель администратора", IsHidden = true, IsAdmin = true)]
		private async Task Panel(TelegramCommandExecutionContext ctx)
		{
			TelegramMessageBuilder message = "🛠 Добро пожаловать в панель администратора.";

			message.AddButton(
				new InlineKeyboardButton("🔄 Обновить турниры").WithData(nameof(UpdateTournaments))
			);

			message.AddButton(
				new InlineKeyboardButton("👥 Получить список студентов").WithData(nameof(GetStudentsList))
			);

			message.AddKeyboard(
			[
				new InlineKeyboardButton("➕ Добавить урок").WithData(nameof(AddLesson)),
				new InlineKeyboardButton("➕ Добавить посещение").WithData(nameof(AddAttendance)),
			]);

			message.AddButton(
				new InlineKeyboardButton("➕ Добавить студентов").WithData(nameof(AddStudents))
			);

			message.AddButton(
				new InlineKeyboardButton("Поиск студента").WithData(nameof(SearchStudent))
			);

			message.AddButton(
				new InlineKeyboardButton("💾 Сохранить турнир").WithData(nameof(SaveTournament))
			);

			message.AddButton(
				new InlineKeyboardButton("🤝 Результаты турнира").WithData(nameof(TournamentResult))
			);

			message.AddButton(
				new InlineKeyboardButton("Посмотреть посещения студента").WithData(nameof(ViewStudentAttendance))
			);

			await ctx.ReplyAsync(message);
		}

		[TelegramButton(nameof(GetStudentsList))]
		private async Task GetStudentsList(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			if (!_polyContext.Students.Any())
			{
				await ctx.ReplyAsync("Студенты отсутствуют!");
				return;
			}

			await ctx.ReplyAsync("Началась сборка таблицы, это может занять некоторое время...");

			List<string> csv = [string.Join(',', ["Имя", "Фамилия", "Отчество", "Институт", "Курс", "Номер зачётки", "Группа", "Ник", "Рапид", "Блиц"])];
			foreach (var student in _polyContext.Students)
			{
				var lichessName = "Аккаунт не привязан";
				var rapidRating = "Отсутсвует";
				var blitzRating = "Отсутсвует";
				if (!string.IsNullOrEmpty(student.LichessId))
				{
					var lichess = await _lichessClient.GetUserAsync(student.LichessId);
					if (lichess != null)
					{
						lichessName = lichess.Username;
						if (lichess.Perfomance.TryGetValue("rapid", out var rapid))
							rapidRating = rapid.Rating.ToString();
						if (lichess.Perfomance.TryGetValue("blitz", out var blitz))
							blitzRating = blitz.Rating.ToString();
					}
				}
				var entry = string.Join(',',
				[
					student.Name,
					student.Surname,
					student.Patronymic,
					student.Institute,
					student.Year.ToString(),
					student.RecordBookId,
					student.Group,
					lichessName,
					rapidRating,
					blitzRating
				]);
				csv.Add(entry);
			}

			TelegramMessageBuilder message = "Таблица со всеми участниками секции в базе";
			Directory.CreateDirectory("Temp");
			var tempFilePath = Path.Combine("Temp", "students.csv");
			var tempFile = File.Create(tempFilePath);
			using (var streamWriter = new StreamWriter(tempFile))
			{
				foreach (var entry in csv)
					streamWriter.WriteLine(entry);
				streamWriter.Close();
			}
			using var stream = File.Open(tempFilePath, FileMode.Open);
			await ctx.ReplyAsync(message.WithFile(stream, "students.csv"));
		}

		[TelegramButton(nameof(AddLesson))]
		private async Task AddLesson(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите дату начала урока"),
					new TelegramMessageBuilder("Введите дату конца урока"),
					new TelegramMessageBuilder("Введите широту урока или введите -, чтобы взять стандартную"),
					new TelegramMessageBuilder("Введите долготу урока или введите -, чтобы взять стандартную")
				],
				HandleLessonsDataEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleLessonsDataEntered(DiscreteMessageEnteredArgs args)
			{
				if (args.Responses.Length != 4)
					return;

				if (!DateTime.TryParse(args.Responses[0].Text, out var startDate))
				{
					await args.ReplyAsync("Ошибка! Неверный формат даты начала урока.");
					return;
				}

				if (!DateTime.TryParse(args.Responses[1].Text, out var endDate))
				{
					await args.ReplyAsync("Ошибка! Неверный формат даты конца урока.");
					return;
				}

				float? latitude = default;
				float? longitude = default;

				var latitudeResponse = args.Responses[2].Text;
				if (!string.IsNullOrEmpty(latitudeResponse) && latitudeResponse != "-")
				{
					if (!float.TryParse(latitudeResponse, out var givenLatitude))
					{
						await args.ReplyAsync("Ошибка! Неверный формат широты.");
						return;
					}
					latitude = givenLatitude;
				}

				var longitudeResponse = args.Responses[3].Text;
				if (!string.IsNullOrEmpty(longitudeResponse) && longitudeResponse != "-")
				{
					if (!float.TryParse(longitudeResponse, out var givenLongitude))
					{
						await args.ReplyAsync("Ошибка! Неверный формат долготы.");
						return;
					}
					longitude = givenLongitude;
				}

				if (startDate >= endDate)
				{
					await args.ReplyAsync("Ошибка! Дата начала идёт после даты конца.");
					return;
				}

				latitude ??= _mainConfig.SchoolLocation.X;
				longitude ??= _mainConfig.SchoolLocation.Y;

				Lesson lesson = new()
				{
					StartDate = startDate,
					EndDate = endDate,
					Latitude = latitude.Value,
					Longitude = longitude.Value
				};

				_polyContext.Lessons.Add(lesson);
				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync($"Урок с <b>{startDate:g} до {endDate:g}</b> успешно добавлен!");
			}
		}

		[TelegramButton(nameof(AddAttendance))]
		private async Task AddAttendance(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите данные в формате списка Id урока,TelegramId студента")
				],
				HandleAttendancesEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleAttendancesEntered(DiscreteMessageEnteredArgs args)
			{
				List<string> errors = [];
				var response = args.Responses.First().Text;
				if (response == null)
				{
					await args.ReplyAsync("Вы не ввели текст!");
					return;
				}

				foreach (var entry in response.Split('\n'))
				{
					var data = entry.Split(',');
					if (data.Length != 2)
					{
						errors.Add($"Неверный формат данных: {entry}");
						continue;
					}

					if (int.TryParse(data[0], out var lessonId) && long.TryParse(data[1], out var telegramId))
					{
						var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == telegramId);
						var lesson = _polyContext.Lessons.FirstOrDefault(l => l.Id == lessonId);
						if (student == null)
						{
							errors.Add($"Студент с TelegramId {telegramId} не найден!");
							continue;
						}
						if (lesson == null)
						{
							errors.Add($"Урок с Id {lessonId} не найден!");
							continue;
						}

						Attendance attendance = new()
						{
							Lesson = lesson,
							Student = student
						};
						_polyContext.Attendances.Add(attendance);
					}
					else
						errors.Add($"Ошибка при разборе данных: {entry}");
				}

				await _polyContext.SaveChangesAsync();
				if (errors.Count > 0)
					await args.ReplyAsync($"Ошибки при добавлении:\n{string.Join('\n', errors)}");
				else
					await args.ReplyAsync($"Посещения успешно добавлены!");
			}
		}

		[TelegramButton(nameof(UpdateTournaments))]
		private async Task UpdateTournaments(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			if (_mainConfig.TeamsWithTournaments.Count > 0)
			{
				foreach (var teamId in _mainConfig.TeamsWithTournaments)
				{
					await ctx.ReplyAsync($"Началась загрузка турниров из {teamId}... Это может занять некоторое время");
					var updatedTournaments = await _tournaments.UpdateTournaments(teamId);
					if (updatedTournaments.Count > 0)
						await ctx.ReplyAsync($"Турниры {string.Join(", ", updatedTournaments.Select(t => "<b>" + t.name + "</b>"))} успешно добавлены!");
					else
						await ctx.ReplyAsync("Все турниры уже загружены! Обновления не требуется");
				}
			}
			else
				await ctx.ReplyAsync("Команда Политеха не найдена!");
		}

		[TelegramButton(nameof(AddStudents))]
		private async Task AddStudents(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите данные студентов в формате Имя,Фамилия,Отчество,Курс,Институт,Lichess,LichessToken,TelegramId")
				],
				HandleStudentsEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleStudentsEntered(DiscreteMessageEnteredArgs args)
			{
				var response = args.Responses.First().Text;
				if (response == null)
				{
					await args.ReplyAsync("Вы не ввели текст!");
					return;
				}
				List<(string student, string error)> skippedStudents = [];
				var students = response.Split('\n');
				foreach (var student in students)
				{
					var studentData = student.Split(',');
					if (studentData.Length != 10)
					{
						skippedStudents.Add((student, "Не введены все данные (для пустых оставьте пропуск). Имя,Фамилия,Отчество,Курс,Группа,Институт,Номер зачётки,Личесс,ЛичессТокен,Телеграм"));
						continue;
					}

					var surname = studentData[0];
					var name = studentData[1];
					var patronomic = studentData[2];
					var yearStr = studentData[3];
					var group = studentData[4];
					var institute = studentData[5];
					var recordBookId = studentData[6];
					var lichess = studentData[7];
					var lichessToken = studentData[8];
					var telegramIdStr = studentData[9];

					if (!int.TryParse(yearStr, out var year))
					{
						skippedStudents.Add((name, "Курс не является числом!"));
						continue;
					}

					long telegramId = 0;
					if (!string.IsNullOrEmpty(telegramIdStr) && !long.TryParse(telegramIdStr, out telegramId))
					{
						skippedStudents.Add((name, "TelegramId не является числом!"));
						continue;
					}

					bool isSkipped = false;
					foreach (var s in _polyContext.Students)
					{
						if (s.Name == name && s.Surname == surname && s.Patronymic == patronomic)
						{
							skippedStudents.Add((name, "Студент с таким именем уже существует!"));
							isSkipped = true;
						}
						else if (s.LichessId == lichess)
						{
							skippedStudents.Add((name, "Студент с таким аккаунтом Lichess уже существует!"));
							isSkipped = true;
						}
						else if (s.TelegramId == telegramId)
						{
							skippedStudents.Add((name, "Студент с таким аккаунтом Telegram уже существует!"));
							isSkipped = true;
						}
					}

					if (isSkipped)
						continue;

					Student studentEntry = new()
					{
						Name = name,
						Surname = surname,
						Patronymic = patronomic,
						Year = year,
						Group = group,
						RecordBookId = string.IsNullOrEmpty(recordBookId) ? null : recordBookId,
						TelegramId = telegramId == 0 ? default : telegramId,
						LichessId = string.IsNullOrEmpty(lichess) ? null : lichess,
						LichessToken = string.IsNullOrEmpty(lichessToken) ? null : lichessToken,
						Institute = institute
					};
					_polyContext.Students.Add(studentEntry);
				}
				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync($"Успешно добавлено {students.Length - skippedStudents.Count} студентов! Пропущенные студенты:\n{string.Join('\n', skippedStudents.Select(s => s.student + ": " + s.error))}");
			}
		}

		[TelegramButton(nameof(SearchStudent))]
		private async Task SearchStudent(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите имя студента или его телеграм id")
				],
				HandleNameOrTelegramEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleNameOrTelegramEntered(DiscreteMessageEnteredArgs args)
			{
				var response = args.Responses.First().Text;
				if (response == null)
				{
					await args.ReplyAsync("Вы не ввели текст!");
					return;
				}

				var foundedStudents = GetStudentByParameter(response);
				if (foundedStudents.Count == 0)
				{
					await args.ReplyAsync("Студенты не найдены!");
					return;
				}

				TelegramMessageBuilder replyMessage = $"Найдено {foundedStudents.Count} студентов:\n";
				foreach (var student in foundedStudents)
					replyMessage.Text += student.ToString();

				await args.ReplyAsync(replyMessage);
			}

			List<Student> GetStudentByParameter(string value)
			{
				List<Student> foundedStudents = [];
				if (long.TryParse(value, out var telegramId))
				{
					var studentByTelegram = _polyContext.Students.FirstOrDefault(s => s.TelegramId == telegramId);
					if (studentByTelegram != null)
						foundedStudents.Add(studentByTelegram);
				}
				else
				{
					foreach (var student in _polyContext.Students)
					{
						if (student.Name + " " + student.Surname + " " + student.Patronymic == value)
						{
							foundedStudents.Add(student);
							break;
						}

						if (student.Surname + " " + student.Name == value)
						{
							foundedStudents.Add(student);
							continue;
						}

						if (student.Surname.Contains(value, StringComparison.CurrentCultureIgnoreCase) ||
						   student.Name.Contains(value, StringComparison.CurrentCultureIgnoreCase) ||
						   student.Patronymic.Contains(value, StringComparison.CurrentCultureIgnoreCase))
						{
							foundedStudents.Add(student);
							continue;
						}
					}
				}

				return foundedStudents;
			}
		}

		[TelegramButton(nameof(SaveTournament))]
		private async Task SaveTournament(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите ссылку на турнир")
				],
				HandleTournamenLinkEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleTournamenLinkEntered(DiscreteMessageEnteredArgs args)
			{
				if (args.Responses.Length == 1)
				{
					var tournamentLink = args.Responses[0].Text;
					if (tournamentLink != null)
					{
						var splittedLink = tournamentLink.Split('/');
						if (splittedLink.Length > 1)
						{
							var type = splittedLink[^2];
							var id = splittedLink[^1];
							if (!string.IsNullOrEmpty(type.Trim()) && !string.IsNullOrEmpty(id.Trim()))
							{
								if (type == "tournament")
								{
									var tournament = await _tournaments.UpdateTournament(id);
									if (tournament != null)
										await args.ReplyAsync($"Турнир <b>{tournament.Tournament.FullName}</b> был сохранён!");
									else
										await args.ReplyAsync("Турнир не был найден!");
								}
								else if (type == "swiss")
								{
									var tournament = await _tournaments.UpdateSwissTournament(id);
									if (tournament != null)
										await args.ReplyAsync($"Турнир <b>{tournament.Tournament.Name}</b> был сохранён!");
									else
										await args.ReplyAsync("Турнир не был найден!");
								}
								else
									await args.ReplyAsync("Неправильная ссылка!");
							}
							else
								await args.ReplyAsync("Неправильная ссылка!");
						}
						else
							await args.ReplyAsync("Неправильная ссылка!");
					}
					else
						await args.ReplyAsync("Необходимо ввести ссылку на турнир!");
				}
			}
		}

		[TelegramButton(nameof(TournamentResult))]
		private async Task TournamentResult(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите ссылку на турнир"),
					new TelegramMessageBuilder("Введите тех, кого не нужно учитывать (разделять пробелами или запятой. Введите -, если все учитываются)")
				],
				HandleTournamentResultEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleTournamentResultEntered(DiscreteMessageEnteredArgs args)
			{
				if (args.Responses.Length == 2)
				{
					var tournamentLink = args.Responses[0].Text;
					if (tournamentLink != null)
					{
						List<string> exclude = new(_mainConfig.ClubTeamPlayers);
						var toExclude = args.Responses[1].Text;
						if (toExclude != null && toExclude.Trim() != "-")
						{
							var stringsToExclude = toExclude.Split(' ').Select(p => p.Split(','));
							foreach (var str in stringsToExclude)
								foreach (var str2 in str)
									if (!string.IsNullOrEmpty(str2.Trim()))
										exclude.Add(str2.Trim());
						}

						var splittedLink = tournamentLink.Split('/');
						if (splittedLink.Length > 1)
						{
							List<TelegramMessageBuilder> messages = [];
							var type = splittedLink[^2];
							var id = splittedLink[^1];
							if (!string.IsNullOrEmpty(type.Trim()) && !string.IsNullOrEmpty(id.Trim()))
							{
								if (type == "tournament")
									messages = await OnArenaResultEntered(id, exclude);
								else if (type == "swiss")
									messages = await OnSwissResultEntered(id, exclude);
								else
								{
									await args.ReplyAsync("Неправильная ссылка!");
									return;
								}

								foreach (var msg in messages)
									await args.ReplyAsync(msg);
							}
							else
								await args.ReplyAsync("Неправильная ссылка!");
						}
						else
							await args.ReplyAsync("Неправильная ссылка!");
					}
					else
						await args.ReplyAsync("Необходимо ввести ссылку на турнир!");
				}
			}

			async Task<List<TelegramMessageBuilder>> OnArenaResultEntered(string tournamentId, List<string> exclude)
			{
				List<TelegramMessageBuilder> result = [];
				var tournament = await _lichessClient.GetTournament(tournamentId);

				var directory = Path.Combine(Environment.CurrentDirectory, "Tournaments");
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);
				var filePath = Path.Combine(directory, tournamentId + ".txt");

				if (File.Exists(filePath))
				{
					List<SheetEntry> tournamentSheet = [];
					using (var file = File.OpenText(filePath))
					{
						tournamentSheet = await _lichessClient.GetTournamentSheet(file);
					}

					if (tournament != null && tournamentSheet != null)
					{
						tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !_mainConfig.InstitutesTeams.Contains(e.Team))).ToList();
						List<string> csv = ["Имя;Ник Lichess;Балл"];
						List<string> text = [
							$"Турнир <b>{tournament.FullName}</b>. Состоялся <b>{tournament.StartDate:g}</b>",
									$"Информация об участии в турнире"
						];

						var tournamentRating = _tournaments.GenerateTournamentRating(tournamentSheet);

						foreach (var divison in tournamentRating.Divisions)
						{
							text.Add($"Игроки дивизиона <b>{divison.Key}</b>:");
							foreach (var entry in divison.Value)
								text.Add($"<b> - {entry.Rank}) {entry.Username}</b>. Рейтинг: {entry.Rating}");
						}

						text.Add("");
						text.Add("<b>Остальной рейтинг и баллы за турнир:</b>");
						text.Add("");

						foreach (var entry in tournamentRating.Players)
						{
							if (entry.Score != -1)
								csv.Add($"{entry.Student?.Name};{entry.TournamentEntry.Username};{entry.Score}");
							text.Add($"<b>{entry.TournamentEntry.Rank}) {entry.TournamentEntry.Username}</b>, {(string.IsNullOrEmpty(entry.Student?.Name) ? "Без имени" : entry.Student?.Name)}. Балл: {(entry.Score == -1 ? "-" : entry.Score)}");
						}

						TelegramMessageBuilder message = "Файл с таблицей результатов";
						if (!Directory.Exists(TempPath))
							Directory.CreateDirectory(TempPath);
						var csvFilePath = Path.Combine(TempPath, tournament.ID + "result.csv");
						if (File.Exists(csvFilePath))
							File.Delete(csvFilePath);
						using (var streamWriter = new StreamWriter(File.Create(csvFilePath), Encoding.UTF8))
						{
							foreach (var entry in csv)
								streamWriter.WriteLine(entry);
							streamWriter.Close();
						}
						var stream = File.Open(csvFilePath, FileMode.Open);
						message.WithFile(stream, "Table.csv");
						result.Add(string.Join('\n', text));
						result.Add(message);
					}
					else
						result.Add("Турнир не был найден!");
				}
				else
					result.Add("Турнир не сохранён с помощью команды /savearena!");
				return result;
			}

			async Task<List<TelegramMessageBuilder>> OnSwissResultEntered(string tournamentId, List<string> exclude)
			{
				List<TelegramMessageBuilder> result = [];
				var tournament = await _lichessClient.GetSwissTournament(tournamentId);

				var directory = Path.Combine(Environment.CurrentDirectory, "SwissTournaments");
				if (!Directory.Exists(directory))
					Directory.CreateDirectory(directory);
				var filePath = Path.Combine(directory, tournamentId + ".txt");
				if (File.Exists(filePath))
				{
					var tournamentSheet = await _lichessClient.GetSwissTournamentSheet(File.OpenText(filePath));
					tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
					if (tournament != null && tournamentSheet != null)
					{
						List<string> csv = ["Имя;Ник Lichess;Балл"];
						List<string> text = [
							$"Турнир по швейцарской <b>{tournament.Name}</b>. Состоялся <b>{tournament.Started:g}</b>",
									$"Информация об участии в турнире"
						];

						var tournamentRating = _tournaments.GenerateTournamentRating(tournamentSheet);

						foreach (var division in tournamentRating.Divisions)
						{
							text.Add($"Игроки дивизиона <b>{division.Key}</b>:");
							foreach (var entry in division.Value)
								text.Add($"<b> - {entry.Rank}) {entry.Username}</b>. Рейтинг: {entry.Rating}");
						}

						text.Add("");
						text.Add("<b>Остальной рейтинг и баллы за турнир:</b>");
						text.Add("");

						foreach (var entry in tournamentRating.Players)
						{
							if (entry.Score != -1)
								csv.Add($"{entry.Student?.Name};{entry.TournamentEntry.Username};{entry.Score}");
							text.Add($"<b>{entry.TournamentEntry.Rank}) {entry.TournamentEntry.Username}</b>, {(string.IsNullOrEmpty(entry.Student?.Name) ? "Без имени" : entry.Student?.Name)}. Балл: {(entry.Score == -1 ? "-" : entry.Score)}");
						}

						TelegramMessageBuilder message = "Файл с таблицей результатов";
						if (!Directory.Exists(TempPath))
							Directory.CreateDirectory(TempPath);
						var csvFilePath = Path.Combine(TempPath, tournament.ID + "result.csv");
						if (File.Exists(csvFilePath))
							File.Delete(csvFilePath);
						using (var streamWriter = new StreamWriter(File.Create(csvFilePath), Encoding.UTF8))
						{
							foreach (var entry in csv)
								streamWriter.WriteLine(entry);
							streamWriter.Close();
						}
						var stream = File.Open(csvFilePath, FileMode.Open);
						message.WithFile(stream, "Table.csv");
						result.Add(string.Join('\n', text));
						result.Add(message);
					}
					else
						result.Add("Турнир не был найден!");
				}
				else
					result.Add("Турнир не сохранён с помощью команды /saveswiss!");

				return result;
			}
		}

		[TelegramButton(nameof(ViewStudentAttendance))]
		private async Task ViewStudentAttendance(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите имя студента  (Фамилия, Фамилия Имя, Имя, ФИО)"),
				],
				HandleStudentNameEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleStudentNameEntered(DiscreteMessageEnteredArgs args)
			{
				var text = args.Responses[0].Text;
				if (text == null)
				{
					await args.ReplyAsync("В сообщении должно быть имя студента!");
					return;
				}

				Student? student = default;
				var splittedName = text.Split(' ');
				if (splittedName.Length >= 3)
				{
					var surname = splittedName[0];
					var name = splittedName[1];
					var patronomic = splittedName[2];
					student = _polyContext.Students.Where(s => s.Name == name && s.Surname == surname && s.Patronymic == patronomic).FirstOrDefault();
				}
				else if (splittedName.Length == 2)
				{
					var surname = splittedName[0];
					var name = splittedName[1];
					var students = _polyContext.Students.Where(s => s.Name == name && s.Surname == surname);
					if (students.Count() > 1)
					{
						await args.ReplyAsync($"По введённой фамилии и имени были найдены студенты:\n{string.Join('\n', students.Select(s => s.Surname + " " + s.Name + " " + s.Patronymic))}");
						return;
					}
					student = students.FirstOrDefault();
				}
				else
				{
					var name = splittedName[0];
					var students = _polyContext.Students.Where(s => s.Name == name || s.Surname == name);
					if (students.Count() > 1)
					{
						await args.ReplyAsync($"По введённой фамилии и имени были найдены студенты:\n{string.Join('\n', students.Select(s => s.Surname + " " + s.Name + " " + s.Patronymic))}");
						return;
					}
					student = students.FirstOrDefault();
				}

				if (student == null)
				{
					await args.ReplyAsync("По вашему запросу не найдено ни одного студента!");
					return;
				}

				var attendace = _polyContext.Attendances.Where(a => a.Student.TelegramId == student.TelegramId);
				List<string> msg =
				[
					$"Посещаемость студента {student.Surname} {student.Name} {student.Patronymic}:"
				];

				if (!attendace.Any())
					msg.Add("Ни одного занятия не посещено!");

				foreach (var lesson in _polyContext.Lessons)
					if (lesson.StartDate < DateTime.Now)
						msg.Add($"Занятие с {lesson.StartDate:g} до {lesson.EndDate:g}: {(attendace.Any(a => a.Lesson.Id == lesson.Id) ? "Посещено" : "Не посещено")}. Занятие {(lesson.IsRequired ? "обязательно" : "не обязательно")} для посещения");

				await ctx.ReplyAsync(string.Join("\n", msg));
			}
		}
	}
}
