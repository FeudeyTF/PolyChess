using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Extensions;
using PolyChessTGBot.Hooks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Bot.Messages
{
    internal class ListMessage<TValue>
    {
        public string ID;

        public string Header;

        public string Footer;

        public Func<Task<List<TValue>>> GetValues;

        public Func<TValue, int, User, Task<string>> ConvertValueToString;

        public Func<TValue, string?>? GetDocumentID;

        public int ValuesPerPage;

        public bool ShowPagesCount;

        public string NextButtonText;

        public string PreviousButtonText;

        public List<List<ListMessageButton<List<TValue>>>> AdditionalKeyboards;

        public ListMessage(string id, Func<Task<List<TValue>>> getValues, Func<TValue, int, User, Task<string>> processString, int valuesPerPage = 10, bool showPagesCount = true, string nextButtonText = "➡️", string previousButtonText = "⬅️", List<List<ListMessageButton<List<TValue>>>>? additionalKeyboards = default)
        {
            ID = id;
            Header = "";
            Footer = "";
            ValuesPerPage = valuesPerPage;
            GetValues = getValues;
            ConvertValueToString = processString;
            ShowPagesCount = showPagesCount;
            NextButtonText = nextButtonText;
            PreviousButtonText = previousButtonText;
            AdditionalKeyboards = additionalKeyboards ?? [];
            ButtonHooks.OnButtonInteract += HandleButtonInteract;
        }

        private async Task HandleButtonInteract(ButtonInteractArgs args)
        {
            await TryUpdate(args.ButtonID, args);
        }

        public async Task Send(TelegramBotClient bot, long channelID, User user)
        {
            var values = await GetValues();
            if (values.Count != 0)
            {
                string text = Header + "\n";
                for (int i = 0; i < (values.Count > ValuesPerPage ? ValuesPerPage : values.Count); i++)
                    text += (await ConvertValueToString(values[i], i, user)) + "\n";
                text += Footer;
                TelegramMessageBuilder message = new(text);
                message.WithMarkup(GenerateKeyBoard(1, GetPagesCount(values.Count), user));

                if (GetDocumentID != null && values.Count > 0)
                {
                    var document = GetDocumentID(values[0]);
                    if (!string.IsNullOrEmpty(document))
                        message.WithFile(document);
                }
                message.WithoutWebPagePreview();
                await bot.SendMessage(message, channelID);
            }
            else
                await bot.SendMessage("Данных нет", channelID);
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

        private InlineKeyboardMarkup GenerateKeyBoard(int page, int pages, User user)
        {
            if (pages == 1)
                return new(GenerateAdditionalKeyboards(page));

            List<List<InlineKeyboardButton>> buttons = [];
            InlineKeyboardButton nextButton = new(NextButtonText);
            nextButton.SetData("Next" + ID, ("Page", page), ("TID", user.Id));
            InlineKeyboardButton pageButton = new($"{page}/{pages}");
            pageButton.SetData("Page" + ID, ("Page", page));
            InlineKeyboardButton prevButton = new(PreviousButtonText);
            prevButton.SetData("Prev" + ID, ("Page", page), ("TID", user.Id));
            List<InlineKeyboardButton> movingButtons = [];
            if (page != 1)
                movingButtons.Add(prevButton);
            if (ShowPagesCount)
                movingButtons.Add(pageButton);
            if (page != pages)
                movingButtons.Add(nextButton);
            buttons.Add(movingButtons);
            foreach (var keyboard in GenerateAdditionalKeyboards(page))
                buttons.Add(keyboard);
            return new(buttons);
        }

        private List<List<InlineKeyboardButton>> GenerateAdditionalKeyboards(int page)
        {
            List<List<InlineKeyboardButton>> buttons = [];
            foreach (var keyboard in AdditionalKeyboards)
            {
                List<InlineKeyboardButton> additionalButtons = [];
                foreach (var b in keyboard)
                {
                    InlineKeyboardButton additionalButton = new(b.Name);
                    additionalButton.SetData(b.ID + ID, ("Page", page));
                    additionalButtons.Add(additionalButton);
                }
                buttons.Add(additionalButtons);
            }
            return buttons;
        }

        public async Task TryUpdate(string buttonID, ButtonInteractArgs args)
        {
            if (buttonID == "Next" + ID)
                await NextButton(args);
            else if (buttonID == "Prev" + ID)
                await PreviousButton(args);
            else
            {
                foreach (var keyboard in AdditionalKeyboards)
                    foreach(var button in keyboard)
                        if (button.ID + ID == args.ButtonID)
                            await button.Delegate(args, await FindValues(args.GetNumber("Page")));
            }
        }

        public async Task NextButton(ButtonInteractArgs args)
        {
            if (args.Query != null && args.Query.Message != null)
            {
                var values = await GetValues();
                int page = args.GetNumber("Page");
                int pages = GetPagesCount(values.Count);
                var userID = args.GetLongNumber("TID");
                var user = userID == args.Query.From.Id ? args.Query.From : new User() { Id = userID };
                if (page > 0 && page < pages)
                {
                    string text = Header + "\n";
                    int startIndex = page * ValuesPerPage;
                    for (int i = startIndex; i < startIndex + (values.Count - startIndex > ValuesPerPage ? ValuesPerPage : values.Count - startIndex); i++)
                        text += (await ConvertValueToString(values[i], i, user)) + "\n";
                    text += Footer;
                    var message = new TelegramMessageBuilder(text)
                        .WithMarkup(GenerateKeyBoard(page + 1, GetPagesCount(values.Count), user));
                    if(GetDocumentID != null && values.Count > startIndex)
                    {
                        var document = GetDocumentID(values[startIndex]);
                        if (!string.IsNullOrEmpty(document))
                            message.WithFile(document);
                    }
                    message.WithoutWebPagePreview();
                    await args.Bot.EditMessage(message, args.Query.Message.Chat.Id, args.Query.Message);
                }
            }
        }

        public async Task PreviousButton(ButtonInteractArgs args)
        {
            if (args.Query != null && args.Query.Message != null)
            {
                var values = await GetValues();
                int page = args.GetNumber("Page");
                int pages = GetPagesCount(values.Count);
                var userID = args.GetLongNumber("TID");
                var user = userID == args.Query.From.Id ? args.Query.From : new User() { Id = userID };
                if (page > 1)
                {
                    string text = Header + "\n";
                    int startIndex = (page - 2) * ValuesPerPage;
                    for (int i = startIndex; i < startIndex + (values.Count - startIndex > ValuesPerPage ? ValuesPerPage : values.Count - startIndex); i++)
                        text += (await ConvertValueToString(values[i], i, user)) + "\n";
                    text += Footer;
                    var message = new TelegramMessageBuilder(text)
                        .WithMarkup(GenerateKeyBoard(page - 1, GetPagesCount(values.Count), user));

                    if (GetDocumentID != null && values.Count > startIndex)
                    {
                        var document = GetDocumentID(values[startIndex]);
                        if (!string.IsNullOrEmpty(document))
                            message.WithFile(document);
                    }
                    message.WithoutWebPagePreview();
                    await args.Bot.EditMessage(message, args.Query.Message.Chat.Id, args.Query.Message);
                }
            }
        }

        public async Task<TValue?> FindValue(int index)
        {
            var values = await GetValues();
            if (values.Count > index)
                return values[index];
            return default;
        }

        public async Task<List<TValue>> FindValues(int page)
        {
            List<TValue> result = [];
            var values = await GetValues();
            int startIndex = (page - 1) * ValuesPerPage;
            for (int i = startIndex; i < startIndex + (values.Count - startIndex > ValuesPerPage ? ValuesPerPage : values.Count - startIndex); i++)
                result.Add(values[i]);
            return result;
        }
    }

    internal class ListMessageButton<TValue>
    {
        public string Name;

        public string ID;

        public Func<ButtonInteractArgs, TValue, Task> Delegate;

        public ListMessageButton(string name, string id, Func<ButtonInteractArgs, TValue, Task> handler)
        {
            Name = name;
            ID = id;
            Delegate = handler;
        }
    }
}
