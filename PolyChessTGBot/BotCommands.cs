using PolyChessTGBot.Bot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot
{
    public class BotCommands
    {
        [Command("question", "Синтаксис: /question \"вопрос\". Команда отправит вопрос напрямую Павлу")]
        public async Task Question(CommandArgs args)
        {
            string question = string.Join(" ", args.Parameters);
            if(!string.IsNullOrEmpty(question))
            {
                List<string> message = new()
                {
                    "**__Вопрос от пользователя!__**🙋‍♂️",
                    $"👤**Ник пользователя:** @{args.User.Username}",
                    $"👤**Имя пользователя:** {args.User.FirstName} {args.User.LastName}",
                    $"🕑**Дата отправки:** {args.Message.Date:G}",
                    $"❓**Вопрос:**\n{question}"
                };
                InlineKeyboardMarkup uesrInfo = new(new InlineKeyboardButton("Данные") { CallbackData = args.User.Id.ToString() });
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
    }
}