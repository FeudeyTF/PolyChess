using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Database;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot
{
    public class BotCommands
    {
        internal readonly ListMessage<FAQEntry> FAQMessage;

        public BotCommands()
        {
            FAQMessage = new("FAQ", GetValues, ProccessString)
            {
                Header = "❓<b>FAQ</b> шахмат❓ Все самые <b>часто задаваемые</b> вопросы собраны в одном месте:"
            };
        }

        [Command("question", "Синтаксис: /question \"вопрос\". Команда отправит вопрос напрямую Павлу", visible: true)]
        public async Task Question(CommandArgs args)
        {
            string question = string.Join(" ", args.Parameters);
            if (!string.IsNullOrEmpty(question))
            {
                List<string> message = new()
                {
                    "**__Вопрос от пользователя!__**🙋‍♂️",
                    $"👤**Ник пользователя:** @{args.User.Username}",
                    $"👤**Имя пользователя:** {args.User.FirstName} {args.User.LastName}",
                    $"🕑**Дата отправки:** {args.Message.Date:G}",
                    $"❓**Вопрос:**\n{question}"
                };
                var data = TelegramButtonData.GetDataString("QuestionDataID", ("ID", args.User.Id), ("ChannelID", args.Message.MessageId));
                InlineKeyboardMarkup uesrInfo = new(new InlineKeyboardButton("Данные") { CallbackData = data });
                await args.Bot.SendTextMessageAsync(Program.MainConfig.QuestionChannel, string.Join("\n", message).RemoveBadSymbols(), parseMode: ParseMode.MarkdownV2, replyMarkup: uesrInfo);
                await args.Reply("Ваш вопрос был успешно отправлен!");
            }
            else
                await args.Reply("Неправильно введён вопрос!");
        }

        [Command("FAQ", "Выдаёт список с FAQ", visible: true)]
        public async Task FAQ(CommandArgs args)
        {
            await FAQMessage.Send(args.Bot, args.Message.Chat.Id);
        }

        private List<FAQEntry> GetValues()
        {
            using var reader = Program.Data.SelectQuery("SELECT * FROM FAQ");
            List<FAQEntry> questions = new();
            while (reader.Read())
                questions.Add(new(reader.Get("Question"), reader.Get("Answer")));
            return questions;
        }

        private string ProccessString(FAQEntry entry, int index)
        {
            return $"{index + 1}) <b>{entry.Question}</b>\n - {entry.Answer}";
        }

        [Command("addfaq", "Создаёт частозадаваемый вопрос")]
        public async Task AddFAQ(CommandArgs args)
        {
            if (args.Parameters.Count == 2)
            {
                var question = args.Parameters[0];
                var answer = args.Parameters[1];
                Program.Data.Query("INSERT INTO FAQ (Question, Answer) VALUES (@0, @1)", question, answer);
                await args.Reply($"Вопрос <b>{question}</b> и ответ на него <b>{answer}</b> были успешно добавлены", parseMode: ParseMode.Html);
            }
            else
                await args.Reply("Ошибка синтаксиса! Правильно: /addFAQ \"вопрос\" \"ответ\"");
        }

        [Command("cstats", "Покажет характеристики канала")]
        public async Task Stats(CommandArgs args)
        {
            await args.Reply($"Айди канала: {args.Message.Chat.Id}");
        }

        [Command("users", "Покажет характеристики канала")]
        public async Task GetUsers(CommandArgs args)
        {
            List<User> users = new();
            using var reader = Program.Data.SelectQuery("SELECT * FROM Users");
            while (reader.Read())
                users.Add(new(reader.Get<int>("TelegramID"), reader.Get("Name"), reader.Get<int>("Year")));
            await args.Reply($"Пользователи: {string.Join("\n", users)}");
        }

        private struct User
        {
            public long TelegramID;

            public string Name;

            public long Year;

            public User(long telegramID, string name, long year)
            {
                TelegramID = telegramID;
                Name = name;
                Year = year;
            }

            public override string ToString()
            {
                return $"{Name} ({TelegramID}), Курс - {Year}";
            }
        }
    }
}