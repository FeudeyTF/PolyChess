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
using PolyChess.LichessAPI.Clients.Authorized;
using PolyChess.LichessAPI.Types.Arena;
using Telegram.Bot.Types;
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

		private readonly PuzzlesComponent _puzzles;

		private readonly IMainConfig _mainConfig;

		private readonly DiscreteMessagesProvider _discreteMessagesProvider;

		public AdminPanel(ITelegramProvider telegramProvider, TournamentsComponent tournaments, PuzzlesComponent puzzlesComponent, IMainConfig config, PolyContext polyContext, ILogger logger, LichessClient client)
		{
			_polyContext = polyContext;
			_logger = logger;
			_lichessClient = client;
			_discreteMessagesProvider = new(telegramProvider);
			_mainConfig = config;
			_tournaments = tournaments;
			_puzzles = puzzlesComponent;
			TempPath = Path.Combine(Environment.CurrentDirectory, "tmp");
		}

		[TelegramCommand("panel", "Выводит панель администратора", IsHidden = true, IsAdmin = true)]
		private async Task Panel(TelegramCommandExecutionContext ctx)
		{
			TelegramMessageBuilder message = "🛠 Добро пожаловать в панель администратора.";

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
				new InlineKeyboardButton("Посмотреть посещения студента").WithData(nameof(ViewStudentAttendance))
			);

			message.AddKeyboard([
				new InlineKeyboardButton("Добавить задачу для урока").WithData(nameof(AddPuzzle)),
				new InlineKeyboardButton("Убрать задачу для урока").WithData(nameof(RemovePuzzle))
			]);

			message.AddKeyboard([
				new InlineKeyboardButton("Посмотреть существующие задания").WithData(nameof(ShowPuzzles)),
				new InlineKeyboardButton("Поставить задачу для урока").WithData(nameof(SetPuzzle))
			]);

			message.AddButton(
				new InlineKeyboardButton("Посмотреть ответы на текущее задание").WithData(nameof(ShowStudentsSolvedPuzzle))
			);

			message.AddKeyboard([
				new InlineKeyboardButton("Создать запись FAQ").WithData(nameof(AddFaq)),
				new InlineKeyboardButton("Удалить запись FAQ").WithData(nameof(RemoveFaq))
			]);

			message.AddKeyboard([
				new InlineKeyboardButton("Добавить полезную ссылку").WithData(nameof(AddHelp)),
				new InlineKeyboardButton("Удалить полезную ссылку").WithData(nameof(RemoveHelp))
			]);

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

			List<string> csv = [string.Join(',', ["Имя", "Фамилия", "Отчество", "Институт", "Курс", "Номер зачётки", "Группа", "Ник", "Рапид", "Блиц", "Посещено занятий", "Задачи", "Турниры (0)", "Турниры (1)", "Турниры (Доп.)"])];
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
				var attendaces = _polyContext.Attendances.Count(a => a.Student.Id == student.Id && a.Lesson.StartDate < DateTime.Now);

				var puzzles = "Нет данных";
				if (!string.IsNullOrEmpty(student.LichessToken))
				{
					LichessAuthorizedClient lichesAuthUser = new(student.LichessToken);
					var puzzleDashboard = await lichesAuthUser.GetPuzzleDashboard((int)(DateTime.Now - _mainConfig.SemesterStartDate).TotalDays);
					if (puzzleDashboard != null)
						puzzles = puzzleDashboard.Global.FirstWins.ToString();
				}

				var oneScoreTournaments = 0;
				var zeroScoreTournaments = 0;
				if (!string.IsNullOrEmpty(student.LichessId))
				{
					foreach (var tournament in _tournaments.TournamentsList)
						if (tournament.Tournament.StartDate < DateTime.UtcNow)
							foreach (var player in tournament.Rating.Players)
								if (player.Student != null && player.Student.TelegramId == student.TelegramId && player.Score > -1)
								{
									if (_mainConfig.TournamentRules.TryGetValue(tournament.Tournament.ID, out var rule))
									{
										if (player.Score == 1)
											oneScoreTournaments += rule.PointsForWinning;
										else if (player.Score == 0)
											zeroScoreTournaments += rule.PointsForBeing;
									}
									else
									{
										if (player.Score == 1)
											oneScoreTournaments += TournamentScoreRule.DefaultWinningPoints;
										else if (player.Score == 0)
											zeroScoreTournaments += TournamentScoreRule.DefaultBeingPoints;
									}
									break;
								}

					foreach (var tournament in _tournaments.SwissTournamentsList)
						if (tournament.Tournament.Started < DateTime.UtcNow)
							foreach (var player in tournament.Rating.Players)
								if (player.Student != null && player.Student.TelegramId == student.TelegramId && player.Score > -1)
								{
									if (_mainConfig.TournamentRules.TryGetValue(tournament.Tournament.ID, out var rule))
									{
										if (player.Score == 1)
											oneScoreTournaments += rule.PointsForWinning;
										else if (player.Score == 0)
											zeroScoreTournaments += rule.PointsForBeing;
									}
									else
									{
										if (player.Score == 1)
											oneScoreTournaments += TournamentScoreRule.DefaultWinningPoints;
										else if (player.Score == 0)
											zeroScoreTournaments += TournamentScoreRule.DefaultBeingPoints;
									}
									break;
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
					blitzRating,
					attendaces,
					puzzles,
					zeroScoreTournaments,
					oneScoreTournaments,
					_polyContext.TournamentEntries.Where(t => t.Student == student).Sum(t => t.Score)
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
					new TelegramMessageBuilder("Введите долготу урока или введите -, чтобы взять стандартную"),
					new TelegramMessageBuilder("Урок обязательный? (- - да, иначе - нет)")
				],
				HandleLessonsDataEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleLessonsDataEntered(DiscreteMessageEnteredArgs args)
			{
				if (args.Responses.Length != 5)
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

				var isRequiredResponse = args.Responses[4].Text;

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
					Longitude = longitude.Value,
					IsRequired = isRequiredResponse == "-" ? true : false
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
					new TelegramMessageBuilder("Введите дату урока (не обязательно точно)"),
					new TelegramMessageBuilder("Введите имя студента (ФИО, Фамилия Имя, Имя)")
				],
				HandleAttendancesEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleAttendancesEntered(DiscreteMessageEnteredArgs args)
			{
				var studentName = args.Responses[1].Text;
				if (studentName == null)
				{
					await args.ReplyAsync("Вы не ввели имя студента!");
					return;
				}

				var students = GetStudentsByIdentifier(studentName);
				if (students.Count() > 1)
				{
					await args.ReplyAsync($"По введённой фамилии и имени были найдены студенты:\n{string.Join('\n', students.Select(s => s.Surname + " " + s.Name + " " + s.Patronymic))}");
					return;
				}

				if (students.Count == 0)
				{
					await args.ReplyAsync("По вашему запросу не найдено ни одного студента!");
					return;
				}

				if (!DateTime.TryParse(args.Responses[0].Text, out var lessonDate))
				{
					await args.ReplyAsync("Вы неправильно ввели дату урока!");
					return;
				}

				var lesson = _polyContext.Lessons.FirstOrDefault(l => l.StartDate < lessonDate && lessonDate < l.EndDate);
				var student = students.First();

				if (lesson == null)
				{
					await args.ReplyAsync($"Урок в '{lessonDate}' не проводился!");
					return;
				}

				if (_polyContext.Attendances.Any(a => a.Lesson.Id == lesson.Id && a.Student.Id == student.Id))
				{
					await args.ReplyAsync($"Студент '{student.Name} {student.Surname} {student.Patronymic}' уже отмечен на урока с '{lesson.StartDate}' до '{lesson.EndDate}'");
					return;
				}

				_polyContext.Attendances.Add(new Attendance()
				{
					Lesson = lesson,
					Student = student
				});

				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync($"Студент '{student.Surname} {student.Name} {student.Patronymic}' был успешно отмечен на уроке с {lesson.StartDate} до {lesson.EndDate}");
			}
		}

		[TelegramButton(nameof(AddStudents))]
		private async Task AddStudents(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите данные студентов в формате Имя,Фамилия,Отчество,Курс,Группа,Институт,Номер зачётки,Личесс,ЛичессТокен,Телеграм")
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

					if (!long.TryParse(telegramIdStr, out var telegramId))
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

				var students = GetStudentsByIdentifier(text);
				if (students.Count() > 1)
				{
					await args.ReplyAsync($"По введённой фамилии и имени были найдены студенты:\n{string.Join('\n', students.Select(s => s.Surname + " " + s.Name + " " + s.Patronymic))}");
					return;
				}

				if (students.Count == 0)
				{
					await args.ReplyAsync("По вашему запросу не найдено ни одного студента!");
					return;
				}

				var student = students.First();

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

		private List<Student> GetStudentsByIdentifier(string text)
		{
			List<Student> students = [];
			var splittedName = text.Split(' ');
			if (splittedName.Length >= 3)
			{
				var surname = splittedName[0];
				var name = splittedName[1];
				var patronomic = splittedName[2];
				students.AddRange(_polyContext.Students.Where(s => s.Name == name && s.Surname == surname && s.Patronymic == patronomic));
			}
			else if (splittedName.Length == 2)
			{
				var surname = splittedName[0];
				var name = splittedName[1];
				students.AddRange(_polyContext.Students.Where(s => s.Name == name && s.Surname == surname));
			}
			else
			{
				var name = splittedName[0];
				students.AddRange(_polyContext.Students.Where(s => s.Name == name || s.Surname == name || (!string.IsNullOrEmpty(s.LichessId) && s.LichessId == name)));
			}

			return students;
		}

		[TelegramButton(nameof(AddPuzzle))]
		private async Task AddPuzzle(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите название задания"),
					new TelegramMessageBuilder("Введите вопрос к заданию"),
					new TelegramMessageBuilder("Пришлите картинку, которая будет отображена в задании"),
					new TelegramMessageBuilder("Введите возможные ответы на задачу. Разделить ответы нужно через ;"),
					new TelegramMessageBuilder("Введите правильный ответ на задачу")
				],
				HandlePuzzleInfoEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandlePuzzleInfoEntered(DiscreteMessageEnteredArgs args)
			{
				var name = args.Responses[0].Text;
				var question = args.Responses[1].Text;
				var fileId = args.Responses[2].Document?.FileId;
				var answersString = args.Responses[3].Text;
				var correctAnswer = args.Responses[4].Text;

				if (string.IsNullOrEmpty(name))
				{
					await args.ReplyAsync("Вы не ввели имя задания!");
					return;
				}

				if (string.IsNullOrEmpty(question))
				{
					await args.ReplyAsync("Вы не ввели вопрос задания!");
					return;
				}

				if (string.IsNullOrEmpty(answersString))
				{
					await args.ReplyAsync("Вы не ввели возможные варианты ответа для задания!");
					return;
				}

				if (string.IsNullOrEmpty(correctAnswer))
				{
					await args.ReplyAsync("Вы не ввели правильный ответ на задание!");
					return;
				}

				var answers = answersString.Split(';').Where(s => !string.IsNullOrEmpty(s.Trim()));
				if (!answers.Contains(correctAnswer))
				{
					await args.ReplyAsync($"Правильный ответ '{correctAnswer}' не находится в списке с ответами '{string.Join(", ", answers)}'!");
					return;
				}

				Puzzle puzzle = new()
				{
					Name = name,
					Question = question,
					ImageFilePath = fileId,
					Answers = [.. answers],
					CorrectAnswer = correctAnswer
				};

				await _puzzles.AddPuzzle(puzzle);
				await args.ReplyAsync($"Задание {name} было успешно добавлено! Вопрос: {question}, Ответы: {string.Join(", ", answers)}, Правильный ответ: {correctAnswer}");
			}
		}

		[TelegramButton(nameof(SetPuzzle))]
		private async Task SetPuzzle(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;
			List<TelegramMessageBuilder> messages = [];
			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите название задания")
				],
				HandlePuzzleInfoEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandlePuzzleInfoEntered(DiscreteMessageEnteredArgs args)
			{
				var name = args.Responses[0].Text;
				if (name == null)
				{
					await args.ReplyAsync("Вы не указали имя задания!");
					return;
				}

				var puzzle = _puzzles.SetCurrentPuzzle(name);
				if (puzzle == null)
				{
					await args.ReplyAsync($"Пазл с именем '{name}' не был найден!");
					return;
				}

				await args.ReplyAsync($"Задание '{puzzle.Name}' был успешно установлено, как текущее задание");
				var now = DateTime.Now;
				var lesson = _polyContext.Lessons.FirstOrDefault(l => l.StartDate <= now && l.EndDate >= now);
				if (lesson == null)
				{
					await args.ReplyAsync("Задание не было отправлено студентам, так как сейчас не проходит занятие!");
					return;
				}

				var students = _polyContext.Attendances.Where(a => a.Lesson.Id == lesson.Id).Select(a => a.Student);

				List<string> msg = [
					"Появилось задание на текущий урок!",
					"",
					$"<b>{puzzle.Question}</b>"
				];

				TelegramMessageBuilder message = string.Join("\n", msg);
				if (puzzle.ImageFilePath != null)
					message.AddMedia(new InputMediaPhoto(new InputFileId(puzzle.ImageFilePath)));

				for (int i = 0; i < puzzle.Answers.Length; i++)
				{
					var answer = puzzle.Answers[i];
					InlineKeyboardButton button = new($"{i + 1}) {answer}");
					button.SetData(nameof(AnswerButtonCallback), ("Index", i));
					message.AddButton(button);
				}

				foreach (var student in students)
					await message.SendAsync(args.Client, args.ChatId, args.Token);

				await args.ReplyAsync($"Задание было отправлено всем студентам, которые отмечены на текущем уроке: {string.Join(", ", students.Select(s => s.Name + " " + s.Surname + " " + s.Patronymic))}.");
			}
		}

		[TelegramButton(nameof(AnswerButtonCallback))]
		private async Task AnswerButtonCallback(TelegramButtonExecutionContext ctx)
		{
			int index = ctx.GetNumber("Index");
			if (_puzzles.CurrentPuzzle == null)
			{
				await ctx.ReplyAsync("Ошибка: в данный момент нет задания");
				return;
			}

			if (index >= _puzzles.CurrentPuzzle.Answers.Length)
			{
				await ctx.ReplyAsync("Ошибка: такого ответа не существует");
				return;
			}

			var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == ctx.Query.From.Id);
			if (student == null)
			{
				await ctx.ReplyAsync("Вы не студент СПбПУ!");
				return;
			}

			if (_puzzles.StudentAnswers.ContainsKey(student.Id))
			{
				await ctx.ReplyAsync("Вы уже ответили на задание!");
				return;
			}

			var answer = _puzzles.CurrentPuzzle.Answers[index];

			_puzzles.StudentAnswers[student.Id] = answer == _puzzles.CurrentPuzzle.CorrectAnswer;
			await ctx.ReplyAsync($"Вы успешно ответили на задание '{_puzzles.CurrentPuzzle.Name}'!");
		}

		[TelegramButton(nameof(ShowPuzzles))]
		private async Task ShowPuzzles(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			if (_puzzles.CurrentPuzzle != null)
				await ctx.ReplyAsync($"Текущая задача: {_puzzles.CurrentPuzzle.Name}");
			await ctx.ReplyAsync($"Существующие задачи: {string.Join(", ", _polyContext.Puzzles.Select(p => p.Name))}.");
		}

		[TelegramButton(nameof(RemovePuzzle))]
		private async Task RemovePuzzle(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;

			_puzzles.StopCurrentPuzzle();
			await ctx.ReplyAsync("Текущая задача убрана!");
		}

		[TelegramButton(nameof(ShowStudentsSolvedPuzzle))]
		private async Task ShowStudentsSolvedPuzzle(TelegramButtonExecutionContext ctx)
		{
			List<string> studentCorrect = [];
			List<string> studentIncorrect = [];
			foreach (var student in _polyContext.Students)
			{
				if (_puzzles.StudentAnswers.TryGetValue(student.Id, out var studentSolvedPuzzle))
				{
					if (studentSolvedPuzzle)
						studentCorrect.Add(student.Surname + " " + student.Name + " " + student.Patronymic);
					else
						studentIncorrect.Add(student.Surname + " " + student.Name + " " + student.Patronymic);
				}
			}
			await ctx.ReplyAsync($"Студенты, правильно ответившие на задание: {string.Join(", ", studentCorrect)}");
			await ctx.ReplyAsync($"Студенты, неправильно ответившие на задание: {string.Join(", ", studentIncorrect)}");
		}
		[TelegramButton(nameof(AddFaq))]
		private async Task AddFaq(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;
			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите вопрос"),
					new TelegramMessageBuilder("Введите ответ на этот вопрос")
				],
				HandleFaqInfoEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleFaqInfoEntered(DiscreteMessageEnteredArgs args)
			{
				var question = args.Responses[0].Text;
				var answer = args.Responses[1].Text;
				if (string.IsNullOrEmpty(question))
				{
					await args.ReplyAsync("Вы не ввели вопрос");
					return;
				}
				if (string.IsNullOrEmpty(answer))
				{
					await args.ReplyAsync("Вы не ввели ответ");
					return;
				}
				FaqEntry faqEntry = new() {
					Id = default, 
					Question = question,
					Answer = answer
				};
				_polyContext.FaqEntries.Add(faqEntry);
				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync("Вопрос был успешно добавлен");
			}
		}

		[TelegramButton(nameof(RemoveFaq))]
		private async Task RemoveFaq(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;
			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите вопрос, который хотите удалить"),
				],
				HandleFaqInfoEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);
				
			async Task HandleFaqInfoEntered(DiscreteMessageEnteredArgs args)
			{
				var question = args.Responses[0].Text;
				if (string.IsNullOrEmpty(question))
				{
					await args.ReplyAsync("Вы не ввели вопрос");
					return;
				}
				FaqEntry? faqEntry = _polyContext.FaqEntries.FirstOrDefault(s => s.Question == question);
				if (faqEntry == null)
				{
					await args.ReplyAsync("Такого вопроса нет в базе");
					return;
				}
				_polyContext.FaqEntries.Remove(faqEntry);
				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync("Вопрос был успешно удален");
			}
		}

		[TelegramButton(nameof(AddHelp))]
		private async Task AddHelp(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;
			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите название"),
					new TelegramMessageBuilder("Введите основной текст"),
					new TelegramMessageBuilder("Введите колонтитул"),
					new TelegramMessageBuilder("Отправьте файл")
				],
				HandleHelpInfoEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleHelpInfoEntered(DiscreteMessageEnteredArgs args)
			{
				var title = args.Responses[0].Text;
				var text = args.Responses[1].Text;
				var footer = args.Responses[2].Text;
				var file = args.Responses[3].Document?.FileId;
				if (string.IsNullOrEmpty(title))
				{
					await args.ReplyAsync("Вы не ввели название");
					return;
				}
				if (string.IsNullOrEmpty(text))
				{
					await args.ReplyAsync("Вы не ввели текст");
					return;
				}
				if (string.IsNullOrEmpty(footer))
				{
					await args.ReplyAsync("Вы не ввели нижний колонтитул");
					return;
				}
				HelpEntry helpEntry = new() {
					Id = default, 
					Title = title,
					Text = text,
					Footer = footer,
					FileId = file
				};
				_polyContext.HelpEntries.Add(helpEntry);
				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync("Ссылка была успешно добавлена");
			}
		}

		[TelegramButton(nameof(RemoveHelp))]
		private async Task RemoveHelp(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TelegramAdmins.Contains(ctx.Query.From.Id))
				return;
			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите название ссылки, которую хотите удалить"),
				],
				HandleHelpInfoEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);
				
			async Task HandleHelpInfoEntered(DiscreteMessageEnteredArgs args)
			{
				var title = args.Responses[0].Text;
				if (string.IsNullOrEmpty(title))
				{
					await args.ReplyAsync("Вы не ввели название");
					return;
				}
				HelpEntry? helpEntry = _polyContext.HelpEntries.FirstOrDefault(s => s.Title == title);
				if (helpEntry == null)
				{
					await args.ReplyAsync("Такой ссылки нет в базе");
					return;
				}
				_polyContext.HelpEntries.Remove(helpEntry);
				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync("Ссылка была успешно удалена");
			}
		}
	}
}
