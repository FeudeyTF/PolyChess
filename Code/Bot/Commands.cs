using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Database;
using PolyChessTGBot.Externsions;
using PolyChessTGBot.Lichess.Types.Arena;
using System.Reflection;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace PolyChessTGBot.Bot
{
    public class BotCommands
    {
        public static readonly string TempPath;

        internal readonly ListMessage<FAQEntry> FAQMessage;

        internal readonly ListMessage<HelpLink> HelpMessage;

        internal readonly ListMessage<HelpLink> HelpAdmin;

        internal readonly ListMessage<FAQEntry> FAQAdmin;

        private readonly List<FAQEntry> FAQEntries;

        private readonly List<HelpLink> HelpLinks;

        private Tuple<int, int> DivisionC = new(0, 1300);

        private Tuple<int, int> DivisionB = new(1301, 1800);

        private Tuple<int, int> DivisionA = new(1801, 2100);

        static BotCommands()
        {
            TempPath = Path.Combine(Environment.CurrentDirectory, "Temp");
        }

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

            FAQAdmin = new("adminFAQ", GetFAQValues, ConvertFAQEntryToString, 1, additionalKeyboards: [[new("🗑Удалить", "Delete", HandleFAQDelete)]]);

            HelpAdmin = new("adminHelp", GetHelpLinksValue, ConvertHelpLinkToString, 1, false, "Далее ➡️", "⬅️ Назад", [[new("🗑Удалить", "Delete", HandleHelpLinkDelete)]])
            {
                GetDocumentID = GetHelpLinkDocumentID
            };

            FAQEntries = Program.Data.GetFAQEntries();
            HelpLinks = Program.Data.GetHelpLinks();
        }

        [Command("version", "Отправляет информацию о боте", true)]
        public async Task Version(CommandArgs args)
        {
            string exeFilePath = Path.Combine(
                Environment.CurrentDirectory,
                Assembly.GetExecutingAssembly().GetName().Name + ".exe");
            List<string> message =
            [
                "🛠<b>Информация о боте</b>🛠",
                $"👨🏻‍💻<b>Разработчик:</b> {Program.MainConfig.BotAuthor}",
                $"🔀<b>Версия бота:</b> v.{Program.Version}",
                $"🕐<b>Дата последнего обновления:</b> {File.GetLastAccessTime(exeFilePath):g}",
                $"⏱<b>Время работы:</b> {(DateTime.Now - Program.Started).ToString("%d' дн. '%h' ч. '%m' мин. '%s' сек.'")}"
            ];
            await args.Reply(string.Join("\n", message));
        }

        [Command("question", "Синтаксис: /question \"вопрос\". Команда отправит вопрос напрямую Павлу", true)]
        public async Task Question(CommandArgs args)
        {
            string question = string.Join(" ", args.Parameters);
            if (!string.IsNullOrEmpty(question))
            {
                List<string> text =
                [
                    "<b><u>Вопрос от пользователя!</u></b>🙋‍",
                    $"👤<b>Ник пользователя:</b> @{args.User.Username}",
                    $"👤<b>Имя пользователя:</b> {args.User.FirstName} {args.User.LastName}",
                    $"🕑<b>Дата отправки:</b> {args.Message.Date:G}",
                    $"❓<b>Вопрос:</b>\n{question}"
                ];
                InlineKeyboardButton button = new("Данные");
                button.SetData("QuestionDataID", ("ID", args.User.Id), ("ChannelID", args.Message.MessageId));
                var message = new TelegramMessageBuilder(string.Join("\n", text))
                    .AddButton(button);
                await args.Bot.SendMessage(message, Program.MainConfig.QuestionChannel);
                await args.Reply("Ваш вопрос был успешно отправлен!");
            }
            else
                await args.Reply("Неправильно введён вопрос!");
        }

        [Command("help", "Выдаёт список с полезными материалами", true)]
        private async Task SendHelpLinks(CommandArgs args)
        {
            await HelpMessage.Send(args.Bot, args.Message.Chat.Id);
        }

        [Command("savearena", "Выдаёт список с полезными материалами", admin: true)]
        private async Task SaveArena(CommandArgs args)
        {
            if (args.Parameters.Count == 1)
            {
                var tournamentId = args.Parameters[0];
                var tournament = await Program.Lichess.GetTournament(tournamentId);
                var tournamentSheet = await Program.Lichess.GetTournamentSheet(tournamentId, true);
                if (tournament != null && tournamentSheet != null)
                {
                    var directory = Path.Combine(Environment.CurrentDirectory, "Tournaments");
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    await Program.Lichess.SaveTournamentSheet(tournamentId, Path.Combine(directory, tournamentId + ".txt"), true);
                    await args.Reply($"Турнир <b>{tournament.FullName}</b> был сохранён!");
                }
                else
                    await args.Reply("Турнир не был найден!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /savearena \"ID турнира\"");
        }

        [Command("arenaresult", "Генерирует таблицу с результатами участников", admin: true)]
        private async Task GenerateTournamentTable(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                var tournamentId = args.Parameters[0];
                var tournament = await Program.Lichess.GetTournament(tournamentId);

                var directory = Path.Combine(Environment.CurrentDirectory, "Tournaments");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, tournamentId + ".txt");
                if (File.Exists(filePath))
                {
                    var tournamentSheet = await Program.Lichess.GetTournamentSheet(File.OpenText(filePath));
                    if (args.Parameters.Count > 1)
                    {
                        var stringsToExclude= args.Parameters[1..].Select(p => p.Split(','));
                        List<string> exclude = [];
                        foreach (var str in stringsToExclude)
                            foreach (var str2 in str)
                                if(!string.IsNullOrEmpty(str2.Trim()))
                                    exclude.Add(str2.Trim());
                        tournamentSheet = tournamentSheet.Except(tournamentSheet.Where(e => exclude.Contains(e.Username))).ToList();
                    }
                    if (tournament != null && tournamentSheet != null)
                    {
                        List<string> csv = ["Имя;Ник Lichess;Балл"];
                        List<string> text = [
                            $"Турнир <b>{tournament.FullName}</b>. Состоялся <b>{tournament.Started:g}</b>",
                            $"Информация об участнии в турнире"
                        ];
                        using var reader = Program.Data.SelectQuery($"SELECT * FROM Users");
                        Dictionary<string, User?> users = [];
                        while (reader.Read())
                            if (!string.IsNullOrEmpty(reader.Get("LichessName")))
                                users.Add(reader.Get("LichessName"), new(reader.Get<long>("TelegramID"), reader.Get("Name"), reader.Get("LichessName"), reader.Get<int>("Year")));

                        Dictionary<int, List<SheetEntry>> playersInDivision = new()
                        {
                            { 1, [] }, // Division A
                            { 2, [] }, // Division B
                            { 3, [] }  // Division C
                        };

                        foreach (var entry in tournamentSheet)
                        {
                            if(entry.Rating >= DivisionC.Item1 && entry.Rating <= DivisionC.Item2)
                            {
                                if(playersInDivision[3].Count < 3)
                                    playersInDivision[3].Add(entry);
                            }
                            else if (entry.Rating >= DivisionB.Item1 && entry.Rating <= DivisionB.Item2)
                            {
                                if (playersInDivision[2].Count < 3)
                                    playersInDivision[2].Add(entry);
                            }
                            else if (entry.Rating >= DivisionA.Item1 && entry.Rating <= DivisionA.Item2)
                            {
                                if (playersInDivision[1].Count < 3)
                                    playersInDivision[1].Add(entry);
                            }
                        }

                        for(int i = 1; i < 4; i++)
                        {
                            char divisionLetter = i == 1 ? 'A' : i == 2 ? 'B' : 'C';
                            text.Add($"Игроки дивизиона <b>{divisionLetter}</b>:");
                            foreach(var entry in playersInDivision[i])
                                text.Add($"<b> - {entry.Rank}) {entry.Username}</b>. Рейтинг: {entry.Rating}");
                        }
                        text.Add("");
                        text.Add("<b>Остальной рейтинг и баллы за турнир:</b>");
                        text.Add("");
                        foreach (var entry in tournamentSheet)
                        {
                            bool inDivision = false;
                            for(int i = 1; i < 4; i++)
                                if (playersInDivision[i].Contains(entry))
                                {
                                    inDivision = true;
                                    break;
                                }
                            if(entry.Sheet != null)
                            {
                                int totalScore = -1;
                                int zeroNumbers = entry.Sheet.Scores.Count(c => c == '0');
                                int twoNumbers = entry.Sheet.Scores.Count(c => c == '2');
                                int fourNumbers = entry.Sheet.Scores.Count(c => c == '4');
                                int total = zeroNumbers + twoNumbers + fourNumbers;
                                string studentName = "Имя студента не найдено";
                                if (users.TryGetValue(entry.Username, out User? student) && student.HasValue)
                                    studentName = student.Value.Name;

                                if (inDivision)
                                    totalScore = 1;
                                else if (total >= 7 && twoNumbers >= 1)
                                    totalScore = 0;
                                
                                if(totalScore != -1)
                                    csv.Add($"{studentName};{entry.Username};{totalScore}");
                                text.Add($"<b>{entry.Rank}) {entry.Username}</b>, {studentName}. Балл: {(totalScore == -1 ? "-" : totalScore)}");
                            }
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
                        using var stream = File.Open(csvFilePath, FileMode.Open);
                        message.WithFile(stream, "Table.csv");
                        await args.Reply(string.Join('\n', text));
                        await args.Reply(message);
                    }
                    else
                        await args.Reply("Турнир не был найден!");
                }
                else
                    await args.Reply("Турнир не сохранён с помощью команды /savearena!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /arenaresult \"ID турнира\"");
        }

        [Command("getplayerscore", "Помогает узнать, прошёл ли человек турнир", admin: true)]
        private async Task GetPlayerScore(CommandArgs args)
        {
            if (args.Parameters.Count == 2)
            {
                var tournamentId = args.Parameters[0];
                var name = args.Parameters[1];

                var tournament = await Program.Lichess.GetTournament(tournamentId); 
                
                var directory = Path.Combine(Environment.CurrentDirectory, "Tournaments");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                var filePath = Path.Combine(directory, tournamentId + ".txt");
                if (File.Exists(filePath))
                {
                    var tournamentSheet = await Program.Lichess.GetTournamentSheet(File.OpenText(filePath)); 
                    if (tournament != null && tournamentSheet != null)
                    {
                        using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE Name='{name}'");
                        Lichess.Types.User? lichessUser = null;
                        if (reader.Read())
                        {
                            lichessUser = await Program.Lichess.GetUserAsync(reader.Get("LichessName"));
                        }
                        else
                            lichessUser = await Program.Lichess.GetUserAsync(name);
                        if (lichessUser != null)
                        {
                            foreach (var player in tournamentSheet)
                            {
                                if (player.Sheet != null && player.Username == lichessUser.Username)
                                {
                                    List<string> text = [
                                        $"Турнир <b>{tournament.FullName}</b>. Состоялся <b>{tournament.Started:g}</b>",
                                        $"Информация об участнике турнира <b>{player.Username}</b>:",
                                        $"<b>Ранг:</b> {player.Rank}",
                                        $"<b>Набрано очков:</b> {player.Score}",
                                        $"<b>Итоговая строка:</b> {player.Sheet.Scores}",
                                        ];

                                    if(player.Team != null)
                                    {
                                        var team = await Program.Lichess.GetTeamAsync(player.Team);
                                        if (team != null)
                                            text.Add($"<b>Команда:</b> {team.Name}");
                                    }

                                    int zeroNumbers = player.Sheet.Scores.Count(c => c == '0');
                                    int twoNumbers = player.Sheet.Scores.Count(c => c == '2');
                                    int fourNumbers = player.Sheet.Scores.Count(c => c == '4');
                                    int total = zeroNumbers + twoNumbers + fourNumbers;
                                    if (total >= 7 && twoNumbers >= 1)
                                    {
                                        text.Add("По критериям регламента игрок <b>участвовал</b> в турнире");
                                    }
                                    else
                                    {
                                        text.Add("По критериям регламента игрок <b>не участвовал</b> в турнире");
                                    }
                                    TelegramMessageBuilder message = string.Join('\n', text);
                                    InlineKeyboardButton playerInfo = new($"🔍Информация об игроке {lichessUser.Username}");
                                    playerInfo.SetData("UserInfo", ("Name", lichessUser.Username));
                                    message.AddButton(playerInfo);
                                    await args.Reply(message);
                                    return;
                                }
                            }
                            await args.Reply($"Ученик {name} не участовал в турнире");
                        }
                        else
                            await args.Reply("Аккаунт Lichess не найден!");
                    }
                    else
                        await args.Reply("Турнир не был найден!");
                }
                else
                    await args.Reply("Турнир не сохранён с помощью команды /savearena!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /getplayerscore \"ID турнира\" \"Ник ученика\"");
        }


        [Command("admin", "Работает с полезными ссылками", admin: true)]
        private async Task AdminHelpLinks(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                var adminType = args.Parameters[0].ToLower();
                if (adminType.StartsWith('f'))
                    await FAQAdmin.Send(args.Bot, args.Message.Chat.Id);
                else if (adminType.StartsWith('h'))
                    await HelpAdmin.Send(args.Bot, args.Message.Chat.Id);
                else
                    await args.Reply("Панель не найдена! Попробуйте /admin faq/helplinks");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /admin faq/helplinks");
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

        private async Task HandleFAQDelete(ButtonInteractArgs args, List<FAQEntry> links)
        {
            if (links.Count != 0)
            {
                var link = links[0];
                FAQEntries.Remove(link);
                Program.Data.Query("DELETE FROM FAQ WHERE ID=@0", link.ID);
                if (args.Query.Message != null)
                {
                    await args.Bot.DeleteMessageAsync(args.Query.Message.Chat.Id, args.Query.Message.MessageId);
                    await args.Bot.SendMessage("Вопрос был успешно удалён!", args.Query.Message.Chat.Id);
                }
            }
            else
                await args.Reply("Не найдено вопросов!");
        }

        private List<HelpLink> GetHelpLinksValue() => HelpLinks;

        private string ConvertHelpLinkToString(HelpLink link, int index)
        {
            return $"<b>{link.Title}</b>\n{link.Text}\n<i>{link.Footer}</i>";
        }

        private string? GetHelpLinkDocumentID(HelpLink link) => link.FileID;

        [Command("fileinfo", "Выдаёт информацию о файле", admin: true)]
        public async Task GetFileInfo(CommandArgs args)
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

        [Command("faq", "Выдаёт список с FAQ", true)]
        public async Task FAQ(CommandArgs args)
        {
            await FAQMessage.Send(args.Bot, args.Message.Chat.Id);
        }

        private List<FAQEntry> GetFAQValues() => FAQEntries;

        private string ConvertFAQEntryToString(FAQEntry entry, int index)
        {
            return $"{index + 1}) <b>{entry.Question}</b>\n - {entry.Answer}";
        }

        [Command("addfaq", "Создаёт частозадаваемый вопрос", admin: true)]
        public async Task AddFAQ(CommandArgs args)
        {
            if (args.Parameters.Count == 2)
            {
                FAQEntry entry = new(default, args.Parameters[0], args.Parameters[1]);
                string text = "INSERT INTO FAQ (Question, Answer) VALUES (@0, @1);";
                int id = Program.Data.QueryScalar<int>(text + "SELECT CAST(last_insert_rowid() as INT);", entry.Question, entry.Answer);
                entry.ID = id;
                FAQEntries.Add(entry);
                await args.Reply($"Вопрос <b>{entry.Question}</b> и ответ на него <b>{entry.Answer}</b> были успешно добавлены");
            }
            else
                await args.Reply("Ошибка синтаксиса! Правильно: /addFAQ \"вопрос\" \"ответ\"");
        }

        [Command("addhelp", "Создаёт частозадаваемый вопрос", admin: true)]
        public async Task AddHelpLink(CommandArgs args)
        {
            if (args.Parameters.Count == 2)
            {
                if (args.Message.Document != null)
                {
                    HelpLink link = new(default, args.Parameters[0], args.Parameters[1], "", args.Message.Document.FileId);
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
                await args.Reply("Ошибка синтаксиса! Правильно: /addhelp \"название\" \"текст\". Чтобы добавить файл - прикрепите его к сообщению с командой");
        }

        [Command("cstats", "Покажет характеристики канала", admin: true)]
        public async Task Stats(CommandArgs args)
        {
            await args.Reply($"Айди канала: {args.Message.Chat.Id}");
        }

        [Command("users", "Покажет пользователей", admin: true)]
        public async Task GetUsers(CommandArgs args)
        {
            List<User> users = [];
            using var reader = Program.Data.SelectQuery("SELECT * FROM Users");
            while (reader.Read())
                users.Add(new(reader.Get<long>("TelegramID"), reader.Get("Name"), reader.Get("LichessName"), reader.Get<int>("Year")));
            await args.Reply($"Пользователи: {string.Join("\n", users)}");
        }

        [Command("userinfo", "Покажет пользователя", admin: true)]
        public async Task GetUserInfo(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                string name = string.Join(" ", args.Parameters);
                using var reader = Program.Data.SelectQuery($"SELECT * FROM Users WHERE Name='{name}'");
                if (reader.Read())
                {
                    var lichessUser = await Program.Lichess.GetUserAsync(reader.Get("LichessName"));

                    if (lichessUser != null)
                    {
                        TelegramMessageBuilder message = await GenerateUserInfo(lichessUser);
                        message.Text = "Информация об ученике <b>{name}</b>\n" + message.Text;
                        await args.Reply(message);
                    }
                    else
                        await args.Reply("Аккаунт Lichess не найден!");
                }
                else
                    await args.Reply("Ученик не найден!");
            }
            else
                await args.Reply("Неправильный синтаксис! Правильно: /userinfo \"ник\"");
        }

        [Button("TeamInfo")]
        internal async Task SendTeamInfo(ButtonInteractArgs args)
        {
            var teamID = args.Get<string>("ID");
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
            var name = args.Get<string>("Name");
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

        private async Task<TelegramMessageBuilder> GenerateUserInfo(Lichess.Types.User user)
        {
            var teams = await Program.Lichess.GetUserTeamsAsync(user.Username);
            TelegramMessageBuilder message = new();
            List<string> text = 
                [
                    $"<b>Имя аккаунта на Lichess:</b> {user.Username}",
                    $"<b>Дата регистрации:</b> {user.RegisterDate:g}",
                    $"<b>Последний вход:</b> {user.LastSeenDate:g}",
                    $"<b>Команды:</b> {user.LastSeenDate:g}",
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

        private struct User(long telegramID, string name, string lichessName, long year)
        {
            public long TelegramID = telegramID;

            public string Name = name;

            public string LichessName = lichessName;

            public long Year = year;

            public override readonly string ToString()
            {
                return $"{Name} '{LichessName}' ({TelegramID}), Курс - {Year}";
            }
        }
    }
}