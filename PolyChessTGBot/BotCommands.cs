using PolyChessTGBot.Bot.Commands;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot
{
    public class BotCommands
    {
        [Command("report", "test command")]
        public async Task Report(CommandArgs args)
        {
            await args.Bot.SendTextMessageAsync(Program.MainConfig.QuestionChannel, $"Было получено новое сообщение: {args}\n||{args.Message.From?.Id}||", parseMode: ParseMode.MarkdownV2);
        }

        [Command("test", "test command")]
        public async Task Test(CommandArgs args)
        {
            await args.Bot.SendTextMessageAsync(args.Message.Chat.Id, "test");
        }
    }
}