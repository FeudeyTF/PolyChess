using PolyChessTGBot.Bot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot
{
    public class BotCommands
    {
        [Command("question", "test command")]
        public async Task Question(CommandArgs args)
        {
            string question = string.Join(" ", args.Parameters);
            if(!string.IsNullOrEmpty(question))
            {
                List<string> message = new()
                {
                    "**__Вопрос от пользователя!__**🙋‍♂️",
                    $"👤**Ник пользователя:** {args.User.Username}",
                    $"👤**Имя пользователя:** {args.User.FirstName} {args.User.LastName}",
                    $"🕑**Дата отправки:** {args.Message.Date:G}",
                    $"❓**Вопрос:**\n{question}",
                    $"||{args.User.Id}||"
                };
                await args.Bot.SendTextMessageAsync(Program.MainConfig.QuestionChannel, string.Join("\n", message).RemoveBadSymbols(), parseMode: ParseMode.MarkdownV2);
            }
            else
                await args.Bot.SendTextMessageAsync(args.User.Id, "Неправильно введён вопрос!");
        }
        
        [Command("cstats", "test command")]
        public async Task Stats(CommandArgs args)
        {
            await args.Bot.SendTextMessageAsync(args.Message.Chat.Id, $"Айди канала: {args.Message.Chat.Id}");
        }
    }
}