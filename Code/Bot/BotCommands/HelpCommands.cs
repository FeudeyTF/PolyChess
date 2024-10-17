using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Database;
using PolyChessTGBot.Externsions;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot.BotCommands
{
    public partial class BotCommands
    {
        private readonly ListMessage<FAQEntry> FAQMessage;

        private readonly ListMessage<HelpLink> HelpMessage;

        private readonly ListMessage<HelpLink> HelpAdmin;

        private readonly ListMessage<FAQEntry> FAQAdmin;

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

        private List<HelpLink> GetHelpLinksValue() => HelpLinks;

        private List<FAQEntry> GetFAQValues() => FAQEntries;

        private string ConvertHelpLinkToString(HelpLink link, int index)
            => $"<b>{link.Title}</b>\n{link.Text}\n<i>{link.Footer}</i>";

        private string? GetHelpLinkDocumentID(HelpLink link) => link.FileID;

        private string ConvertFAQEntryToString(FAQEntry entry, int index)
            => $"{index + 1}) <b>{entry.Question}</b>\n - {entry.Answer}";

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

        [Command("faq", "Выдаёт список с FAQ", true)]
        public async Task FAQ(CommandArgs args)
        {
            await FAQMessage.Send(args.Bot, args.Message.Chat.Id);
        }
    }
}
