using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Externsions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot.Messages
{
    internal class ListMessage<TValue>
    {
        public string ID;

        public string Header;

        public string Footer;

        public Func<List<TValue>> GetValues;

        public Func<TValue, int, string> ProcessString;

        public Func<TValue, string?>? GetDocumentID;

        public int ValuesPerPage;

        public bool ShowPagesCount;

        public string NextButtonText;

        public string PreviousButtonText;


        public ListMessage(string id, Func<List<TValue>> getValues, Func<TValue, int, string> processString, int valuesPerPage = 10, bool showPagesCount = true, string nextButtonText = "➡️", string previousButtonText = "⬅️")
        {
            ID = id;
            Header = "";
            Footer = "";
            ValuesPerPage = valuesPerPage;
            GetValues = getValues;
            ProcessString = processString;
            ShowPagesCount = showPagesCount;
            NextButtonText = nextButtonText;
            PreviousButtonText = previousButtonText;
        }

        public async Task Send(TelegramBotClient bot, long channelID)
        {
            var values = GetValues();
            string message = Header + "\n";
            for (int i = 0; i < (values.Count > ValuesPerPage ? ValuesPerPage : values.Count); i++)
                message += ProcessString(values[i], i) + "\n";
            message += Footer;
            bool sentDocumentMessage = false;
            if (GetDocumentID != null && values.Count > 0)
            {
                var document = GetDocumentID(values[0]);
                if (!string.IsNullOrEmpty(document))
                {
                    sentDocumentMessage = true;
                    await bot.SendDocumentAsync(channelID, new InputFileId(document), replyMarkup: GenerateKeyBoard(1, GetPagesCount(values.Count)), caption: message, parseMode: ParseMode.Html);
                }
            }
            if(!sentDocumentMessage)
                await bot.SendTextMessageAsync(channelID, message, replyMarkup: GenerateKeyBoard(1, GetPagesCount(values.Count)), parseMode: ParseMode.Html);
        }

        private int GetPagesCount(int count)
        {
            if (count < ValuesPerPage)
                return 1;
            else if (count % ValuesPerPage == 0)
                return count / ValuesPerPage;
            else
                return count / ValuesPerPage + 1;
        }

        private InlineKeyboardMarkup? GenerateKeyBoard(int page, int pages)
        {
            if (pages == 1)
                return null;
            InlineKeyboardButton nextButton = new(NextButtonText);
            nextButton.SetData("Next" + ID, ("Page", page));
            InlineKeyboardButton pageButton = new($"{page}/{pages}");
            pageButton.SetData("Page" + ID);
            InlineKeyboardButton prevButton = new(PreviousButtonText);
            prevButton.SetData("Prev" + ID, ("Page", page));
            List<InlineKeyboardButton> movingButtons = new()
            {
                prevButton,
                nextButton
            };
            if (ShowPagesCount)
                movingButtons.Insert(1, pageButton);
            return new(movingButtons);
        }

        public async Task TryUpdate(string buttonID, ButtonInteractArgs args)
        {
            if (buttonID == "Next" + ID)
                await NextButton(args);
            else if (buttonID == "Prev" + ID)
                await PreviousButton(args);
        }

        public async Task NextButton(ButtonInteractArgs args)
        {
            if (args.Query != null && args.Query.Message != null)
            {
                var values = GetValues();
                int page = args.Get<int>("Page");
                int pages = GetPagesCount(values.Count);
                if (page > 0 && page < pages)
                {
                    string message = Header + "\n";
                    int startIndex = page * ValuesPerPage;
                    for (int i = startIndex; i < startIndex + (values.Count - startIndex > ValuesPerPage ? ValuesPerPage : values.Count - startIndex); i++)
                        message += ProcessString(values[i], i) + "\n";
                    message += Footer;
                    bool editedMessageWithFile = false;
                    if(GetDocumentID != null && values.Count > startIndex)
                    {
                        var document = GetDocumentID(values[startIndex]);
                        if (!string.IsNullOrEmpty(document))
                        {
                            editedMessageWithFile = true;
                            await args.Bot.EditMessageMediaAsync(args.Query.Message.Chat.Id, args.Query.Message.MessageId, new InputMediaDocument(new InputFileId(document)) { Caption = message, ParseMode = ParseMode.Html }, GenerateKeyBoard(page + 1, pages));
                        }
                    }
                    if(!editedMessageWithFile)
                        await args.Bot.EditMessageTextAsync(args.Query.Message.Chat.Id, args.Query.Message.MessageId, message, replyMarkup: GenerateKeyBoard(page + 1, pages), parseMode: ParseMode.Html);
                }
            }
        }

        public async Task PreviousButton(ButtonInteractArgs args)
        {
            if (args.Query != null && args.Query.Message != null)
            {
                var values = GetValues();
                int page = args.Get<int>("Page");
                int pages = GetPagesCount(values.Count);
                if (page > 1)
                {
                    string message = Header + "\n";
                    int startIndex = (page - 2) * ValuesPerPage;
                    for (int i = startIndex; i < startIndex + (values.Count - startIndex > ValuesPerPage ? ValuesPerPage : values.Count - startIndex); i++)
                        message += ProcessString(values[i], i) + "\n";
                    message += Footer;
                    bool editedMessageWithFile = false;
                    if (GetDocumentID != null && values.Count > startIndex)
                    {
                        var document = GetDocumentID(values[startIndex]);
                        if (!string.IsNullOrEmpty(document))
                        {
                            editedMessageWithFile = true;
                            await args.Bot.EditMessageMediaAsync(args.Query.Message.Chat.Id, args.Query.Message.MessageId, new InputMediaDocument(new InputFileId(document)) { Caption = message, ParseMode = ParseMode.Html }, replyMarkup: GenerateKeyBoard(page - 1, pages));
                        }
                    }
                    if (!editedMessageWithFile)
                        await args.Bot.EditMessageTextAsync(args.Query.Message.Chat.Id, args.Query.Message.MessageId, message, replyMarkup: GenerateKeyBoard(page - 1, pages), parseMode: ParseMode.Html);
                }
            }
        }
    }
}
