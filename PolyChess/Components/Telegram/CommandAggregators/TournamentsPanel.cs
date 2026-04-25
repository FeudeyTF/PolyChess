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
	internal class TournamentsPanel : TelegramCommandAggregator
	{
		public string TempPath { get; private set; }

		private readonly PolyContext _polyContext;

		private readonly LichessClient _lichessClient;

		private readonly ILogger _logger;

		private readonly TournamentsComponent _tournaments;

		private readonly IMainConfig _mainConfig;

		private readonly DiscreteMessagesProvider _discreteMessagesProvider;

		public TournamentsPanel(ITelegramProvider telegramProvider, TournamentsComponent tournaments, IMainConfig config, PolyContext polyContext, ILogger logger, LichessClient client)
		{
			_polyContext = polyContext;
			_logger = logger;
			_lichessClient = client;
			_discreteMessagesProvider = new(telegramProvider);
			_mainConfig = config;
			_tournaments = tournaments;
			TempPath = Path.Combine(Environment.CurrentDirectory, "tmp");
		}
		[TelegramCommand("tournaments", "Выводит панель администратора", IsHidden = true, IsAdmin = false)]
		private async Task Tournaments(TelegramCommandExecutionContext ctx)
		{
			TelegramMessageBuilder message = "Добро пожаловать в панель турниров.";

			message.AddButton(
				new InlineKeyboardButton("Добавить турнир").WithData(nameof(AddCustomTournament))
			);

			message.AddButton(
				new InlineKeyboardButton("Поставить баллы за турнир").WithData(nameof(SetScore))
			);

			message.AddButton(
				new InlineKeyboardButton("Отметка участия (старт/финиш)").WithData(nameof(MarkAttendanceScore))
			);

			message.AddButton(
				new InlineKeyboardButton("Посмотреть существующие турниры").WithData(nameof(CheckCustomTournaments))
			);

			message.AddButton(
				new InlineKeyboardButton("💾 Сохранить турнир").WithData(nameof(SaveTournament))
			);

			message.AddButton(
				new InlineKeyboardButton("🤝 Результаты турнира").WithData(nameof(TournamentResult))
			);

			message.AddButton(
				new InlineKeyboardButton("🔄 Обновить турниры").WithData(nameof(UpdateTournaments))
			);

			await ctx.ReplyAsync(message);
		}

		[TelegramButton(nameof(AddCustomTournament))]
		private async Task AddCustomTournament(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TournamentsAdmins.Contains(ctx.Query.From.Id))
				return;
			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите название турнира"),
					new TelegramMessageBuilder("Введите описание турнира"),
					new TelegramMessageBuilder("Введите дату проведения турнира (дд-мм-гггг чч:мм)"),
					new TelegramMessageBuilder("Турнир требует двух отметок участия (в начале и в конце) с категориями 3/2/1? (- да, иначе - нет)")
				],
				HandleTournamentInfoEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleTournamentInfoEntered(DiscreteMessageEnteredArgs args)
			{
				var name = args.Responses[0].Text;
				if (string.IsNullOrEmpty(name))
				{
					await args.ReplyAsync("Некорректное название турнира");
					return;
				}
				var description = args.Responses[1].Text;
				if (string.IsNullOrEmpty(description))
				{
					await args.ReplyAsync("Некорректное описание турнира");
					return;
				}

				if (!DateTime.TryParse(args.Responses[2].Text, out var startDate))
				{
					await args.ReplyAsync("Ошибка! Неверный формат даты начала турнира.");
					return;
				}

				CustomTournament tournament = new()
				{
					Id = default,
					Name = name,
					Description = description,
					StartDate = startDate,
					UseAttendanceBonus = IsPositiveAnswer(args.Responses[3].Text)
				};
				_polyContext.Tournaments.Add(tournament);
				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync($"Турнир {name} проводящийся {startDate} успешно добавлен! Режим отметок участия: {(tournament.UseAttendanceBonus ? "включён" : "выключен")}.");
			}
		}

		[TelegramButton(nameof(CheckCustomTournaments))]
		private async Task CheckCustomTournaments(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TournamentsAdmins.Contains(ctx.Query.From.Id))
				return;

			if (!_polyContext.Tournaments.Any())
			{
				await ctx.ReplyAsync("Пока никаких турниров нет");
				return;
			}
			List<string> msg = ["Были найдены следующие турниры:"];
			foreach (var tournament in _polyContext.Tournaments)
			{
				msg.Add($"{tournament.Name} проводится {tournament.StartDate:g}, описание: \"{tournament.Description}\", режим отметок: {(tournament.UseAttendanceBonus ? "да" : "нет")}");
			}
			await ctx.ReplyAsync(string.Join("\n", msg));
		}

		[TelegramButton(nameof(SetScore))]
		private async Task SetScore(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TournamentsAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите название турнира"),
					new TelegramMessageBuilder("Введите имена студентов (каждое с новой строки) (Фамилия, Фамилия Имя, Имя, ФИО)"),
					new TelegramMessageBuilder("Введите кол-во баллов")
				],
				HandleScoreInfoEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleScoreInfoEntered(DiscreteMessageEnteredArgs args)
			{
				var tour = args.Responses[0].Text;
				if (string.IsNullOrEmpty(tour))
				{
					await args.ReplyAsync("Вы ввели название турнира");
					return;
				}
				var tournament = FindTournamentByName(tour);
				if (tournament == null)
				{
					await args.ReplyAsync("Вы ввели название несуществующего турнира");
					return;
				}

				if (tournament.UseAttendanceBonus)
				{
					await args.ReplyAsync("Для этого турнира ручное начисление отключено. Используйте кнопку \"Отметка участия (старт/финиш)\".");
					return;
				}

				var enteredStudents = args.Responses[1].Text;
				if (string.IsNullOrEmpty(enteredStudents))
				{
					await args.ReplyAsync("Вы не ввели ФИО студентов");
					return;
				}
				var students = enteredStudents
					.Split("\n")
					.Select(s => s.Trim())
					.Where(s => !string.IsNullOrEmpty(s))
					.ToList();
				if (students.Count == 0)
				{
					await args.ReplyAsync("Вы не ввели ФИО студентов");
					return;
				}

				if (!int.TryParse(args.Responses[2].Text, out var score))
				{
					await args.ReplyAsync("Количество очков введено некорректно");
					return;
				}

				foreach (var studentData in students)
				{
					var studentsFound = _polyContext.GetStudentsByIdentifier(studentData);

					if (studentsFound.Count() > 1)
					{
						await args.ReplyAsync($"По введённой фамилии и имени были найдены студенты:\n{string.Join('\n', studentsFound.Select(s => s.Surname + " " + s.Name + " " + s.Patronymic))}");
						return;
					}
					if (studentsFound.Count == 0)
					{
						await args.ReplyAsync("По вашему запросу не найдено ни одного студента!");
						return;
					}

					var student = studentsFound.First();
					var entry = _polyContext.TournamentEntries.FirstOrDefault(e => e.Student.Id == student.Id && e.Tournament.Id == tournament.Id);
					if (entry == null)
					{
						entry = new CustomTournamentEntry()
						{
							Id = default,
							Tournament = tournament,
							Student = student,
							Score = score
						};
						_polyContext.TournamentEntries.Add(entry);
					}
					else
						entry.Score = score;
				}
				await _polyContext.SaveChangesAsync();

				List<string> msg = [
					$"{score} очков было начислено студентам"
				];
				msg.Add(string.Join('\n', students));

				await args.ReplyAsync(string.Join("\n", msg));
			}
		}

		[TelegramButton(nameof(MarkAttendanceScore))]
		private async Task MarkAttendanceScore(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TournamentsAdmins.Contains(ctx.Query.From.Id))
				return;

			DiscreteMessage message = new(
				_discreteMessagesProvider,
				[
					new TelegramMessageBuilder("Введите название турнира"),
					new TelegramMessageBuilder("Введите этап отметки: начало/конец"),
					new TelegramMessageBuilder("Введите категорию участника: основной/запасной/гость"),
					new TelegramMessageBuilder("Введите имена студентов (каждое с новой строки) (Фамилия, Фамилия Имя, Имя, ФИО)")
				],
				HandleAttendanceScoreEntered
			);

			if (ctx.Query.Message != null)
				await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

			async Task HandleAttendanceScoreEntered(DiscreteMessageEnteredArgs args)
			{
				var tournamentName = args.Responses[0].Text;
				if (string.IsNullOrEmpty(tournamentName))
				{
					await args.ReplyAsync("Вы не ввели название турнира");
					return;
				}

				var tournament = FindTournamentByName(tournamentName);
				if (tournament == null)
				{
					await args.ReplyAsync("Вы ввели название несуществующего турнира");
					return;
				}

				if (!tournament.UseAttendanceBonus)
				{
					await args.ReplyAsync("Для этого турнира режим двух отметок отключён. Включите его при создании турнира.");
					return;
				}

				if (!TryParseAttendanceStage(args.Responses[1].Text, out var markAtStart))
				{
					await args.ReplyAsync("Некорректный этап отметки. Доступные варианты: начало/конец.");
					return;
				}

				if (!TryParseParticipantCategory(args.Responses[2].Text, out var category))
				{
					await args.ReplyAsync("Некорректная категория. Доступные варианты: основной/запасной/гость.");
					return;
				}

				var enteredStudents = args.Responses[3].Text;
				if (string.IsNullOrEmpty(enteredStudents))
				{
					await args.ReplyAsync("Вы не ввели ФИО студентов");
					return;
				}

				var students = enteredStudents
					.Split("\n")
					.Select(s => s.Trim())
					.Where(s => !string.IsNullOrEmpty(s))
					.ToList();
				if (students.Count == 0)
				{
					await args.ReplyAsync("Вы не ввели ФИО студентов");
					return;
				}

				List<Student> foundStudents = [];
				HashSet<int> uniqueStudentIds = [];
				foreach (var studentData in students)
				{
					var studentsFound = _polyContext.GetStudentsByIdentifier(studentData);

					if (studentsFound.Count > 1)
					{
						await args.ReplyAsync($"По введённой фамилии и имени были найдены студенты:\n{string.Join('\n', studentsFound.Select(s => s.Surname + " " + s.Name + " " + s.Patronymic))}");
						return;
					}
					if (studentsFound.Count == 0)
					{
						await args.ReplyAsync("По вашему запросу не найдено ни одного студента!");
						return;
					}

					var student = studentsFound.First();
					if (uniqueStudentIds.Add(student.Id))
						foundStudents.Add(student);
				}

				List<string> categoryConflicts = [];
				var categoryScore = (int)category;
				foreach (var student in foundStudents)
				{
					var attendance = _polyContext.TournamentAttendances.FirstOrDefault(a => a.Student.Id == student.Id && a.Tournament.Id == tournament.Id);
					if (attendance != null && attendance.Category != category)
					{
						categoryConflicts.Add($"{student.Surname} {student.Name} {student.Patronymic}: уже отмечен как \"{ParticipantCategoryToString(attendance.Category)}\"");
						continue;
					}

					var entry = _polyContext.TournamentEntries.FirstOrDefault(e => e.Student.Id == student.Id && e.Tournament.Id == tournament.Id);
					if (entry != null && entry.Score >= (int)CustomTournamentParticipantCategory.Guest && entry.Score <= (int)CustomTournamentParticipantCategory.Main && entry.Score != categoryScore)
					{
						categoryConflicts.Add($"{student.Surname} {student.Name} {student.Patronymic}: за турнир уже выставлено {entry.Score} очк.");
					}
				}

				if (categoryConflicts.Count > 0)
				{
					await args.ReplyAsync("Нельзя выставлять разные категории одному и тому же участнику в одном турнире:\n" + string.Join('\n', categoryConflicts));
					return;
				}

				var awarded = 0;
				foreach (var student in foundStudents)
				{
					var attendance = _polyContext.TournamentAttendances.FirstOrDefault(a => a.Student.Id == student.Id && a.Tournament.Id == tournament.Id);
					if (attendance == null)
					{
						attendance = new()
						{
							Id = default,
							Tournament = tournament,
							Student = student,
							Category = category,
							IsMarkedAtStart = markAtStart,
							IsMarkedAtFinish = !markAtStart
						};
						_polyContext.TournamentAttendances.Add(attendance);
					}
					else if (markAtStart)
						attendance.IsMarkedAtStart = true;
					else
						attendance.IsMarkedAtFinish = true;

					if (attendance.IsMarkedAtStart && attendance.IsMarkedAtFinish)
					{
						var entry = _polyContext.TournamentEntries.FirstOrDefault(e => e.Student.Id == student.Id && e.Tournament.Id == tournament.Id);
						if (entry == null)
						{
							entry = new()
							{
								Id = default,
								Tournament = tournament,
								Student = student,
								Score = categoryScore
							};
							_polyContext.TournamentEntries.Add(entry);
						}
						else
							entry.Score = categoryScore;

						if (!attendance.IsScoreApplied)
							awarded++;
						attendance.IsScoreApplied = true;
					}
				}

				await _polyContext.SaveChangesAsync();
				await args.ReplyAsync($"Отметка \"{(markAtStart ? "начало" : "конец")}\" поставлена для {foundStudents.Count} участников. Подтверждённых начислений: {awarded}.");
			}
		}
		[TelegramButton(nameof(SaveTournament))]
		private async Task SaveTournament(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TournamentsAdmins.Contains(ctx.Query.From.Id))
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
			if (!_mainConfig.TournamentsAdmins.Contains(ctx.Query.From.Id))
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
		[TelegramButton(nameof(UpdateTournaments))]
		private async Task UpdateTournaments(TelegramButtonExecutionContext ctx)
		{
			if (!_mainConfig.TournamentsAdmins.Contains(ctx.Query.From.Id))
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

		private CustomTournament? FindTournamentByName(string name)
		{
			var trimmedName = name.Trim();
			var tournament = _polyContext.Tournaments.FirstOrDefault(t => t.Name == trimmedName);
			if (tournament != null)
				return tournament;

			var tournamentList = _polyContext.Tournaments.Where(t => t.Name.Contains(trimmedName)).ToList();
			if (tournamentList.Count == 1)
				return tournamentList[0];
			return null;
		}

		private static bool IsPositiveAnswer(string? value)
		{
			if (string.IsNullOrEmpty(value))
				return false;
			var normalized = value.Trim().ToLowerInvariant();
			return normalized is "-" or "да" or "yes" or "y" or "1";
		}

		private static bool TryParseAttendanceStage(string? value, out bool markAtStart)
		{
			markAtStart = false;
			if (string.IsNullOrEmpty(value))
				return false;

			var normalized = value.Trim().ToLowerInvariant();
			if (normalized is "начало" or "старт" or "start" or "1")
			{
				markAtStart = true;
				return true;
			}

			if (normalized is "конец" or "финиш" or "end" or "2")
			{
				markAtStart = false;
				return true;
			}

			return false;
		}

		private static bool TryParseParticipantCategory(string? value, out CustomTournamentParticipantCategory category)
		{
			category = default;
			if (string.IsNullOrEmpty(value))
				return false;

			var normalized = value.Trim().ToLowerInvariant();
			if (normalized is "основной" or "основные" or "main" or "1")
			{
				category = CustomTournamentParticipantCategory.Main;
				return true;
			}

			if (normalized is "запасной" or "запасные" or "reserve" or "2")
			{
				category = CustomTournamentParticipantCategory.Reserve;
				return true;
			}

			if (normalized is "гость" or "гости" or "зритель" or "зрители" or "guest" or "viewer" or "3")
			{
				category = CustomTournamentParticipantCategory.Guest;
				return true;
			}

			return false;
		}

		private static string ParticipantCategoryToString(CustomTournamentParticipantCategory category)
		{
			return category switch
			{
				CustomTournamentParticipantCategory.Main => "основной",
				CustomTournamentParticipantCategory.Reserve => "запасной",
				CustomTournamentParticipantCategory.Guest => "гость/зритель",
				_ => "неизвестно"
			};
		}
	}
}
