using PolyChessTGBot.Bot.Buttons;
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
            Telegram.OnMakingApiRequest += HandleMakingApiRequest; ;

            Logger.Write($"{TelegramUser.FirstName} запущен!", LogType.Info);
            CommandRegistrator.RegisterCommands(Commands);
            ButtonRegistrator.RegisterButtons();
            await CommandRegistrator.RegisterCommandsInTelegram();
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        if (update.Message != null)
                        {
                            var user = update.Message.From;
                            if (user != null)
                            {
                                var text = update.Message.Text;
                                if (text != null && text.StartsWith('/'))
                                    await CommandRegistrator.ExecuteCommand(text, update.Message, user);

                                if (update.Message.Chat.Id == Program.MainConfig.QuestionChannel && update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.ReplyMarkup != null)
                                {
                                    if (update.Message.ReplyToMessage.ReplyMarkup.InlineKeyboard.Any())
                                    {
                                        var inlineKeyBoard = update.Message.ReplyToMessage.ReplyMarkup.InlineKeyboard.First();
                                        if (inlineKeyBoard.Any())
                                        {
                                            var dataButton = inlineKeyBoard.First();
                                            if (!string.IsNullOrEmpty(dataButton.CallbackData))
                                            {
                                                var data = TelegramButtonData.ParseDataString(dataButton.CallbackData);
                                                if(data != null)
                                                {
                                                    var userIDlong = data.Get<long>("ID");
                                                    var userIDint = data.Get<int>("ID");
                                                    var questionChannelID = data.Get<int>("ChannelID");
                                                    if((userIDlong != default || userIDint != default) && questionChannelID != default)
                                                    {
                                                        var userID = userIDlong == default ? userIDint : userIDlong;
                                                        await Telegram.SendTextMessageAsync(userID, $"❗️Получен **ответ** на ваш вопрос от {user.FirstName} {user.LastName}:\n{update.Message.Text}".RemoveBadSymbols(), replyToMessageId: questionChannelID, cancellationToken: token, parseMode: ParseMode.MarkdownV2);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                Logger.Write($"Получено сообщение: [{user.FirstName} {user.LastName} (@{user.Username}) в {update.Message.Chat.Id}]: {update.Message.Text}", LogType.Info);
                            }
                        }
                        break;
                    }
                case UpdateType.CallbackQuery:
                    if(update.CallbackQuery != null && update.CallbackQuery.Data != null)
                    {
                        var data = TelegramButtonData.ParseDataString(update.CallbackQuery.Data);
                        if (data != null)
                        {
                            var args = new ButtonArgs(data.ButtonID, update.CallbackQuery, data);
                            await Commands.QnAMessage.TryUpdate(data.ButtonID, args);
                            foreach (var button in ButtonRegistrator.Buttons)
                                if (data.ButtonID == button.ID)
                                    await button.Delegate(new ButtonArgs(data.ButtonID, update.CallbackQuery, data));

                        }
                    }
                    break;
                default:
                    Logger.Write("Получено обновление: " + update.Type, LogType.Info);
                    break;
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

        private ValueTask HandleMakingApiRequest(ITelegramBotClient bot, ApiRequestEventArgs args, CancellationToken cancellationToken = default)
        {
            if (Program.MainConfig.ShowApiResponseLogs)
                Console.WriteLine("MAKING API REQUEST: " + args.HttpRequestMessage);
            return new ValueTask();
        }

        private ValueTask HandleApiResponseRecieved(ITelegramBotClient bot, ApiResponseEventArgs args, CancellationToken cancellationToken = default)
        {
            if (Program.MainConfig.ShowApiResponseLogs)
                Console.WriteLine("RECIEVING API RESPONSE: " + args.ResponseMessage);
            return new ValueTask();
        }
    }
}
