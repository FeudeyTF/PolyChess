using LichessAPI.Clients.Authorized;
using LichessAPI.Types;
using LichessAPI.Types.Arena;
using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Commands.Basic;
using PolyChessTGBot.Bot.Commands.Discrete;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Bot.Messages.Discrete;
using PolyChessTGBot.Database;
using PolyChessTGBot.Extensions;
using PolyChessTGBot.Managers.Tournaments;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using User = PolyChessTGBot.Database.User;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        private readonly ListMessage<HelpLink> HelpAdmin;

        private readonly ListMessage<FAQEntry> FAQAdmin;

        private readonly ListMessage<User> AdminCheckUsers;

        [Command("panel", "Работает с полезными ссылками", admin: true)]
        private async Task AdminPanel(CommandArgs args)
        {
            List<string> text = [
                "😎 Добро пожаловать в <b>панель администратора</b>!",
                "🌟 Вы - один из немногих, у кого есть доступ к управлению ботом",
                "🔽 Для того, чтобы использовать панель, нажмите на кнопки управления под сообщением"
                ];
            TelegramMessageBuilder msg = new();
            InlineKeyboardButton checkUsers = new("👥 Увидеть всех студентов");
            checkUsers.SetData("SendAllUsers");
            msg.AddButton(checkUsers);
            InlineKeyboardButton updateTournaments = new("🤝 Загрузить турниры");
            updateTournaments.SetData("DownloadTournaments");
            msg.AddButton(updateTournaments);

            InlineKeyboardButton viewTournaments = new("🤝 Показать все турниры");
            viewTournaments.SetData("ViewAllTournaments");
            msg.AddButton(viewTournaments);

            InlineKeyboardButton deleteHelp = new("✏️ Изменить полезную ссылку");
            deleteHelp.SetData("DeleteHelpLinks");

            InlineKeyboardButton deleteFAQ = new("✏️ Изменить запись FAQ");
            deleteFAQ.SetData("DeleteFAQEntry");
            msg.AddKeyboard([deleteHelp, deleteFAQ]);

            InlineKeyboardButton addHelp = new("➕ Полезная ссылка");
            addHelp.SetData("AddHelpLink");

            InlineKeyboardButton addFAQ = new("➕ Запись FAQ");
            addFAQ.SetData("AddFAQEntry");
            msg.AddKeyboard([addHelp, addFAQ]);

            InlineKeyboardButton saveTournament = new("💾 Сохранить турнир");
            saveTournament.SetData("SaveTournament");
            msg.AddButton(saveTournament);

            InlineKeyboardButton tournamentResult = new("🤝 Результаты турнира");
            tournamentResult.SetData("TournamentResult");
            msg.AddButton(tournamentResult);

            InlineKeyboardButton lookPlayer = new("🔍 Посмотреть информацию о студенте");
            lookPlayer.SetData("LookPlayer");
            msg.AddButton(lookPlayer);

            InlineKeyboardButton lookGraduated = new("🔍 Посмотреть студентов, получивших зачёт");
            lookGraduated.SetData("LookGraduated");
            msg.AddButton(lookGraduated);

            InlineKeyboardButton viewTournamentsTop = new("🔝 Посмотреть лучших по турнирам студентов");
            viewTournamentsTop.SetData("ViewTournamentsTop");
            msg.AddButton(viewTournamentsTop);

            InlineKeyboardButton viewTeamsMembers= new("Посмотреть количество участников от команды");
            viewTeamsMembers.SetData("ViewTeamsMembers");
            msg.AddButton(viewTeamsMembers);

            InlineKeyboardButton addOtherTournaments = new("Добавить доп. турниры");
            addOtherTournaments.SetData("AddOtherTournaments");
            msg.AddButton(addOtherTournaments);

            InlineKeyboardButton addEvent = new("Добавить событие");
            addEvent.SetData("AddEvent");
            msg.AddButton(addEvent);

            InlineKeyboardButton getTournamentsTable = new("Получить таблицу турниров");
            getTournamentsTable.SetData("GetTournamentsTable");
            msg.AddButton(getTournamentsTable);

            await args.Reply(msg.WithText(string.Join("\n", text)));
        }

        [Button("LookPlayer")]
        private async Task LookPlayer(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    ["Введите имя студента или ник на Lichess"],
                    OnCheckPlayerEntered);

            static async Task OnCheckPlayerEntered(DiscreteMessageEnteredArgs args)
            {
                User? user = null;
                var name = args.Responses[0].Text;
                foreach (var dataUser in Program.Data.Users)
                    if (dataUser.LichessName == name || dataUser.Name == name)
                    {
                        user = dataUser;
                        break;
                    }

                if (user != null)
                {
                    List<string> text = [$"Информация о студенте <b>{user.Name}</b>"];
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
                            viewTournaments.SetData("MeViewTournaments", ("ID", user.TelegramID));
                            message.AddButton(viewTournaments);

                            InlineKeyboardButton viewProgress = new("📊 Посмотреть прогресс по зачёту");
                            viewProgress.SetData("MeViewProgress", ("ID", user.TelegramID));
                            message.AddButton(viewProgress);

                            await args.Reply(message);
                        }
                        else
                            await args.Reply($"Ник на Lichess: <b>Аккаунт не найден</b>.");
                    }
                    else
                        await args.Reply($"Ник на Lichess: <b>Аккаунт не привязан</b>.");
                }
                else
                    await args.Reply("Информация о аккаунте не найдена, обратитесь к администратору бота!");
            }
        }

        [Button("DeleteHelpLinks")]
        private async Task DeleteHelpLinks(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await HelpAdmin.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From, args.Token);
        }

        private async Task HandleHelpLinkDelete(ButtonInteractArgs args, List<HelpLink> links)
        {
            if (links.Count != 0)
            {
                var link = links[0];
                HelpLinks.Remove(link);
                Program.Data.Query("DELETE FROM HelpLinks WHERE ID=@0", link.ID);
                if (args.Query.Message != null)
                {
                    await args.Bot.DeleteMessageAsync(args.Query.Message.Chat.Id, args.Query.Message.MessageId);
                    await args.Bot.SendMessage("Полезная ссылка была успешно удалена!", args.Query.Message.Chat.Id);
                }
            }
            else
                await args.Reply("Не найдено полезных ссылок!");
        }

        [Button("DeleteFAQEntry")]
        private async Task DeleteFAQEntry(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await FAQAdmin.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From, args.Token);
        }

        [Button("SendAllUsers")]
        private async Task SendAllUsersButton(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await AdminCheckUsers.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From, args.Token);
        }

        private async Task HandleHelpLinkChange(ButtonInteractArgs args, List<HelpLink> links)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите новое название (-, если оставить прежним)",
                        "Введите новый текст этой ссылки (-, если оставить прежним)", 
                        "Отправьте новый файл этой ссылки (-, если оставить прежним)"
                    ],
                    OnHelpLinkChangeEntered,
                    data: links[0]);

            static async Task OnHelpLinkChangeEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 3 && args.Data.Count == 1)
                {
                    if (args.Data[0] is HelpLink link)
                    {
                        var newTitle = args.Responses[0].Text;
                        var newText = args.Responses[1].Text;
                        var newFile = args.Responses[2].Document;
                        if (newTitle != null && newText != null)
                        {
                            if (newTitle.Trim() != "-")
                                link.Title = newTitle;

                            if (newText.Trim() != "-")
                                link.Text = newText;

                            if (newFile != default)
                                link.FileID = newFile.FileId;

                            Program.Data.Query($"UPDATE HelpLinks SET Text='{link.Text}', Title='{link.Title}', FileID='{link.FileID}' WHERE ID='{link.ID}'");
                            await args.Reply("Полезная ссылка была успешно обновлена!");
                        }
                    }
                }
            }
        }

        private async Task HandleFAQChange(ButtonInteractArgs args, List<FAQEntry> entries)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите новый вопрос (-, если оставить прежним)",
                        "Введите новый ответ на этот вопрос (-, если оставить прежним)"
                    ],
                    OnFAQChangeEntered,
                    data: entries[0]);

            static async Task OnFAQChangeEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 2 && args.Data.Count == 1)
                {
                    if (args.Data[0] is FAQEntry entry)
                    {
                        var newQuestions = args.Responses[0].Text;
                        var newAnswer = args.Responses[1].Text;
                        if (newQuestions != null && newAnswer != null)
                        {
                            if (newQuestions.Trim() != "-")
                                entry.Question = newQuestions;

                            if (newAnswer.Trim() != "-")
                                entry.Answer = newAnswer;

                            Program.Data.Query($"UPDATE FAQ SET Question='{entry.Question}', Answer='{entry.Answer}' WHERE ID='{entry.ID}'");
                            await args.Reply("Запись FAQ была успешно обновлена!");
                        }
                    }
                }
            }
        }

        private async Task HandleFAQDelete(ButtonInteractArgs args, List<FAQEntry> entries)
        {
            if (entries.Count != 0)
            {
                var entry = entries[0];
                FAQEntries.Remove(entry);
                Program.Data.Query("DELETE FROM FAQ WHERE ID=@0", entry.ID);
                if (args.Query.Message != null)
                {
                    await args.Bot.DeleteMessageAsync(args.Query.Message.Chat.Id, args.Query.Message.MessageId);
                    await args.Bot.SendMessage("Вопрос был успешно удалён!", args.Query.Message.Chat.Id);
                }
            }
            else
                await args.Reply("Не найдено вопросов!");
        }

        [Button("AddHelpLink")]
        private async Task AddeHelpLink(ButtonInteractArgs args)
        {
            if(args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите название",
                        "Введите основной текст",
                        "Отправьте файл"
                    ],
                    OnHelpLinkAddEntered);

            async Task OnHelpLinkAddEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 3)
                {
                    var title = args.Responses[0].Text;
                    var footer = args.Responses[1].Text;
                    var file = args.Responses[2].Document;
                    if (title != null && footer != null)
                    {
                        if (file != null)
                        {
                            HelpLink link = new(default, title, footer, "", file.FileId);
                            string text = "INSERT INTO HelpLinks (Title, Text, Footer, FileID) VALUES (@0, @1, @2, @3);";
                            int id = Program.Data.QueryScalar<int>(text + "SELECT CAST(last_insert_rowid() as INT);", link.Title, link.Text, link.Footer, link.FileID == null ? DBNull.Value : link.FileID);
                            link.ID = id;
                            HelpLinks.Add(link);
                            await args.Reply($"Полезная ссылка была успешно добавлена!");
                        }
                        else
                            await args.Reply("К полезной ссылке нужно прикрепить файл! Для этого прикрепите его к сообщеию с командой");
                    }
                    else
                        await args.Reply("Необходимо ввести текст");
                }
            }
        }

        [Button("AddFAQEntry")]
        private async Task AddFAQEntry(ButtonInteractArgs args)
        {
            if(args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите вопрос",
                        "Введите ответ на этот вопрос"
                    ],
                    OnFAQAddEntered);

            async Task OnFAQAddEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 2)
                {
                    var question = args.Responses[0].Text;
                    var answer = args.Responses[1].Text;
                    if (question != null && answer != null)
                    {
                        FAQEntry entry = new(default, question, answer);
                        string text = "INSERT INTO FAQ (Question, Answer) VALUES (@0, @1);";
                        int id = Program.Data.QueryScalar<int>(text + "SELECT CAST(last_insert_rowid() as INT);", entry.Question, entry.Answer);
                        entry.ID = id;
                        FAQEntries.Add(entry);
                        await args.Reply($"Вопрос <b>{entry.Question}</b> и ответ на него <b>{entry.Answer}</b> были успешно добавлены");
                    }
                    else
                        await args.Reply("Необходимо ввести текст");
                }
            }
        }

        [Command("fileinfo", "Выдаёт информацию о файле", admin: true)]
        private async Task GetFileInfo(CommandArgs args)
        {
            if (args.Message.ReplyToMessage != null)
            {
                DocumentInfo? documentInfo = null;
                if (args.Message.ReplyToMessage.Document != null)
                {
                    var document = args.Message.ReplyToMessage.Document;
                    documentInfo = new(document.FileName, document.FileSize, document.FileId, document.FileUniqueId);
                }
                else if (args.Message.ReplyToMessage.Video != null)
                {
                    var document = args.Message.ReplyToMessage.Video;
                    documentInfo = new(document.FileName, document.FileSize, document.FileId, document.FileUniqueId);
                }
                else if (args.Message.ReplyToMessage.Photo != null && args.Message.ReplyToMessage.Photo.Length > 0)
                {
                    var photo = args.Message.ReplyToMessage.Photo.First();
                    documentInfo = new("Noname", photo.FileSize, photo.FileId, photo.FileUniqueId);
                }

                if (documentInfo.HasValue)
                {
                    string message = $"Информация о файле '{documentInfo.Value.FileName}'\n";
                    message += $"Имя: {documentInfo.Value.FileName}\n";
                    message += $"Размер: {documentInfo.Value.FileSize}\n";
                    message += $"Unique ID: {documentInfo.Value.FileUniqueId}\n";
                    message += $"File ID: {documentInfo.Value.FileID}";
                    await args.Reply(message);
                }
                else
                    await args.Reply("Нужно ответить на сообщение с файлом!");
            }
            else
                await args.Reply("Нужно ответить на сообщение с файлом!");
        }

        private struct DocumentInfo(string? fileName, long? fileSize, string fileID, string fileUniqueID)
        {
            public string? FileName = fileName;

            public long? FileSize = fileSize;

            public string FileID = fileID;

            public string FileUniqueId = fileUniqueID;
        }

        [Button("TeamInfo")]
        private async Task SendTeamInfo(ButtonInteractArgs args)
        {
            var teamID = args.GetString("ID");
            if (!string.IsNullOrEmpty(teamID))
            {
                var team = await Program.Lichess.GetTeamAsync(teamID);
                if (team != null)
                {
                    List<string> text =
                        [
                            $"Информация о команде <b>{team.Name}</b>",
                            "<b>Описание:</b>",
                            team.Description,
                            $"<b>Тип:</b> {(team.Open ? "Открытая" : "Закрытая")}",
                            $"<b>Лидер:</b> {team.Leader.Name}",
                            "<i><b>Остальные лидеры</b></i>",
                        ];
                    if (team.Leaders.Count > 0)
                    {
                        foreach (var leader in team.Leaders)
                            text.Add($" - <b>{leader.Name}</b>");
                    }
                    else
                        text.Add(" - Отсутствуют");

                    TelegramMessageBuilder message = string.Join("\n", text);
                    message.WithoutWebPagePreview();
                    InlineKeyboardButton leaderInfo = new($"🔍Информация о лидере {team.Leader.Name}");
                    leaderInfo.SetData("UserInfo", ("Name", team.Leader.Name));
                    message.AddButton(leaderInfo);

                    await args.Reply(message);
                }
                else
                    await args.Reply("Команда не найдена!");
            }
            else
                await args.Reply("Команда не найдена!");
        }

        [Button("UserInfo")]
        private async Task SendUserInfo(ButtonInteractArgs args)
        {
            var name = args.GetString("Name");
            if (!string.IsNullOrEmpty(name))
            {
                var lichessUser = await Program.Lichess.GetUserAsync(name);
                if (lichessUser != null)
                    await args.Reply(await GenerateUserInfo(lichessUser));
                else
                    await args.Reply("Аккаунт Lichess не найден!");
            }
            else
                await args.Reply("Аккаунт Lichess не найден!");
        }

        [Button("LookGraduated")]
        private async Task LookGraduated(ButtonInteractArgs args)
        {
            await args.Reply("Идёт подсчёт, ожидайте...");
            List<string> graduatedUsers = []; 
            foreach (var user in Program.Data.Users)
            {
                if (string.IsNullOrEmpty(user.TokenKey))
                    continue;

                if (!string.IsNullOrEmpty(user.LichessName))
                {
                    int tournamentsCount = user.OtherTournaments;
                    foreach (var tournament in Program.Tournaments.TournamentsList)
                        if (tournament.Tournament.StartDate < DateTime.UtcNow)
                            foreach (var player in tournament.Rating.Players)
                                if (player.User != null && player.User.TelegramID == user.TelegramID && player.Score > -1)
                                {
                                    if (Program.MainConfig.TournamentRules.TryGetValue(tournament.Tournament.ID, out var rule))
                                    {
                                        if (player.Score == 1)
                                            tournamentsCount += rule.PointsForWinning;
                                        else if (player.Score == 0)
                                            tournamentsCount += rule.PointsForBeing;
                                    }
                                    else
                                    {
                                        if (player.Score == 1)
                                            tournamentsCount += TournamentScoreRule.DEFAULT_POINTS_FOR_WINNING;
                                        else if (player.Score == 0)
                                            tournamentsCount += TournamentScoreRule.DEFAULT_POINTS_FOR_BEING;
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
                                            tournamentsCount += rule.PointsForWinning;
                                        else if (player.Score == 0)
                                            tournamentsCount += rule.PointsForBeing;
                                    }
                                    else
                                    {
                                        if (player.Score == 1)
                                            tournamentsCount += TournamentScoreRule.DEFAULT_POINTS_FOR_WINNING;
                                        else if (player.Score == 0)
                                            tournamentsCount += TournamentScoreRule.DEFAULT_POINTS_FOR_BEING;
                                    }
                                    break;
                                }

                    int puzzleCount = -1;
                    LichessAuthorizedClient lichesAuthUser = new(user.TokenKey);
                    var puzzleDashboard = await lichesAuthUser.GetPuzzleDashboard((int)(DateTime.Now - Program.SemesterStartDate).TotalDays);
                    if (puzzleDashboard != null)
                        puzzleCount = puzzleDashboard.Global.FirstWins;

                    if (puzzleCount >= Program.MainConfig.Test.RequiredPuzzlesSolved &&
                        tournamentsCount >= Program.MainConfig.Test.RequiredTournamentsCount &&
                        user.CreativeTaskCompleted
                        )
                        graduatedUsers.Add("<b>" + user.Name + "</b> (" + user.LichessName + ")");

                }
            }

            await args.Reply($"Студенты, получившие зачёт:\n{string.Join("\n", graduatedUsers)}");
        }

        private static async Task<TelegramMessageBuilder> GenerateUserInfo(LichessAPI.Types.User user)
        {
            var teams = await Program.Lichess.GetUserTeamsAsync(user.Username);
            TelegramMessageBuilder message = new();
            List<string> text =
                [
                    $"<b>Имя аккаунта на Lichess:</b> {user.Username}",
                    $"<b>Дата регистрации:</b> {user.RegisterDate:g}",
                    $"<b>Последний вход:</b> {user.LastSeenDate:g}",
                    "<i><b>Команды</b></i>",
                ];

            if (teams.Count > 0)
            {
                foreach (var team in teams)
                {
                    text.Add($" - <b>{team.Name} ({team.MembersCount} участников)</b>");
                    InlineKeyboardButton teamInfo = new($"👥{team.Name}");
                    teamInfo.SetData("TeamInfo", ("ID", team.ID));
                    message.AddButton(teamInfo);
                }
            }
            else
                text.Add(" - Отсутствуют");
            text.Add("<i><b>Рейтинги</b></i>");
            foreach (var perfomance in user.Perfomance)
                text.Add($" - <b>{perfomance.Key.Beautify()}</b>, Сыграно: {perfomance.Value.Games}, Рейтинг: {perfomance.Value.Rating}");
            message.WithText(string.Join("\n", text));
            InlineKeyboardButton accountLinkButton =
              new("♟Lichess профиль")
              {
                  Url = user.URL
              };
            message.AddButton(accountLinkButton);
            message.WithoutWebPagePreview();
            return message;
        }

        [Command("getadmins", "Выдаёт список с админами всех команд, участвовавших в турнирах", admin: true)]
        private async Task GetAdmins(CommandArgs args)
        {
            Dictionary<string, List<Team>> admins = [];
            List<string> entries = [];
            foreach (var arenaID in args.Parameters)
            {
                var arenaData = await Program.Lichess.GetTournament(arenaID);
                if (arenaData != null && arenaData.TeamBattle.Teams != null)
                {
                    List<string> teams = [];
                    foreach (var battleTeam in arenaData.TeamBattle.Teams)
                    {
                        var team = await Program.Lichess.GetTeamAsync(battleTeam.Key);
                        if (team != null)
                        {
                            foreach (var admin in team.Leaders)
                                if (admins.TryGetValue(admin.Name, out var teamList2))
                                    teamList2.Add(team);
                                else
                                {
                                    var user = await Program.Lichess.GetUserAsync(admin.Name);
                                    if (user != null && user.LastSeenDate >= DateTime.Now.AddMonths(-3))
                                        admins.Add(admin.Name, [team]);
                                }
                        }
                    }
                }
            }
            await args.Reply($"ВСЕГО: {admins.Count} админ(ов)!");
            int adminsPerPage = 20;
            if (admins.Count > 0)
            {
                for (int i = 0; i < admins.Count; i += adminsPerPage)
                {
                    List<string> text = [];
                    for (int j = i; j < i + adminsPerPage && j < admins.Count; j++)
                    {
                        var admin = admins.ElementAt(j);
                        entries.Add($"https://lichess.org/@/{admin.Key}");
                        text.Add($"<a href=\"https://lichess.org/@/{admin.Key}\">{admin.Key}</a> ({string.Join(", ", admin.Value.Select(t => $"<b>{t.Name}</b>"))})");
                    }
                    TelegramMessageBuilder message = string.Join("\n", text);
                    message.WithoutWebPagePreview();
                    await args.Reply(message);
                }
                var listFilePath = Path.Combine(TempPath, "admins.txt");
                if (File.Exists(listFilePath))
                    File.Delete(listFilePath);
                using (var streamWriter = new StreamWriter(File.Create(listFilePath), Encoding.UTF8))
                {
                    foreach (var entry in entries)
                        streamWriter.WriteLine(entry);
                    streamWriter.Close();
                }
                using var stream = File.Open(listFilePath, FileMode.Open);
                await args.Reply(new TelegramMessageBuilder("Админы клубов").WithFile(stream, "admins.txt"));
            }
            else
                await args.Reply("Не было найдено админов команд!");
        }

        [Button("SaveTournament")]
        private async Task SaveTournament(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    ["Введите ссылку на турнир"],
                    OnTournamentSwissEntered);

            static async Task OnTournamentSwissEntered(DiscreteMessageEnteredArgs args)
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
                                    var tournament = await Program.Tournaments.UpdateTournament(id);
                                    if (tournament != null)
                                        await args.Reply($"Турнир <b>{tournament.Tournament.FullName}</b> был сохранён!");
                                    else
                                        await args.Reply("Турнир не был найден!");
                                }
                                else if (type == "swiss")
                                {
                                    var tournament = await Program.Tournaments.UpdateSwissTournament(id);
                                    if (tournament != null)
                                        await args.Reply($"Турнир <b>{tournament.Tournament.Name}</b> был сохранён!");
                                    else
                                        await args.Reply("Турнир не был найден!");
                                }
                                else
                                    await args.Reply("Неправильная ссылка!");
                            }
                            else
                                await args.Reply("Неправильная ссылка!");
                        }
                        else
                            await args.Reply("Неправильная ссылка!");
                    }
                    else
                        await args.Reply("Необходимо ввести ссылку на турнир!");
                }
            }
        }

        [Button("TournamentResult")]
        private async Task TournamentResult(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите ссылку на турнир",
                        "Введите тех, кого не нужно учитывать (разделять пробелами или запятой. Введите -, если все учитываются)"
                    ],
                    OnTournamentResultEntered);

            static async Task OnTournamentResultEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 2)
                {
                    var tournamentLink = args.Responses[0].Text;
                    if (tournamentLink != null)
                    {
                        List<string> exclude = new(Program.MainConfig.TopPlayers);
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
                                    await args.Reply("Неправильная ссылка!");
                                    return;
                                }

                                foreach (var msg in messages)
                                    await args.Reply(msg);
                            }
                            else
                                await args.Reply("Неправильная ссылка!");
                        }
                        else
                            await args.Reply("Неправильная ссылка!");
                    }
                    else
                        await args.Reply("Необходимо ввести ссылку на турнир!");
                }
            }

            static async Task<List<TelegramMessageBuilder>> OnArenaResultEntered(string tournamentId, List<string> exclude)
            {
                List<TelegramMessageBuilder> result = [];
                var tournament = await Program.Lichess.GetTournament(tournamentId);

                var directory = Path.Combine(Environment.CurrentDirectory, "Tournaments");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, tournamentId + ".txt");

                if (File.Exists(filePath))
                {
                    List<SheetEntry> tournamentSheet = [];
                    using (var file = File.OpenText(filePath))
                    {
                        tournamentSheet = await Program.Lichess.GetTournamentSheet(file);
                    }

                    if (tournament != null && tournamentSheet != null)
                    {
                        tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username) || e.Team != null && !Program.MainConfig.InstitutesTeamsIDs.Contains(e.Team))).ToList();
                        List<string> csv = ["Имя;Ник Lichess;Балл"];
                        List<string> text = [
                            $"Турнир <b>{tournament.FullName}</b>. Состоялся <b>{tournament.StartDate:g}</b>",
                                    $"Информация об участии в турнире"
                        ];

                        var tournamentRating = Program.Tournaments.GenerateTournamentRating(tournamentSheet);

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
                                csv.Add($"{entry.User?.Name};{entry.TournamentEntry.Username};{entry.Score}");
                            text.Add($"<b>{entry.TournamentEntry.Rank}) {entry.TournamentEntry.Username}</b>, {(string.IsNullOrEmpty(entry.User?.Name) ? "Без имени" : entry.User?.Name)}. Балл: {(entry.Score == -1 ? "-" : entry.Score)}");
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

            static async Task<List<TelegramMessageBuilder>> OnSwissResultEntered(string tournamentId, List<string> exclude)
            {
                List<TelegramMessageBuilder> result = [];
                var tournament = await Program.Lichess.GetSwissTournament(tournamentId);

                var directory = Path.Combine(Environment.CurrentDirectory, "SwissTournaments");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, tournamentId + ".txt");
                if (File.Exists(filePath))
                {
                    var tournamentSheet = await Program.Lichess.GetSwissTournamentSheet(File.OpenText(filePath));
                    tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
                    if (tournament != null && tournamentSheet != null)
                    {
                        List<string> csv = ["Имя;Ник Lichess;Балл"];
                        List<string> text = [
                            $"Турнир по швейцарской <b>{tournament.Name}</b>. Состоялся <b>{tournament.Started:g}</b>",
                                    $"Информация об участии в турнире"
                        ];

                        var tournamentRating = Program.Tournaments.GenerateTournamentRating(tournamentSheet);

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
                                csv.Add($"{entry.User?.Name};{entry.TournamentEntry.Username};{entry.Score}");
                            text.Add($"<b>{entry.TournamentEntry.Rank}) {entry.TournamentEntry.Username}</b>, {(string.IsNullOrEmpty(entry.User?.Name) ? "Без имени" : entry.User?.Name)}. Балл: {(entry.Score == -1 ? "-" : entry.Score)}");
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

        [Button("DownloadTournaments")]
        private async Task DownloadTournaments(ButtonInteractArgs args)
        {
            if (Program.MainConfig.TeamsWithTournaments.Count > 0)
            {
                foreach (var teamId in Program.MainConfig.TeamsWithTournaments)
                {
                    await args.Reply($"Началась загрузка турниров из {teamId}... Это может занять некоторое время");
                    var updatedTournaments = await Program.Tournaments.UpdateTournaments(teamId);
                    if (updatedTournaments.Count > 0)
                        await args.Reply($"Турниры {string.Join(", ", updatedTournaments.Select(t => "<b>" + t.name + "</b>"))} успешно добавлены!");
                    else
                        await args.Reply("Все турниры уже загружены! Обновления не требуется");
                }
            }
            else
                await args.Reply("Команда Политеха не найдена!");
        }

        [Button("ViewTournamentsTop")]
        private async Task ViewTournamentsTop(ButtonInteractArgs args)
        {
            List<string> text = ["<b>Лучшие студенты по результатам турниров в семестре:</b>"];
            Dictionary<User, TournamentsScore> players = [];

            foreach (var tournament in Program.Tournaments.TournamentsList)
                if (tournament.Tournament.StartDate < DateTime.UtcNow)
                    foreach (var player in tournament.Rating.Players)
                    {
                        if (player.User != null)
                        {
                            if (players.TryGetValue(player.User, out var scores))
                            {
                                if (player.Score == 0)
                                    scores.Zeros++;
                                else if (player.Score == 1)
                                    scores.Ones++;
                            }
                            else
                            {
                                if (player.Score == 0)
                                    players.Add(player.User, new(0, 1));
                                else if (player.Score == 1)
                                    players.Add(player.User, new(1, 0));
                            }
                        }
                    }

            foreach (var tournament in Program.Tournaments.SwissTournamentsList)
                if (tournament.Tournament.Started < DateTime.UtcNow)
                    foreach (var player in tournament.Rating.Players)
                    {
                        if (player.User != null)
                        {
                            if (players.TryGetValue(player.User, out var scores))
                            {
                                if (player.Score == 0)
                                    scores.Zeros++;
                                else if (player.Score == 1)
                                    scores.Ones++;
                            }
                            else
                            {
                                if (player.Score == 0)
                                    players.Add(player.User, new(0, 1));
                                else if (player.Score == 1)
                                    players.Add(player.User, new(1, 0));
                            }
                        }
                    }
            players = (from player in players
                       orderby player.Value.Zeros
                       descending
                       orderby player.Value.Ones
                       descending
                       select player).ToDictionary();
            for (int i = 0; i < players.Count; i++)
            {
                var player = players.ElementAt(i);
                text.Add($"{i + 1}) <b>{player.Key.LichessName} ({player.Key.Name})</b>, Победы: {player.Value.Ones}, Посещения: {player.Value.Zeros + player.Value.Ones}");
            }
            await args.Reply(text);
        }

        [Button("ViewTeamsMembers")]
        private async Task ViewTeamsMembers(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    ["Введите ссылку на турнир"],
                    OnTournamentEntered);

            static async Task OnTournamentEntered(DiscreteMessageEnteredArgs args)
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
                                    var stream = File.OpenText(TournamentsManager.GetTournamentPath(id));
                                    var tournament = await Program.Lichess.GetTournamentSheet(stream);
                                    if (tournament != null)
                                    {
                                        Dictionary<string, int> teams = [];
                                        foreach (var entry in tournament)
                                            if (entry.Team != null)
                                            {
                                                if (teams.TryGetValue(entry.Team, out var count))
                                                    teams[entry.Team] = ++count;
                                                else
                                                    teams.Add(entry.Team, 1);
                                            }
                                        teams = (from team in teams
                                                 orderby team.Value
                                                 descending
                                                 select team).ToDictionary();

                                        List<string> text = [
                                            "Команды по количеству участников в турнире",
                                            ];

                                        for (int i = 0; i < teams.Count; i++)
                                        {
                                            var team = teams.ElementAt(i);
                                            Team? realTeam = null;
                                            if (i < 10)
                                                realTeam = await Program.Lichess.GetTeamAsync(team.Key);
                                            text.Add($"{i + 1}) {(realTeam != null ? realTeam.Name : team.Key)} - {team.Value} человек");
                                        }

                                        if (teams.Count == 0)
                                            await args.Reply("Это не командный турнир!");
                                        else
                                            await args.Reply(string.Join("\n", text));
                                    }
                                    else
                                        await args.Reply("Турнир не был найден!");
                                    stream.Close();
                                }
                                else
                                    await args.Reply("Неправильная ссылка!");
                            }
                            else
                                await args.Reply("Неправильная ссылка!");
                        }
                        else
                            await args.Reply("Неправильная ссылка!");
                    }
                    else
                        await args.Reply("Необходимо ввести ссылку на турнир!");
                }
            }
        }

        [Button("ViewAllTournaments")]
        private async Task ViewAllTournaments(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await AllTournaments.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From, args.Token);
        }

        [Button("AddOtherTournaments")]
        private async Task AddOtherTournaments(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    ["Введите имена студентов, которым нужно проставить дополнительный балл за турниры."],
                    OnAddTournamentsEntered);
            static async Task OnAddTournamentsEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 1)
                {
                    var text = args.Responses[0].Text;
                    if (text != null)
                    {

                        List<string> studentsNames = (from name in text.Split(',')
                                                      select name.Trim()).ToList();
                        List<User> addedUsers = [];
                        foreach (var user in Program.Data.Users)
                            if (studentsNames.Remove(user.Name))
                                addedUsers.Add(user);

                        foreach (var user in Program.Data.Users)
                        {
                            foreach(var name in new List<string>(studentsNames))
                                if (user.Name.Contains(name))
                                {
                                    studentsNames.Remove(name);
                                    addedUsers.Add(user);
                                }
                        }

                        foreach (var user in Program.Data.Users)
                        {
                            foreach (var name in new List<string>(studentsNames))
                                if (user.Name.Contains(name, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    studentsNames.Remove(name);
                                    addedUsers.Add(user);
                                }
                        }

                        foreach (var student in addedUsers)
                            Program.Data.Query($"UPDATE Users SET OtherTournaments='{++student.OtherTournaments}' WHERE TelegramID='{student.TelegramID}'");

                        await args.Reply($"Дополнительный бапл был поставлен следующим студентам: {string.Join(", ", addedUsers.Select(u => $"<b>{u.Name}</b>"))}");
                        await args.Reply($"Оставшиеся студенты: {string.Join(", ", studentsNames)}");
                    }
                }
            }
    }

        [Button("AddEvent")]
        private async Task AddEvent(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await args.SendDiscreteMessage(
                    args.Query.Message.Chat.Id,
                    ["Введите название события",
                    "Введите описание события",
                    "Введите дату начала события (- - текущая дата)",
                    "Введите дату конца события"],
                    OnEventInfoEntered);

            static async Task OnEventInfoEntered(DiscreteMessageEnteredArgs args)
            {
                if (args.Responses.Length == 4)
                {
                    var name = args.Responses[0].Text;
                    var description = args.Responses[1].Text;
                    var startFormat = args.Responses[2].Text;
                    var endFormat = args.Responses[3].Text;
                    if(name != null && description != null && startFormat != null && endFormat != null)
                    {
                        DateTime start;
                        if (startFormat == "-")
                            start = DateTime.Now;
                        else if (!DateTime.TryParse(startFormat, out start))
                            await args.Reply("Неправильный формат начальной даты!");
                        if (DateTime.TryParse(endFormat, out var end))
                        {
                            if (start < end)
                            {
                                Program.Data.InsertEvent(new Event(name, description, start, end));
                                await args.Reply($"Событие <b>{name}</b> было успешно добавлено!");
                            }
                            else
                                await args.Reply("Конечная дата не может быть раньше!");
                        }
                        else
                            await args.Reply("Неправильный формат конечной даты!");
                    }
                    else
                        await args.Reply("Один из параметров не был введён!");
                }
                else
                    await args.Reply("Вы ввели неправильно кол-во аргументов!");
            }
        }

        [Button("GetTournamentsTable")]
        private async Task GetTournamentsTable(ButtonInteractArgs args)
        {
            List<string> text = ["Имя;Аккаунт;Результат"];
            Dictionary<User, TournamentsScore> players = [];

            foreach (var tournament in Program.Tournaments.TournamentsList)
                if (tournament.Tournament.IsSemesterTournament())
                    foreach (var player in tournament.Rating.Players)
                    {
                        if (player.User != null)
                        {
                            if (players.TryGetValue(player.User, out var scores))
                            {
                                if (player.Score == 0)
                                    scores.Zeros++;
                                else if (player.Score == 1)
                                    scores.Ones++;
                            }
                            else
                            {
                                if (player.Score == 0)
                                    players.Add(player.User, new(0, 1));
                                else if (player.Score == 1)
                                    players.Add(player.User, new(1, 0));
                            }
                        }
                    }

            foreach (var tournament in Program.Tournaments.SwissTournamentsList)
                if (tournament.Tournament.IsSemesterTournament())
                    foreach (var player in tournament.Rating.Players)
                    {
                        if (player.User != null)
                        {
                            if (players.TryGetValue(player.User, out var scores))
                            {
                                if (player.Score == 0)
                                    scores.Zeros++;
                                else if (player.Score == 1)
                                    scores.Ones++;
                            }
                            else
                            {
                                if (player.Score == 0)
                                    players.Add(player.User, new(0, 1));
                                else if (player.Score == 1)
                                    players.Add(player.User, new(1, 0));
                            }
                        }
                    }
            for (int i = 0; i < players.Count; i++)
            {
                var player = players.ElementAt(i);
                text.Add($"{player.Key.Name};{player.Key.LichessName};{player.Value.Ones + player.Value.Zeros + player.Key.OtherTournaments}");
            }

            TelegramMessageBuilder message = "Файл с таблицей участия в турнирах";
            if (!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);
            var csvFilePath = Path.Combine(TempPath, "tournaments.csv");
            if (File.Exists(csvFilePath))
                File.Delete(csvFilePath);
            using (var streamWriter = new StreamWriter(File.Create(csvFilePath), Encoding.UTF8))
            {
                foreach (var entry in text)
                    streamWriter.WriteLine(entry);
                streamWriter.Close();
            }
            var stream = File.Open(csvFilePath, FileMode.Open);
            message.WithFile(stream, "Table.csv");
            await args.Reply(message);
        }

        [DiscreteCommand("sendinfo", "Рассылает сообщение ВСЕМ студентам", ["Введите сообщение для рассылки или -, если хотите отменить отправку"], admin: true)]
        private async Task SendMessageToAllStudents(CommandArgs<Message> args)
        {
            if (args.Parameters.Count == 1)
            {
                var text = args.Parameters[0].Text;
                if (text != null)
                {
                    if (text == "-")
                    {
                        await args.Reply("Вы отменили отправку сообщения!");
                        return;
                    }
                    TelegramMessageBuilder msg = string.Join("\n", [
                        text,
                        $"Рассылку отправил: <b>{args.User.FirstName}</b>"
                        ]);
                    msg.WithToken(args.Token);
                    foreach (var student in Program.Data.Users)
                    {
                        try
                        {
                            await args.Bot.SendMessage(msg, student.TelegramID);
                            Console.WriteLine("SEND MSG TO " + student.Name);
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("SENDING TOT " + student.Name + " ERRORED! " + e.ToString());
                        }
                    }
                    await args.Reply("Вы успешно отослали сообщение студентам! Их количество: " + Program.Data.Users.Count);
                }
                else
                    await args.Reply("Необходимо ввести сообщение!");
            }
            else
                await args.Reply("Необходимо ввести сообщение!");
        }
    }
}
