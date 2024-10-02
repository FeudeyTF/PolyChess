using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands;
using PolyChessTGBot.Logs;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot
{
    internal class PolyBot
    {
        public readonly TelegramBotClient Telegram;

        public User? TelegramUser;

        private readonly ReceiverOptions BotReceiverOptions;

        private readonly CommandRegistrator CommandRegistrator;

        private readonly ButtonsRegistrator ButtonRegistrator;

        private readonly ILog Logger;

        private BotCommands Commands;

        public PolyBot(ILog logger)
        {
            Telegram = new TelegramBotClient(Program.MainConfig.BotToken);
            
            BotReceiverOptions = new ReceiverOptions
            {
                ThrowPendingUpdates = true,
            };
            Logger = logger;
            CommandRegistrator = new();
            ButtonRegistrator = new();
            Commands = new();
        }

        public async Task LoadBot()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            try
            {
                Telegram.StartReceiving(UpdateHandler, ErrorHandler, BotReceiverOptions, cancellationTokenSource.Token);
                TelegramUser = await Telegram.GetMeAsync();
            }
            catch (Exception)
            {
                Program.Logger.Write($"Бот не был запущен из-за ошибки!", LogType.Error);
                Environment.Exit(0);
            }

            Telegram.OnApiResponseReceived += HandleApiResponseRecieved;
            Telegram.OnMakingApiRequest += HandleMakingApiRequest;

            Logger.Write($"{TelegramUser.FirstName} запущен!", LogType.Info);
            CommandRegistrator.RegisterCommands(Commands);
            ButtonRegistrator.RegisterButtons();
            foreach (var c in CommandRegistrator.Commands)
                Console.WriteLine(c.Name);
            //await CommandRegistrator.RegisterCommandsInTelegram();
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        if (update.Message != null)
                            await MessageRecieveHandler(update.Message, token);
                        break;
                    }
                case UpdateType.CallbackQuery:
                    if(update.CallbackQuery != null && update.CallbackQuery.Data != null)
                    {
                        var data = TelegramButtonData.ParseDataString(update.CallbackQuery.Data);
                        if (data != null)
                        {
                            var args = new ButtonInteractArgs(data.ButtonID, update.CallbackQuery, data);
                            await Commands.FAQMessage.TryUpdate(data.ButtonID, args);
                            foreach (var button in ButtonRegistrator.Buttons)
                                if (data.ButtonID == button.ID)
                                    await button.Delegate(new ButtonInteractArgs(data.ButtonID, update.CallbackQuery, data));

                        }
                    }
                    break;
                default:
                    Logger.Write("Получено обновление: " + update.Type, LogType.Info);
                    break;
            }
        }

        private async Task MessageRecieveHandler(Message message, CancellationToken token)
        {
            var user = message.From;
            if (user != null)
            {
                var text = message.Text;
                if (text != null && text.StartsWith('/'))
                    await CommandRegistrator.ExecuteCommand(text, message, user);

                if (message.Chat.Id == Program.MainConfig.QuestionChannel && message.ReplyToMessage != null && message.ReplyToMessage.ReplyMarkup != null)
                {
                    if (message.ReplyToMessage.ReplyMarkup.InlineKeyboard.Any())
                    {
                        var inlineKeyBoard = message.ReplyToMessage.ReplyMarkup.InlineKeyboard.First();
                        if (inlineKeyBoard.Any())
                        {
                            var dataButton = inlineKeyBoard.First();
                            if (!string.IsNullOrEmpty(dataButton.CallbackData))
                            {
                                var data = TelegramButtonData.ParseDataString(dataButton.CallbackData);
                                if (data != null)
                                {
                                    var userIDlong = data.Get<long>("ID");
                                    var userIDint = data.Get<int>("ID");
                                    var questionChannelID = data.Get<int>("ChannelID");
                                    if ((userIDlong != default || userIDint != default) && questionChannelID != default)
                                    {
                                        var userID = userIDlong == default ? userIDint : userIDlong;
                                        await Telegram.SendTextMessageAsync(userID, $"❗️Получен **ответ** на ваш вопрос от {user.FirstName} {user.LastName}:\n{message.Text}".RemoveBadSymbols(), replyToMessageId: questionChannelID, cancellationToken: token, parseMode: ParseMode.MarkdownV2);
                                    }
                                }
                            }
                        }
                    }
                }
                Logger.Write($"Получено сообщение: [{user.FirstName} {user.LastName} (@{user.Username}) в {message.Chat.Id}]: {message.Text}", LogType.Info);
            }
        }

        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            var message = exception switch
            {
                ApiRequestException apiRequestException
                    => $"[Telegram Ошибка]:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Logger.Write(message, LogType.Error);
            foreach (var debugChatID in Program.MainConfig.DebugChats)
                await Telegram.SendTextMessageAsync(debugChatID, message, cancellationToken: token);
        }

        private async ValueTask HandleMakingApiRequest(ITelegramBotClient bot, ApiRequestEventArgs args, CancellationToken cancellationToken = default)
        {
            if (args.HttpRequestMessage != null && args.HttpRequestMessage.Content != null && Program.MainConfig.ShowApiResponseLogs)
                Console.WriteLine("MAKING API REQUEST: " + (await args.HttpRequestMessage.Content.ReadAsStringAsync()));
        }

        private async ValueTask HandleApiResponseRecieved(ITelegramBotClient bot, ApiResponseEventArgs args, CancellationToken cancellationToken = default)
        {
            if (Program.MainConfig.ShowApiResponseLogs)
                Console.WriteLine("RECIEVING API RESPONSE: " + (await args.ResponseMessage.Content.ReadAsStringAsync()));
        }
    }
}
