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
                    new TelegramMessageBuilder("Введите дату проведения турнира (дд-мм-гггг чч:мм)")
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
                    StartDate = startDate
                };
                _polyContext.Tournaments.Add(tournament);
                await _polyContext.SaveChangesAsync();
                await args.ReplyAsync($"Турнир {name} проводящийся {startDate} успешно добавлен!");
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
			foreach(var tournament in _polyContext.Tournaments)
			{
				msg.Add($"{tournament.Name} проводится {tournament.StartDate:g}, описание: \"{tournament.Description}\"");
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
                var tournament = _polyContext.Tournaments.FirstOrDefault(t => t.Name == tour);
                if (tournament == null)
                {
					var tournamentList = _polyContext.Tournaments.Where(t => t.Name.Contains(tour));
					if (tournamentList.Count() == 1)
					{
						tournament = tournamentList.First();
					} else
					{
                    	await args.ReplyAsync("Вы ввели название несуществующего турнира");
						return;
					}
                }
                var enteredStudents = args.Responses[1].Text;
                if (string.IsNullOrEmpty(enteredStudents))
                {
                    await args.ReplyAsync("Вы не ввели ФИО студентов");
                    return;
                }
				var students = enteredStudents.Split("\n");
                if (int.TryParse(args.Responses[2].Text, out var score))
                {
                    await args.ReplyAsync("Кол-во очков введено некорректно");
                    return;
                }
				foreach(var studentData in students)
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
					CustomTournamentEntry entry = new()
					{
						Id = default,
						Tournament = tournament,
						Student = student,
						Score = score
					};
					_polyContext.TournamentEntries.Add(entry);
					await _polyContext.SaveChangesAsync();
				}

				List<string> msg = [
					$"{score} очков было начислено студентам"
				];
				msg.Add(enteredStudents);
                
                await args.ReplyAsync(string.Join("\n", msg));
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
    }
}