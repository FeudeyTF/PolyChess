using LichessAPI.Clients.Authorized;
using LichessAPI.Types;
using LichessAPI.Types.Arena;
using LichessAPI.Types.Swiss;
using LichessAPI.Types.Tokens;
using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Commands.Basic;
using PolyChessTGBot.Bot.Commands.Discrete;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Bot.Messages.Discrete;
using PolyChessTGBot.Database;
using PolyChessTGBot.Extensions;
using PolyChessTGBot.Managers.Tournaments;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = PolyChessTGBot.Database.User;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        private readonly ListMessage<FAQEntry> FAQMessage;

        private readonly ListMessage<HelpLink> HelpMessage;

        private readonly List<FAQEntry> FAQEntries;

        private readonly List<HelpLink> HelpLinks;

        private readonly ListMessage<object> Tournaments;

        private readonly ListMessage<object> NextTournaments;

        private readonly Dictionary<long, (string Name, string FlairID)> AccountVerifyCodes;

        public BotCommands()
        {
            FAQMessage = new("FAQ", GetFAQValues, ConvertFAQEntryToString)
            {
                Header = "❓<b>FAQ</b> шахмат❓ Все самые <b>часто задаваемые</b> вопросы собраны в одном месте:"
            };

            HelpMessage = new("Help", GetHelpLinksValue, ConvertHelpLinkToString, 1, false, "Далее ➡️", "⬅️ Назад")
            {
                GetDocumentID = GetHelpLinkDocumentID
            };

            FAQAdmin = new("adminFAQ", GetFAQValues, ConvertFAQEntryToString, 1, additionalKeyboards: [[new("🗑Удалить", "Delete", HandleFAQDelete), new("✏️Изменить", "Change", HandleFAQChange)]]);

            HelpAdmin = new("adminHelp", GetHelpLinksValue, ConvertHelpLinkToString, 1, false, "Далее ➡️", "⬅️ Назад", [[new("🗑Удалить", "Delete", HandleHelpLinkDelete), new("✏️Изменить", "Change", HandleHelpLinkChange)]])
            {
                GetDocumentID = GetHelpLinkDocumentID
            };

            Tournaments = new("tournaments", GetTournamentsIDs, TournamentToString, 5, true, "Далее ➡️", "⬅️ Назад")
            {
                Header = "<b> - Информация об участии в турнирах!</b>"
            };

            NextTournaments = new("gdfgsdf", GetNextTournamentsIDs, NextTournamentToString, 5, true, "Далее ➡️", "⬅️ Назад")
            {
                Header = "<b> - Информация о будущих турнирах!</b>"
            };

            AdminCheckUsers = new("checkUsers",
                () => Program.Data.Users,
                (user, index, tgUser) => user.ToString(),
                10,
                true,
                "Далее ➡️",
                "⬅️ Назад");

            AccountVerifyCodes = [];
            FAQEntries = Program.Data.GetFAQEntries();
            HelpLinks = Program.Data.GetHelpLinks();
        }

        private List<HelpLink> GetHelpLinksValue() => HelpLinks;

        private List<FAQEntry> GetFAQValues() => FAQEntries;

        private string ConvertHelpLinkToString(HelpLink link, int index, Telegram.Bot.Types.User user)
            => $"<b>{link.Title}</b>\n{link.Text}\n<i>{link.Footer}</i>";

        private string? GetHelpLinkDocumentID(HelpLink link) => link.FileID;

        private string ConvertFAQEntryToString(FAQEntry entry, int index, Telegram.Bot.Types.User user)
            => $"{index + 1}) <b>{entry.Question}</b>\n - {entry.Answer}";

        [DiscreteCommand("question", "Команда отправит вопрос напрямую Павлу", ["Введите вопрос, которых хотите задать"], visible: true)]
        private async Task Question(CommandArgs<Message> args)
        {
            if (args.Parameters.Count == 1)
            {
                var question = args.Parameters[0];
                if (!string.IsNullOrEmpty(question.Text))
                {
                    List<string> text =
                    [
                        "<b><u>Вопрос от пользователя!</u></b>🙋‍",
                        $"👤<b>Ник пользователя:</b> @{args.User.Username}",
                        $"👤<b>Имя пользователя:</b> {args.User.FirstName} {args.User.LastName}",
                        $"🕑<b>Дата отправки:</b> {question.Date:G}",
                        $"❓<b>Вопрос:</b>\n{question.Text}"
                    ];
                    InlineKeyboardButton button = new("Данные");
                    button.SetData("QuestionDataID", ("ID", args.User.Id), ("ChannelID", question.MessageId));
                    var message = new TelegramMessageBuilder(string.Join("\n", text))
                        .AddButton(button);
                    await args.Bot.SendMessage(message.WithToken(args.Token), Program.MainConfig.QuestionChannel);
                    await args.Reply("Ваш вопрос был успешно отправлен!");
                }
                else
                    await args.Reply("Неправильно введён вопрос!");
            }
        }

        [Command("help", "Выдаёт список с полезными материалами", true)]
        private async Task SendHelpLinks(CommandArgs args)
        {
            await HelpMessage.Send(args.Bot, args.Message.Chat.Id, args.User, args.Token);
        }

        [Command("cmds", "Выдаёт список всех команд", admin: true)]
        private async Task GetCommands(CommandArgs args)
        {
            List<string> text = ["<b>Список команд для бота:</b>", "", " - Обычные команды:"];
            foreach (var command in Program.Bot.CommandRegistrator.Commands)
                if (!command.AdminCommand)
                    text.Add($"<b>{command.Name}</b> - {command.Description}");
            text.Add("");
            text.Add(" - Админские команды");
            foreach (var command in Program.Bot.CommandRegistrator.Commands)
                if (command.AdminCommand)
                    text.Add($"<b>{command.Name}</b> - {command.Description}");

            await args.Reply(string.Join("\n", text));
        }

        [Command("faq", "Выдаёт список с FAQ", true)]
        private async Task FAQ(CommandArgs args)
        {
            await FAQMessage.Send(args.Bot, args.Message.Chat.Id, args.User, args.Token);
        }

        [Command("reg", "Меняет привязанный к студенту аккаунт Lichess")]
        private async Task Register(CommandArgs args)
        {
            if (AccountVerifyCodes.TryGetValue(args.User.Id, out (string Name, string FlairID) code))
            {
                var account = await Program.Lichess.GetUserAsync(code.Name);
                if (account != null)
                {
                    if (account.Flair == code.FlairID)
                    {
                        using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE LichessName='{account.Username}'");
                        if (!reader.Read())
                        {
                            var user = Program.Data.GetUser(args.User.Id);
                            if (user != null)
                            {
                                user.LichessName = account.Username;
                                Program.Data.Query($"UPDATE Users SET LichessName='{account.Username}' WHERE TelegramID='{args.User.Id}'");
                                AccountVerifyCodes.Remove(args.User.Id);
                                await args.Reply($"Ваш аккаунт теперь - <b>{account.Username}</b>");
                            }
                            else
                                await args.Reply($"Ваши данные не были найдены!");
                        }
                        else
                            await args.Reply($"Аккаунт <b>{account.Username}</b> уже занят!");
                    }
                    else
                        await args.Reply($"Значок аккаунта <b>{code.Name}</b> не установлен на <b>{code.FlairID}</b> или отсутствует");
                }
                else
                    await args.Reply($"Аккаунт <b>{code.Name}</b> не был найден");
            }
            else
            {
                if (args.Parameters.Count == 1)
                {
                    var account = await Program.Lichess.GetUserAsync(args.Parameters[0]);
                    if (account != null)
                    {
                        using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE LichessName='{account.Username}'");
                        if (!reader.Read())
                        {
                            var flairCode = Program.MainConfig.Flairs[Random.Shared.Next(Program.MainConfig.Flairs.Count)];
                            while (flairCode == account.Flair)
                                flairCode = Program.MainConfig.Flairs[Random.Shared.Next(Program.MainConfig.Flairs.Count)];
                            AccountVerifyCodes.Add(args.User.Id, (account.Username, flairCode));
                            await args.Reply($"Вам нужно установить значок аккаунта {account.Username} на <b>{flairCode}</b> (делается в настройках на Lichess. Нужно вставить в поле выбора значка <b>{flairCode.Split('.')[1]}</b>), после чего найти точное совпадение значка с <b>{flairCode}</b>. Дальше вы опять прописываете /reg");

                        }
                        else
                            await args.Reply($"Аккаунт <b>{account.Username}</b> уже занят!");
                    }
                    else
                        await args.Reply($"Аккаунт <b>{args.Parameters[0]}</b> не был найден!");
                }
                else
                    await args.Reply("Неправильный синтаксис! Правильно: /reg ник Lichess");
            }
        }

        [Command("me", "Выдаёт информацию о студенте", true)]
        private async Task MyInfo(CommandArgs args)
        {
            User? user = Program.Data.GetUser(args.User.Id);
            if (user != null)
            {
                List<string> text = [$"👋 Приветствую, <b>{user.Name}</b>"];
                if (!string.IsNullOrEmpty(user.LichessName))
                {
                    TelegramMessageBuilder message = new();
                    var lichessUser = await Program.Lichess.GetUserAsync(user.LichessName);
                    if (lichessUser != null)
                    {
                        text.Add($"♟ <b>Имя аккаунта на Lichess:</b> {lichessUser.Username}");
                        text.Add($"🕓 <b>Дата регистрации:</b> {lichessUser.RegisterDate:g}");
                        text.Add($"🕓 <b>Последний вход:</b> {lichessUser.LastSeenDate:g}");
                        text.Add("👥 <i><b>Команды</b></i>");

                        var teams = await Program.Lichess.GetUserTeamsAsync(lichessUser.Username);

                        if (teams.Count > 0)
                            foreach (var team in teams)
                                text.Add($" - <b>{team.Name} ({team.MembersCount} участников)</b>");
                        else
                            text.Add(" - Отсутствуют");

                        text.Add("");
                        text.Add("📈 <i><b>Рейтинги</b></i>");

                        foreach (var perfomance in lichessUser.Perfomance)
                            text.Add($" - <b>{perfomance.Key.Beautify()}</b>, Двизион: <b>{Program.Tournaments.GetTournamentDivision(perfomance.Value.Rating)}</b>, Сыграно: {perfomance.Value.Games}, Рейтинг: {perfomance.Value.Rating}");

                        message.WithoutWebPagePreview();
                        message.WithText(string.Join("\n", text));
                        InlineKeyboardButton accountLinkButton =
                          new("♟Lichess профиль")
                          {
                              Url = lichessUser.URL
                          };
                        message.AddButton(accountLinkButton);

                        InlineKeyboardButton viewTournaments = new("💪 Посмотреть баллы за турниры");
                        viewTournaments.SetData("MeViewTournaments", ("ID", args.User.Id));
                        message.AddButton(viewTournaments);

                        InlineKeyboardButton viewProgress = new("📊 Посмотреть прогресс по зачёту");
                        viewProgress.SetData("MeViewProgress", ("ID", args.User.Id));
                        message.AddButton(viewProgress);

                        InlineKeyboardButton viewNextTournaments = new("🏟 Посмотреть будущие турниры");
                        viewNextTournaments.SetData("ViewNextTournaments", ("ID", args.User.Id));
                        message.AddButton(viewNextTournaments);

                        await args.Reply(message);
                    }
                    else
                        await args.Reply($"Ник на Lichess: <b>Аккаунт не найден</b>. Перепривяжите аккаунт с помощью /reg");
                }
                else
                    await args.Reply($"Ник на Lichess: <b>Аккаунт не привязан</b>. Привяжите аккаунт с помощью /reg");
            }
            else
                await args.Reply("Информация о Вашем аккаунте не найдена, обратитесь к администратору бота!");
        }

        [Button("MeViewTournaments")]
        private async Task ViewTournaments(ButtonInteractArgs args)
        {
            var id = args.GetLongNumber("ID");
            Telegram.Bot.Types.User tgUser = new() { Id = id };
            if (args.Query.Message != null)
                await Tournaments.Send(args.Bot, args.Query.Message.Chat.Id, id == args.Query.From.Id ? args.Query.From : tgUser, args.Token);
        }

        [Button("ViewNextTournaments")]
        private async Task ViewNextTournaments(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await NextTournaments.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From, args.Token);
        }

        [Button("MeViewProgress")]
        private async Task ViewProgress(ButtonInteractArgs args)
        {
            User? user = Program.Data.GetUser(args.GetLongNumber("ID"));

            float totalScore = 0;
            int totalRatingsCount = 3;
            int barsInBar = 15;

            int zeroScoreTournaments = 0;
            int oneScoreTournaments = 0;

            if (user != null)
            {
                if (!string.IsNullOrEmpty(user.LichessName))
                {
                    foreach (var tournament in Program.Tournaments.TournamentsList)
                        if (tournament.Tournament.StartDate < DateTime.UtcNow)
                            foreach (var player in tournament.Rating.Players)
                                if (player.User != null && player.User.TelegramID == user.TelegramID && player.Score > -1)
                                {
                                    if (Program.MainConfig.TournamentRules.TryGetValue(tournament.Tournament.ID, out var rule))
                                    {
                                        if (player.Score == 1)
                                            oneScoreTournaments += rule.PointsForWinning;
                                        else if (player.Score == 0)
                                            zeroScoreTournaments += rule.PointsForBeing;
                                    }
                                    else
                                    {
                                        if (player.Score == 1)
                                            oneScoreTournaments += TournamentScoreRule.DEFAULT_POINTS_FOR_WINNING;
                                        else if (player.Score == 0)
                                            zeroScoreTournaments += TournamentScoreRule.DEFAULT_POINTS_FOR_BEING;
                                    }
                                    break;
                                }

                    foreach (var tournament in Program.Tournaments.SwissTournamentsList)
                        if (tournament.Tournament.Started < DateTime.UtcNow)
                            foreach (var player in tournament.Rating.Players)
                                if (player.User != null && player.User.TelegramID == user.TelegramID && player.Score > -1)
                                {
                                    if (Program.MainConfig.TournamentRules.TryGetValue(tournament.Tournament.ID, out var rule))
                                    {
                                        if (player.Score == 1)
                                            oneScoreTournaments += rule.PointsForWinning;
                                        else if (player.Score == 0)
                                            zeroScoreTournaments += rule.PointsForBeing;
                                    }
                                    else
                                    {
                                        if (player.Score == 1)
                                            oneScoreTournaments += TournamentScoreRule.DEFAULT_POINTS_FOR_WINNING;
                                        else if (player.Score == 0)
                                            zeroScoreTournaments += TournamentScoreRule.DEFAULT_POINTS_FOR_BEING;
                                    }
                                    break;
                                }

                    float visitedTournamentsCount = zeroScoreTournaments + oneScoreTournaments;
                    List<string> text = ["📌<b>Ваш прогресс по выполнению регламента зачёта:</b>"];
                    text.Add("📚<b>Посещение занятий:</b> Недоступно");

                    totalScore += Math.Min(visitedTournamentsCount / Program.MainConfig.Test.RequiredTournamentsCount, 1f);

                    text.Add($"🤝<b>Участие в турнирах:</b>");
                    text.Add($"       <b>Всего</b>: {visitedTournamentsCount} из {Program.MainConfig.Test.RequiredTournamentsCount} ({Utils.CreateSimpleBar(visitedTournamentsCount, Program.MainConfig.Test.RequiredTournamentsCount, bars: barsInBar)})");
                    text.Add("         - Не в топе: " + zeroScoreTournaments);
                    text.Add("         - В топе: " + oneScoreTournaments);

                    if (!string.IsNullOrEmpty(user.TokenKey))
                    {
                        var lichesAuthUser = new LichessAuthorizedClient(user.TokenKey);
                        var puzzleDashboard = await lichesAuthUser.GetPuzzleDashboard((int)(DateTime.Now - Program.SemesterStartDate).TotalDays);
                        if (puzzleDashboard != null)
                        {
                            totalScore += Math.Min((float)puzzleDashboard.Global.FirstWins / Program.MainConfig.Test.RequiredPuzzlesSolved, 1f);
                            text.Add($"🧩<b>Решение пазлов:</b> {puzzleDashboard.Global.FirstWins} из {Program.MainConfig.Test.RequiredPuzzlesSolved} ({Utils.CreateSimpleBar(puzzleDashboard.Global.FirstWins, Program.MainConfig.Test.RequiredPuzzlesSolved, bars: barsInBar)})");
                        }
                        else
                            text.Add($"🧩<b>Решение пазлов:</b> Токен не подключён!");
                    }
                    else
                        text.Add($"🧩<b>Решение пазлов:</b> Токен не верен!");

                    float creativeTask = user.CreativeTaskCompleted ? 1f : 0f;
                    totalScore += creativeTask;
                    text.Add($"🧠<b>Творческое задание:</b> {Utils.CreateSimpleBar(creativeTask, 1, bars: 1)} Не выполнено!");

                    text.Add("");
                    text.Add("📊<b>Полный прогресс:</b>");
                    text.Add($"{Math.Round(totalScore * barsInBar / totalRatingsCount)} из 15 {Utils.CreateSimpleBar(totalScore, totalRatingsCount, bars: barsInBar)}");
                    TelegramMessageBuilder msg = new(string.Join("\n", text));

                    if (string.IsNullOrEmpty(user.TokenKey) && user.TelegramID == args.Query.From.Id)
                    {
                        InlineKeyboardButton button = new("🔑 Подключить токен");
                        button.SetData("ConnectPuzzleToken");
                        msg.AddButton(button);
                    }

                    await args.Reply(msg);
                }
                else
                    await args.Reply("Ваш аккаунт не найден на Lichess!");
            }
            else
                await args.Reply("Ваш аккаунт не найден в системе!");
        }

        [Button("ConnectPuzzleToken")]
        private async Task ConnectToken(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    ["Введите токен. Его можно создать <a href=\"https://lichess.org/account/oauth/token\">здесь</a>"],
                    OnTokenEntered);

            static async Task OnTokenEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 1)
                {
                    var token = args.Responses[0].Text;
                    if (!string.IsNullOrEmpty(token))
                    {
                        var tokenInfos = await Program.Lichess.TestTokens(token);
                        if (tokenInfos.TryGetValue(token, out var tokenInfo) && tokenInfo != null)
                        {
                            var user = Program.Data.GetUser(args.User.Id);
                            if (user != null)
                            {
                                var lichessUser = await Program.Lichess.GetUserAsync(user.LichessName);
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
                                                    user.TokenKey = token;
                                                    Program.Data.Query($"UPDATE Users SET TokenKey='{user.TokenKey}' WHERE TelegramID='{args.User.Id}'");
                                                    await args.Reply("Токен успешно установлен!");
                                                }
                                                else
                                                    await args.Reply("У токена нет доступа к просмотру статистики пазлов!");
                                            }
                                            else
                                                await args.Reply("Не найдено токенов!");
                                        }
                                        else
                                            await args.Reply("Токен просрочен!");
                                    }
                                    else
                                        await args.Reply("Этот токен принадлежит не Вам!");
                                }
                                else
                                    await args.Reply("Информация о Вашем аккаунта не была найдена!");
                            }
                            else
                                await args.Reply("Информация о Вас не была найдена!");
                        }
                        else
                            await args.Reply("Информация об этом токене не была найдена!");
                    }
                    else
                        await args.Reply("Нужно ввести токен");
                }
            }
        }

        private List<object> GetTournamentsIDs()
        {
            List<object> result = [];
            foreach (var tournament in Program.Tournaments.TournamentsList)
                if (tournament.Tournament.StartDate < DateTime.UtcNow)
                    result.Add(tournament);
            foreach (var tournament in Program.Tournaments.SwissTournamentsList)
                if (tournament.Tournament.Started < DateTime.UtcNow)
                    result.Add(tournament);
            return new List<object>([.. from r in result orderby (r is ArenaTournamentInfo t ? t.Tournament.StartDate : r is SwissTournamentInfo s ? s.Tournament.Started : DateTime.Now) descending select r]);
        }

        private string TournamentToString(object info, int index, Telegram.Bot.Types.User tgUser)
        {
            User? user = Program.Data.GetUser(tgUser.Id);
            if (user != null)
            {
                if (!string.IsNullOrEmpty(user.LichessName))
                {
                    TelegramMessageBuilder message = new();
                    List<string> result = [];
                    if (info is ArenaTournamentInfo arenaTournamentInfo)
                    {
                        if (arenaTournamentInfo != null)
                        {
                            result.Add($"\U0001f91d Турнир <b><a href=\"https://lichess.org/tournament/{arenaTournamentInfo.Tournament.ID}\">{arenaTournamentInfo.Tournament.FullName}</a></b>. Состоялся <b>{arenaTournamentInfo.Tournament.StartDate.AddHours(3):g}</b>");
                            TournamentUser<SheetEntry>? player = default;
                            foreach (var p in arenaTournamentInfo.Rating.Players)
                            {
                                if (p.TournamentEntry.Username == user.LichessName)
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
                                    if (div.Value.Any(e => e.Username == user.LichessName))
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
                                if (p.TournamentEntry.Username == user.LichessName)
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
                                    if (div.Value.Any(e => e.Username == user.LichessName))
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

        private List<object> GetNextTournamentsIDs()
        {
            List<object> result = [];
            foreach (var tournament in Program.Tournaments.TournamentsList)
                if (tournament.Tournament.StartDate > DateTime.UtcNow && tournament.Tournament.StartDate < Program.SemesterEndDate)
                    result.Add(tournament);
            foreach (var tournament in Program.Tournaments.SwissTournamentsList)
                if (tournament.Tournament.Started > DateTime.UtcNow && tournament.Tournament.Started < Program.SemesterEndDate)
                    result.Add(tournament);
            return new List<object>([.. from r in result orderby (r is ArenaTournamentInfo t ? t.Tournament.StartDate : r is SwissTournamentInfo s ? s.Tournament.Started : DateTime.Now) select r]);
        }

        private string NextTournamentToString(object info, int index, Telegram.Bot.Types.User user)
        {
            List<string> result = [];
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
    }
}
