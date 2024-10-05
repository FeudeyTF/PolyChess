using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Database;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot
{
    public class BotCommands
    {
        internal readonly ListMessage<FAQEntry> FAQMessage;

        internal readonly ListMessage<HelpLink> HelpMessage;

        internal readonly ListMessage<HelpLink> HelpAdmin;

        internal readonly ListMessage<FAQEntry> FAQAdmin;

        private readonly List<FAQEntry> FAQEntries;

        private readonly List<HelpLink> HelpLinks;

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
            List<string> message =
            [
                "🛠<b>Информация о боте</b>🛠",
                $"👨🏻‍💻<b>Разработчик:</b> {Program.MainConfig.BotAuthor}",
                $"🔀<b>Версия бота:</b> v.{Program.Version}",
                $"🕐<b>Дата последнего обновления:</b> Неизвестно",
                $"⏱<b>Время работы:</b> {(DateTime.Now - Program.Started).ToString("%d' дн. '%h' ч. '%m' мин. '%s' сек.'")}"
            ];
            await args.Reply(string.Join("\n", message), parseMode: ParseMode.Html);
        }

        [Command("question", "Синтаксис: /question \"вопрос\". Команда отправит вопрос напрямую Павлу", true)]
        public async Task Question(CommandArgs args)
        {
            string question = string.Join(" ", args.Parameters);
            if (!string.IsNullOrEmpty(question))
            {
                List<string> message =
                [
                    "<b><u>Вопрос от пользователя!</u></b>🙋‍",
                    $"👤<b>Ник пользователя:</b> @{args.User.Username}",
                    $"👤<b>Имя пользователя:</b> {args.User.FirstName} {args.User.LastName}",
                    $"🕑<b>Дата отправки:</b> {args.Message.Date:G}",
                    $"❓<b>Вопрос:</b>\n{question}"
                ];
                var data = TelegramButtonData.GetDataString("QuestionDataID", ("ID", args.User.Id), ("ChannelID", args.Message.MessageId));
                InlineKeyboardMarkup uesrInfo = new(new InlineKeyboardButton("Данные") { CallbackData = data });
                await args.Bot.SendTextMessageAsync(Program.MainConfig.QuestionChannel, string.Join("\n", message).RemoveBadSymbols(), parseMode: ParseMode.Html, replyMarkup: uesrInfo);
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
                    await args.Bot.SendTextMessageAsync(args.Query.Message.Chat.Id, "Полезная ссылка была успешно удалена!");
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
                    await args.Bot.SendTextMessageAsync(args.Query.Message.Chat.Id, "Вопрос был успешно удалён!");
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

                if(documentInfo.HasValue)
                {
                    string message = $"Информация о файле '{documentInfo.Value.FileName}'\n";
                    message += $"Имя: {documentInfo.Value.FileName}\n";
                    message += $"Размер: {documentInfo.Value.FileSize}\n";
                    message += $"Unique ID: {documentInfo.Value.FileUniqueId}\n";
                    message += $"File ID: {documentInfo.Value.FileID}";
                    await args.Reply(message, parseMode: ParseMode.Html);
                }
                else
                    await args.Reply("Нужно ответить на сообщение с файлом!", parseMode: ParseMode.Html);
            }
            else
                await args.Reply("Нужно ответить на сообщение с файлом!", parseMode: ParseMode.Html);
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
                await args.Reply($"Вопрос <b>{entry.Question}</b> и ответ на него <b>{entry.Answer}</b> были успешно добавлены", parseMode: ParseMode.Html);
            }
            else
                await args.Reply("Ошибка синтаксиса! Правильно: /addFAQ \"вопрос\" \"ответ\"");
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
                users.Add(new(reader.Get<int>("TelegramID"), reader.Get("Name"), reader.Get<int>("Year")));
            await args.Reply($"Пользователи: {string.Join("\n", users)}");
        }

        private struct User(long telegramID, string name, long year)
        {
            public long TelegramID = telegramID;

            public string Name = name;

            public long Year = year;

            public override readonly string ToString()
            {
                return $"{Name} ({TelegramID}), Курс - {Year}";
            }
        }
    }
}