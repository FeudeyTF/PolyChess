using PolyChessTGBot.Logs;
using Telegram.Bot;
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

        private ILog Logger;

        public PolyBot(ILog logger)
        {
            CommandRegistrator = new();
            Telegram = new TelegramBotClient(Program.MainConfig.BotToken);
            BotReceiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                },
                ThrowPendingUpdates = true,
            };
            Logger = logger;
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

            Logger.Write($"{TelegramUser.FirstName} запущен!", LogType.Info);
            CommandRegistrator.RegisterCommands<BotCommands>();
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

                                if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.ReplyMarkup != null)
                                {
                                    if (update.Message.ReplyToMessage.ReplyMarkup.InlineKeyboard.Any())
                                    {
                                        var inlineKeyBoard = update.Message.ReplyToMessage.ReplyMarkup.InlineKeyboard.First();
                                        if (inlineKeyBoard.Any())
                                        {
                                            var userId = inlineKeyBoard.First().CallbackData;
                                            if (long.TryParse(userId, out long realUserId))
                                                await Telegram.SendTextMessageAsync(realUserId, $"❗️Получен **ответ** на ваш вопрос от {user.FirstName} {user.LastName}:\n{update.Message.Text}".RemoveBadSymbols(), cancellationToken: token, parseMode: ParseMode.MarkdownV2);
                                        }
                                    }
                                }

                                Logger.Write($"Получено сообщение: [{user.FirstName} {user.LastName} (@{user.Username}) в {update.Message.Chat.Id}]: {update.Message.Text}", LogType.Info);
                            }
                        }
                        break;
                    }
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
    }
}
