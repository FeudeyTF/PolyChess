using LichessAPI.Types;
using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Commands.Basic;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Bot.Messages.Discrete;
using PolyChessTGBot.Database;
using PolyChessTGBot.Extensions;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
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
            InlineKeyboardButton checkUsers = new("👥 Увидеть всех пользователей");
            checkUsers.SetData("SendAllUsers");
            msg.AddButton(checkUsers);
            InlineKeyboardButton updateTournaments = new("🤝 Загрузить турниры");
            updateTournaments.SetData("DownloadTournaments");
            msg.AddButton(updateTournaments);

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

            InlineKeyboardButton lookPlayer = new("🔍 Посмотреть информацию об игроке");
            lookPlayer.SetData("LookPlayer");
            msg.AddButton(lookPlayer);

            await args.Reply(msg.WithText(string.Join("\n", text)));
        }

        [Button("LookPlayer")]
        internal async Task LookPlayer(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await DiscreteMessage.Send(
                    args.Query.Message.Chat.Id,
                    ["Введите имя ученика или ник на Lichess"],
                    OnCheckPlayerEntered);

            static async Task OnCheckPlayerEntered(DecretiveMessageEnteredArgs args)
            {
                User? user = null;
                var name = args.Answers[0].Text;
                foreach (var dataUser in Program.Data.Users)
                    if (dataUser.LichessName == name || dataUser.Name == name)
                    {
                        user = dataUser;
                        break;
                    }

                if (user != null)
                {
                    List<string> text = [$"Информация об ученике <b>{user.Name}</b>"];
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
        internal async Task DeleteHelpLinks(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await HelpAdmin.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From);
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
        internal async Task DeleteFAQEntry(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await FAQAdmin.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From);
        }

        [Button("SendAllUsers")]
        internal async Task SendAllUsersButton(ButtonInteractArgs args)
        {
            if (args.Query.Message != null)
                await AdminCheckUsers.Send(args.Bot, args.Query.Message.Chat.Id, args.Query.From);
        }

        [Button("DownloadTournaments")]
        internal async Task DownloadTournaments(ButtonInteractArgs args)
        {
            if (!string.IsNullOrEmpty(Program.MainConfig.MainPolytechTeamID))
            {
                await args.Reply("Началась загрузка турниров... Это может занять некоторое время");
                var updatedTournaments = await Program.Tournaments.UpdateTournaments(Program.MainConfig.MainPolytechTeamID);
                if (updatedTournaments.Count > 0)
                    await args.Reply($"Турниры {string.Join(", ", updatedTournaments.Select(t => "<b>" + t.name + "</b>"))} успешно добавлены!");
                else
                    await args.Reply("Все турниры уже загружены! Обновления не требуется");
            }
            else
                await args.Reply("Команда Политеха не найдена!");
        }

        private async Task HandleHelpLinkChange(ButtonInteractArgs args, List<HelpLink> links)
        {
            if (args.Query.Message != null)
                await DiscreteMessage.Send(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите новое название (-, если оставить прежним)",
                        "Введите новый текст этой ссылки (-, если оставить прежним)", 
                        "Отправьте новый файл этой ссылки (-, если оставить прежним)"
                    ],
                    OnHelpLinkChangeEntered,
                    links[0]);

            static async Task OnHelpLinkChangeEntered(DecretiveMessageEnteredArgs args)
            {
                if (args.Answers.Length == 3 && args.Data.Count == 1)
                {
                    if (args.Data[0] is HelpLink link)
                    {
                        var newTitle = args.Answers[0].Text;
                        var newText = args.Answers[1].Text;
                        var newFile = args.Answers[2].Document;
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
                await DiscreteMessage.Send(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите новый вопрос (-, если оставить прежним)",
                        "Введите новый ответ на этот вопрос (-, если оставить прежним)"
                    ],
                    OnFAQChangeEntered,
                    entries[0]);

            static async Task OnFAQChangeEntered(DecretiveMessageEnteredArgs args)
            {
                if (args.Answers.Length == 2 && args.Data.Count == 1)
                {
                    if (args.Data[0] is FAQEntry entry)
                    {
                        var newQuestions = args.Answers[0].Text;
                        var newAnswer = args.Answers[1].Text;
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
        internal async Task AddeHelpLink(ButtonInteractArgs args)
        {
            if(args.Query.Message != null)
                await DiscreteMessage.Send(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите название",
                        "Введите основной текст",
                        "Отправьте файл"
                    ],
                    OnHelpLinkAddEntered);

            async Task OnHelpLinkAddEntered(DecretiveMessageEnteredArgs args)
            {
                if (args.Answers.Length == 3)
                {
                    var title = args.Answers[0].Text;
                    var footer = args.Answers[1].Text;
                    var file = args.Answers[2].Document;
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
        internal async Task AddFAQEntry(ButtonInteractArgs args)
        {
            if(args.Query.Message != null)
                await DiscreteMessage.Send(
                    args.Query.Message.Chat.Id,
                    [
                        "Введите вопрос",
                        "Введите ответ на этот вопрос"
                    ],
                    OnFAQAddEntered);

            async Task OnFAQAddEntered(DecretiveMessageEnteredArgs args)
            {
                if (args.Answers.Length == 2)
                {
                    var question = args.Answers[0].Text;
                    var answer = args.Answers[1].Text;
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
        internal async Task SendTeamInfo(ButtonInteractArgs args)
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
        internal async Task SendUserInfo(ButtonInteractArgs args)
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
    }
}
