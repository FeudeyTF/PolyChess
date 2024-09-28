using PolyChessTGBot.Bot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot
{
    public class BotCommands
    {
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
                var data = Utils.GetDataString("QuestionDataID", ("ID", args.User.Id), ("ChannelID", args.Message.MessageId));
                InlineKeyboardMarkup uesrInfo = new(new InlineKeyboardButton("Данные") { CallbackData = data });
                await args.Bot.SendTextMessageAsync(Program.MainConfig.QuestionChannel, string.Join("\n", message).RemoveBadSymbols(), parseMode: ParseMode.MarkdownV2, replyMarkup: uesrInfo);
            }
            else
                await args.Reply("Неправильно введён вопрос!");
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