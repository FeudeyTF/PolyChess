using LichessAPI.Clients;
using LichessAPI.Clients.Authorized;
using LichessAPI.Types.Arena;
using LichessAPI.Types.Swiss;
using LichessAPI.Types.Tokens;
using PolyChess.Components.Data;
using PolyChess.Components.Telegram.Buttons;
using PolyChess.Components.Telegram.Commands;
using PolyChess.Components.Tournaments;
using PolyChess.Configuration;
using PolyChess.Core;
using PolyChess.Core.Telegram;
using PolyChess.Core.Telegram.Messages;
using PolyChess.Core.Telegram.Messages.Discrete;
using PolyChess.Core.Telegram.Messages.Discrete.Messages;
using PolyChess.Core.Telegram.Messages.Pagination;
using PolyChess.Core.Telegram.Messages.Pagination.Builders;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Components.Telegram.ClientCommands
{
    /// <summary>
    /// TODO: Полностью переписать код команды
    /// </summary>
    internal class MeTelegramCommand : TelegramCommandAggregator
    {
        private readonly PolyContext _polyContext;

        private readonly LichessClient _lichess;

        private readonly TournamentsComponent _tournaments;

        private readonly IMainConfig _mainConfig;

        private readonly PaginationMessage<object> _myTournamentsMessage;

        private readonly PaginationMessage<object> _nextTournamentsMessage;

        private readonly DiscreteMessagesProvider _discreteMessagesProvider;

        public MeTelegramCommand(ITelegramProvider telegramProvider, LichessClient lichessClient, PolyContext polyContext, TournamentsComponent tournaments, IMainConfig mainConfig, DiscreteMessagesProvider discreteMessagesProvider)
        {
            _polyContext = polyContext;
            _lichess = lichessClient;
            _tournaments = tournaments;
            _mainConfig = mainConfig;
            _discreteMessagesProvider = discreteMessagesProvider;


            SimplePaginationMessageBuilder<object> myTournamentsBuilder = new(MyTournamentToString);
            SimplePaginationMessageBuilder<object> nextTournamentsBuilder = new(NextTournamentToString);

            _myTournamentsMessage = new("myTournaments", 1, GetMyTournaments, myTournamentsBuilder, telegramProvider);
            _nextTournamentsMessage = new("nextTournaments", 1, GetNextTournaments, nextTournamentsBuilder, telegramProvider);
        }

        [TelegramCommand("me", "Выдаёт информацию о Вас")]
        private async Task Me(TelegramCommandExecutionContext ctx)
        {
            var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == ctx.User.Id);
            if (student == null)
            {
                await ctx.ReplyAsync("Информация о Вашем аккаунте не найдена, обратитесь к администратору бота!");
                return;
            }

            if (string.IsNullOrEmpty(student.LichessId))
            {
                await ctx.ReplyAsync($"Ник на Lichess: <b>Аккаунт не привязан</b>. Привяжите аккаунт с помощью /reg");
                return;
            }

            var lichessUser = await _lichess.GetUserAsync(student.LichessId);
            if (lichessUser == null)
            {
                await ctx.ReplyAsync($"Ник на Lichess: <b>Аккаунт не найден</b>. Перепривяжите аккаунт с помощью /reg");
                return;
            }


            TelegramMessageBuilder message = new();
            List<string> text =
            [
                $"👋 Приветствую, <b>{student.Name}</b>",
                $"♟ <b>Имя аккаунта на Lichess:</b> {lichessUser.Username}",
                $"🕓 <b>Дата регистрации:</b> {lichessUser.RegisterDate:g}",
                $"🕓 <b>Последний вход:</b> {lichessUser.LastSeenDate:g}",
                "👥 <i><b>Команды</b></i>"
            ];

            var teams = await _lichess.GetUserTeamsAsync(lichessUser.Username);

            if (teams.Count > 0)
            {
                foreach (var team in teams)
                    text.Add($" - <b>{team.Name} ({team.MembersCount} участников)</b>");
            }
            else
                text.Add(" - Отсутствуют");

            text.Add("");
            text.Add("📈 <i><b>Рейтинги</b></i>");

            foreach (var perfomance in lichessUser.Perfomance)
                text.Add($" - <b>{perfomance.Key.Upperize()}</b>, Двизион: <b>{_tournaments.GetTournamentDivision(perfomance.Value.Rating)}</b>, Сыграно: {perfomance.Value.Games}, Рейтинг: {perfomance.Value.Rating}");

            message.WithoutWebPagePreview();
            message.WithText(string.Join("\n", text));
            message.AddButton(InlineKeyboardButton.WithUrl("♟Lichess профиль", lichessUser.URL));

            InlineKeyboardButton viewTournaments = new("💪 Посмотреть баллы за турниры");
            viewTournaments.SetData(nameof(MeViewTournaments), ("ID", ctx.User.Id));
            message.AddButton(viewTournaments);

            InlineKeyboardButton viewProgress = new("📊 Посмотреть прогресс по зачёту");
            viewProgress.SetData(nameof(MeViewProgress), ("ID", ctx.User.Id));
            message.AddButton(viewProgress);

            InlineKeyboardButton viewNextTournaments = new("🏟 Посмотреть будущие турниры");
            viewNextTournaments.SetData(nameof(MeViewNextTournaments));
            message.AddButton(viewNextTournaments);
            await ctx.ReplyAsync(message);
        }

        [TelegramButton(nameof(MeViewTournaments))]
        private async Task MeViewTournaments(TelegramButtonExecutionContext ctx)
        {
            if (ctx.Query.Message != null)
                await ctx.SendMessageAsync(_myTournamentsMessage, ctx.Query.Message.Chat.Id);
        }

        [TelegramButton(nameof(MeViewNextTournaments))]
        private async Task MeViewNextTournaments(TelegramButtonExecutionContext ctx)
        {
            if (ctx.Query.Message != null)
                await ctx.SendMessageAsync(_nextTournamentsMessage, ctx.Query.Message.Chat.Id);
        }

        [TelegramButton(nameof(MeViewProgress))]
        private async Task MeViewProgress(TelegramButtonExecutionContext ctx)
        {
            var id = ctx.GetLongNumber("ID");
            var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == id);
            float totalScore = 0;
            int totalRatingsCount = 4;
            int barsInBar = 20;

            int zeroScoreTournaments = 0;
            int oneScoreTournaments = 0;

            if (student != null)
            {
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

                    List<string> text = ["📌<b>Ваш прогресс по выполнению регламента зачёта:</b>"];
                    if (_polyContext.Lessons.Any())
                    {
                        var lessons = _polyContext.Attendances.Where(a => a.Student == student);
                        float attendancePercent = (float)lessons.Count() / _polyContext.Lessons.Count();
                        text.Add($"📚<b>Посещение занятий:</b> {lessons.Count()}/{_polyContext.Lessons.Count()} ({(int)(attendancePercent * 100)}%)");
                        totalScore += attendancePercent;
                    }

                    float visitedTournamentsCount = zeroScoreTournaments + oneScoreTournaments + student.AdditionalTournamentsScore;
                    totalScore += Math.Min(visitedTournamentsCount / _mainConfig.Test.RequiredTournamentsCount, 1f);

                    text.Add($"🤝<b>Участие в турнирах:</b>");
                    text.Add($"       <b>Всего</b>: {visitedTournamentsCount} из {_mainConfig.Test.RequiredTournamentsCount} ({StringUtils.CreateSimpleBar(visitedTournamentsCount, _mainConfig.Test.RequiredTournamentsCount, bars: barsInBar)})");
                    text.Add("         - Не в топе: " + zeroScoreTournaments);
                    text.Add("         - В топе: " + oneScoreTournaments);
                    if (student.AdditionalTournamentsScore != 0)
                        text.Add("         - Дополнительно: " + student.AdditionalTournamentsScore);

                    if (!string.IsNullOrEmpty(student.LichessToken))
                    {
                        LichessAuthorizedClient lichesAuthUser = new(student.LichessToken);
                        var puzzleDashboard = await lichesAuthUser.GetPuzzleDashboard((int)(DateTime.Now - _mainConfig.SemesterStartDate).TotalDays);
                        if (puzzleDashboard != null)
                        {
                            totalScore += Math.Min((float)puzzleDashboard.Global.FirstWins / _mainConfig.Test.RequiredPuzzlesSolved, 1f);
                            text.Add($"🧩<b>Решение пазлов:</b> {puzzleDashboard.Global.FirstWins} из {_mainConfig.Test.RequiredPuzzlesSolved} ({StringUtils.CreateSimpleBar(puzzleDashboard.Global.FirstWins, _mainConfig.Test.RequiredPuzzlesSolved, bars: barsInBar)})");
                        }
                        else
                            text.Add($"🧩<b>Решение пазлов:</b> Данные не были получены!");
                    }
                    else
                        text.Add($"🧩<b>Решение пазлов:</b> Токен не подключён!");

                    float creativeTask = student.CreativeTaskCompleted ? 1f : 0f;
                    totalScore += creativeTask;
                    text.Add($"🧠<b>Творческое задание:</b> {StringUtils.CreateSimpleBar(creativeTask, 1, bars: 1)} {(student.CreativeTaskCompleted ? "В" : "Не в")}ыполнено!");

                    text.Add("");
                    text.Add("📊<b>Полный прогресс:</b>");
                    text.Add($"{Math.Round(totalScore * barsInBar / totalRatingsCount)} из 20 {StringUtils.CreateSimpleBar(totalScore, totalRatingsCount, bars: barsInBar)}");
                    TelegramMessageBuilder msg = new(string.Join("\n", text));

                    if (string.IsNullOrEmpty(student.LichessToken) && student.TelegramId == ctx.Query.From.Id)
                    {
                        InlineKeyboardButton button = new("🔑 Подключить токен");
                        button.SetData(nameof(ConnectPuzzleToken));
                        msg.AddButton(button);
                    }

                    await ctx.ReplyAsync(msg);
                }
                else
                    await ctx.ReplyAsync("Ваш аккаунт не найден на Lichess!");
            }
            else
                await ctx.ReplyAsync("Ваш аккаунт не найден в системе!");
        }

        [TelegramButton(nameof(ConnectPuzzleToken))]
        private async Task ConnectPuzzleToken(TelegramButtonExecutionContext ctx)
        {
            DiscreteMessage message = new(
                _discreteMessagesProvider,
                [new TelegramMessageBuilder("Введите токен. Его можно создать <a href=\"https://lichess.org/account/oauth/token\">здесь</a>")],
                OnTokenEntered
            );

            if (ctx.Query.Message != null)
                await ctx.SendMessageAsync(message, ctx.Query.Message.Chat.Id);

            async Task OnTokenEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 1)
                {
                    var token = args.Responses[0].Text;
                    if (!string.IsNullOrEmpty(token))
                    {
                        var tokenInfos = await _lichess.TestTokens(token);
                        if (tokenInfos.TryGetValue(token, out var tokenInfo) && tokenInfo != null)
                        {
                            var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == args.User.Id);
                            if (student != null && student.LichessId != null)
                            {
                                var lichessUser = await _lichess.GetUserAsync(student.LichessId);
                                if (lichessUser != null)
                                {
                                    if (tokenInfo.UserID == lichessUser.ID)
                                    {
                                        if (tokenInfo.Expires == default || tokenInfo.Expires > DateTime.Now)
                                        {
                                            if (tokenInfo.Scopes != null && tokenInfo.Scopes.Count > 0)
                                            {
                                                if (tokenInfo.Scopes.Any(scope => scope.AccessLevel == TokenScopeAccessLevel.Read && scope.Type == TokenScopeType.Puzzle))
                                                {
                                                    student.LichessToken = token;
                                                    await _polyContext.SaveChangesAsync();
                                                    await args.ReplyAsync("Токен успешно установлен!");
                                                }
                                                else
                                                    await args.ReplyAsync("У токена нет доступа к просмотру статистики пазлов!");
                                            }
                                            else
                                                await args.ReplyAsync("Не найдено токенов!");
                                        }
                                        else
                                            await args.ReplyAsync("Токен просрочен!");
                                    }
                                    else
                                        await args.ReplyAsync("Этот токен принадлежит не Вам!");
                                }
                                else
                                    await args.ReplyAsync("Информация о Вашем аккаунта не была найдена!");
                            }
                            else
                                await args.ReplyAsync("Информация о Вас не была найдена!");
                        }
                        else
                            await args.ReplyAsync("Информация об этом токене не была найдена!");
                    }
                    else
                        await args.ReplyAsync("Нужно ввести токен");
                }
            }
        }

        private List<object> GetNextTournaments(Message message)
        {
            List<object> result = [];
            foreach (var tournament in _tournaments.TournamentsList)
                if (tournament.Tournament.StartDate > DateTime.UtcNow && tournament.Tournament.StartDate < _mainConfig.SemesterEndDate)
                    result.Add(tournament);
            foreach (var tournament in _tournaments.SwissTournamentsList)
                if (tournament.Tournament.Started > DateTime.UtcNow && tournament.Tournament.Started < _mainConfig.SemesterEndDate)
                    result.Add(tournament);
            return new List<object>([.. from r in result orderby (r is ArenaTournamentInfo t ? t.Tournament.StartDate : r is SwissTournamentInfo s ? s.Tournament.Started : DateTime.Now) select r]);
        }

        private string NextTournamentToString(object info, int index)
        {
            List<string> result = ["<b> - Информация о будущих турнирах!</b>"];
            if (info is ArenaTournamentInfo arenaTournamentInfo)
            {
                if (arenaTournamentInfo != null)
                {
                    var tournament = arenaTournamentInfo.Tournament;
                    result.Add($"\U0001f91d Турнир <b><a href=\"https://lichess.org/tournament/{tournament.ID}\">{tournament.FullName}</a></b>.");
                    result.Add($" - Состоится: <b>{tournament.StartDate.AddHours(3):g}</b>");
                    result.Add($" - Закончится: <b>{tournament.FinishDate.AddHours(3):g}</b>");
                    result.Add($" - Время: <b>{TimeSpan.FromSeconds(tournament.Clock.Limit).Minutes}+{tournament.Clock.Increment}</b>");
                    result.Add($" - Длительность: <b>{TimeSpan.FromMinutes(tournament.Minutes):hh\\:mm}</b>");
                    result.Add($" - Доступен ли Берсерк: <b>{(tournament.Berserkable ? "Да" : "Нет")}</b>");
                    result.Add($" - Рейтинговый: <b>{(tournament.Rated ? "Да" : "Нет")}</b>");
                }
            }
            else if (info is SwissTournamentInfo swissTournamentInfo)
            {
                if (swissTournamentInfo != null)
                {
                    var tournament = swissTournamentInfo.Tournament;
                    result.Add($"🇨🇭 Турнир <b><a href=\"https://lichess.org/swiss/{tournament.ID}\">{tournament.Name}</a></b>.");
                    result.Add($" - Состоится: <b>{tournament.Started.AddHours(3):g}</b>");
                    result.Add($" - Время: <b>{TimeSpan.FromSeconds(tournament.Clock.Limit).Minutes}+{tournament.Clock.Increment}</b>");
                    result.Add($" - Количество партий: <b>{tournament.RoundsNumber}</b>");
                    result.Add($" - Рейтинговый: <b>{(tournament.Rated ? "Да" : "Нет")}</b>");
                }
            }
            result.Add("");
            return string.Join("\n", result);
        }

        private List<object> GetMyTournaments(Message message)
        {
            var userId = message.From != null ? message.From.Id : message.Chat.Id;
            List<object> result = [];
            foreach (var tournament in _tournaments.TournamentsList)
                if (tournament.Tournament.StartDate < DateTime.UtcNow && tournament.Rating.Players.Any(p => p.Student != null && p.Student.TelegramId == userId))
                    result.Add(tournament);
            foreach (var tournament in _tournaments.SwissTournamentsList)
                if (tournament.Tournament.Started < DateTime.UtcNow && tournament.Rating.Players.Any(p => p.Student != null && p.Student.TelegramId == userId))
                    result.Add(tournament);
            return new List<object>([.. from r in result orderby (r is ArenaTournamentInfo t ? t.Tournament.StartDate : r is SwissTournamentInfo s ? s.Tournament.Started : DateTime.Now) descending select r]);
        }

        private string MyTournamentToString(object info, int index)
        {
            var student = _polyContext.Students.FirstOrDefault(s => s.TelegramId == 0);
            if (student != null)
            {
                if (!string.IsNullOrEmpty(student.LichessId))
                {
                    TelegramMessageBuilder message = new("<b> - Информация об участии в турнирах!</b>");
                    List<string> result = [];
                    if (info is ArenaTournamentInfo arenaTournamentInfo)
                    {
                        if (arenaTournamentInfo != null)
                        {
                            result.Add($"\U0001f91d Турнир <b><a href=\"https://lichess.org/tournament/{arenaTournamentInfo.Tournament.ID}\">{arenaTournamentInfo.Tournament.FullName}</a></b>. Состоялся <b>{arenaTournamentInfo.Tournament.StartDate.AddHours(3):g}</b>");
                            TournamentUser<SheetEntry>? player = default;
                            foreach (var p in arenaTournamentInfo.Rating.Players)
                            {
                                if (p.TournamentEntry.Username == student.LichessId)
                                {
                                    player = p;
                                    break;
                                }
                            }

                            if (player != default)
                            {
                                result.Add($" - Ранг: <b>{player.TournamentEntry.Rank}</b>");
                                result.Add($" - Рейтинг: <b>{player.TournamentEntry.Rating}</b>");
                                result.Add($" - Перформанс: <b>{player.TournamentEntry.Performance}</b>");
                                result.Add($" - Балл: <b>{(player.Score == -1 ? "Турнир не зачтён" : player.Score)}</b>");
                                if (player.TournamentEntry.Sheet != null && !string.IsNullOrEmpty(player.TournamentEntry.Sheet.Scores))
                                    result.Add($" - Итоговая строка: <b>{player.TournamentEntry.Sheet.Scores}</b>");
                                DivisionType division = DivisionType.None;
                                foreach (var div in arenaTournamentInfo.Rating.Divisions)
                                    if (div.Value.Any(e => e.Username == student.LichessId))
                                    {
                                        division = div.Key;
                                        break;
                                    }

                                result.Add($" - Дивизион: <b>{(division == DivisionType.None ? "Нет" : division)}</b>");

                            }
                            else
                                result.Add(" - <b>Отсутствовал</b>");
                        }
                    }
                    else if (info is SwissTournamentInfo swissTournamentInfo)
                    {
                        if (swissTournamentInfo != null)
                        {
                            result.Add($"🇨🇭 Турнир <b><a href=\"https://lichess.org/swiss/{swissTournamentInfo.Tournament.ID}\">{swissTournamentInfo.Tournament.Name}</a></b>. Состоялся <b>{swissTournamentInfo.Tournament.Started.AddHours(3):g}</b>");
                            TournamentUser<SwissSheetEntry>? player = default;
                            foreach (var p in swissTournamentInfo.Rating.Players)
                            {
                                if (p.TournamentEntry.Username == student.LichessId)
                                {
                                    player = p;
                                    break;
                                }
                            }

                            if (player != default)
                            {
                                result.Add($" - Ранг: <b>{player.TournamentEntry.Rank}</b>");
                                result.Add($" - Очки: <b>{player.TournamentEntry.Points}</b>");
                                result.Add($" - TieBreak: <b>{player.TournamentEntry.TieBreak}</b>");
                                result.Add($" - Перформанс: <b>{player.TournamentEntry.Performance}</b>");
                                result.Add($" - Балл: <b>{(player.Score == -1 ? "Отсутствовал" : player.Score)}</b>");
                                DivisionType division = DivisionType.None;
                                foreach (var div in swissTournamentInfo.Rating.Divisions)
                                    if (div.Value.Any(e => e.Username == student.LichessId))
                                    {
                                        division = div.Key;
                                        break;
                                    }

                                result.Add($" - Дивизион: <b>{(division == DivisionType.None ? "Нет" : division)}</b>");
                            }
                            else
                                result.Add(" - <b>Отсутствовал</b>");
                        }
                    }
                    result.Add("");
                    return string.Join("\n", result);
                }
            }
            return string.Empty;
        }
    }
}
