using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Database;
using PolyChessTGBot.Externsions;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        public static readonly string TempPath;

        static BotCommands()
        {
            TempPath = Path.Combine(Environment.CurrentDirectory, "Temp");
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
                $"🕐<b>Дата последнего обновления:</b> {File.GetLastWriteTime(exeFilePath):g}",
                $"⏱<b>Время работы:</b> {DateTime.Now - Program.Started:%d' дн. '%h' ч. '%m' мин. '%s' сек.'}"
            ];
            await args.Reply(string.Join("\n", message));
        }

        [Command("admin", "Работает с полезными ссылками", admin: true)]
        private async Task AdminHelpLinks(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                var adminType = args.Parameters[0].ToLower();
                if (adminType.StartsWith('f'))
                    await FAQAdmin.Send(args.Bot, args.Message.Chat.Id, args.User);
                else if (adminType.StartsWith('h'))
                    await HelpAdmin.Send(args.Bot, args.Message.Chat.Id, args.User);
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