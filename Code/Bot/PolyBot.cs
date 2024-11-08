using PolyChessTGBot.Bot.Buttons;
using PolyChessTGBot.Bot.Commands.Basic;
using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Extensions;
using PolyChessTGBot.Hooks;
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

        public readonly CommandRegistrator CommandRegistrator;

        public readonly DiscreteCommandRegistrator DiscreteCommandRegistrator;

        public User? TelegramUser;

        private readonly ReceiverOptions BotReceiverOptions;

        private readonly ButtonsRegistrator ButtonRegistrator;

        private readonly ILog Logger;

        private readonly BotCommands.BotCommands Commands;

        public PolyBot(ILog logger)
        {
            Telegram = new(Program.MainConfig.BotToken);
            BotReceiverOptions = new()
            {
                ThrowPendingUpdates = true,
            };
            Logger = logger;
            CommandRegistrator = new();
            DiscreteCommandRegistrator = new();
            ButtonRegistrator = new();
            Commands = new();
        }

        public async Task LoadBot()
        {
            using CancellationTokenSource cancellationTokenSource = new();
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
            DiscreteCommandRegistrator.RegisterCommands(Commands);
            ButtonRegistrator.RegisterButtons();
            ButtonRegistrator.RegisterButtons(Commands);
            await Telegram.DeleteMyCommandsAsync();

            Dictionary<BotCommandScopeType, List<BotCommand>> dic = [];
            foreach (var commandList in CommandRegistrator.GetCommandsInTelegram())
                if (dic.TryGetValue(commandList.Key, out var val))
                    val.AddRange(commandList.Value);
                else
                    dic.Add(commandList.Key, commandList.Value);
            foreach (var commandList in DiscreteCommandRegistrator.GetCommandsInTelegram())
                if (dic.TryGetValue(commandList.Key, out var val))
                    val.AddRange(commandList.Value);
                else
                    dic.Add(commandList.Key, commandList.Value);
            foreach (var commandList in dic)
                await Program.Bot.Telegram.SetMyCommandsAsync(commandList.Value, Utils.GetScopeByType(commandList.Key));
           
        }

        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            BotUpdateEventArgs updateArgs = new(Telegram, update);
            await BotHooks.InvokeOnBotUpdate(updateArgs);
            if (updateArgs.Handled)
                return;
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
                            if (Program.MainConfig.ShowButtonInteractLogs)
                            {
                                var user = args.Query.From;
                                Logger.Write($"Получено нажатие кнопки: [{user.FirstName} {user.LastName} (@{user.Username})]: Data: {args.Query.Data}", LogType.Info);
                            }
                            ButtonHooks.InvokeButtonInteract(args);
                            await args.Bot.AnswerCallbackQueryAsync(args.Query.Id, cancellationToken: token);
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
                {
                    if(!await CommandRegistrator.ExecuteCommand(text, message, user, token))
                        if(!await DiscreteCommandRegistrator.ExecuteCommand(text, message, user, token))
                            await Program.Bot.Telegram.SendMessage(new TelegramMessageBuilder("Команда не была найдена!").ReplyTo(message.MessageId), message.Chat.Id);
                }

                if (text == null && message.Caption != null && message.Caption.StartsWith('/'))
                {
                    if (!await CommandRegistrator.ExecuteCommand(message.Caption, message, user, token))
                        if (!await DiscreteCommandRegistrator.ExecuteCommand(message.Caption, message, user, token))
                            await Program.Bot.Telegram.SendMessage(new TelegramMessageBuilder("Команда не была найдена!").ReplyTo(message.MessageId), message.Chat.Id);

                }


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
                                    var userID = data.GetLongNumber("ID");
                                    var questionChannelID = data.GetNumber("ChannelID");
                                    if (userID != default && questionChannelID != default)
                                    {
                                        var msg = new TelegramMessageBuilder($"❗️Получен **ответ** на ваш вопрос от {user.FirstName} {user.LastName}:\n{message.Text}".RemoveBadSymbols())
                                            .ReplyTo(questionChannelID)
                                            .WithParseMode(ParseMode.MarkdownV2)
                                            .WithToken(token);
                                        await Telegram.SendMessage(msg, userID);
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
                    => !Program.MainConfig.SkippingApiRequestErrors.Contains(apiRequestException.ErrorCode) ?
                        $"[Telegram Ошибка] [{apiRequestException.ErrorCode}]: {apiRequestException.Message}" :
                        "",
                    
                RequestException =>
                    "Потеряно соединение с ботом. Переподключение...",
                _ => exception.ToString()
            };
            if(!string.IsNullOrEmpty(message))
            {
                Logger.Write(message, LogType.Error);
                foreach (var debugChatID in Program.MainConfig.DebugChats)
                    await Telegram.SendMessage(new TelegramMessageBuilder(message).WithToken(token), debugChatID);
            }
        }

        private async ValueTask HandleMakingApiRequest(ITelegramBotClient bot, ApiRequestEventArgs args, CancellationToken cancellationToken = default)
        {
            if (args.HttpRequestMessage != null && args.HttpRequestMessage.Content != null && Program.MainConfig.ShowApiResponseLogs)
                Console.WriteLine("MAKING API REQUEST:\n" + (await args.HttpRequestMessage.Content.ReadAsStringAsync()));
        }

        private async ValueTask HandleApiResponseRecieved(ITelegramBotClient bot, ApiResponseEventArgs args, CancellationToken cancellationToken = default)
        {
            if (Program.MainConfig.ShowApiResponseLogs)
                Console.WriteLine("RECIEVING API RESPONSE:\n" + (await args.ResponseMessage.Content.ReadAsStringAsync()));
        }
    }
}
