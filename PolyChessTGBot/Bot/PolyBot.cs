using Telegram.Bot.Polling;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using PolyChessTGBot.Logs;
using Telegram.Bot.Exceptions;

namespace PolyChessTGBot.Bot
{
    internal class PolyBot
    {
        public readonly TelegramBotClient RealBot;

        public User? BotUser;

        private readonly ReceiverOptions BotReceiverOptions;

        private readonly CommandRegistrator CommandRegistrator;

        private ILog Logger;

        public PolyBot(ILog logger)
        {
            CommandRegistrator = new();
            RealBot = new TelegramBotClient(Program.MainConfig.BotToken);
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
                RealBot.StartReceiving(UpdateHandler, ErrorHandler, BotReceiverOptions, cancellationTokenSource.Token);
                BotUser = await RealBot.GetMeAsync();
            }
            catch (Exception)
            {
                Program.Logger.Write($"Бот не был запущен из-за ошибки!", LogType.Error);
                Environment.Exit(0);
            }

            Logger.Write($"{BotUser.FirstName} запущен!", LogType.Info);
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

                                if (update.Message.ReplyToMessage != null && update.Message.ReplyToMessage.Text != null)
                                {
                                    var userId = update.Message.ReplyToMessage.Text.Split("\n").Last().Replace("|", "");
                                    if (long.TryParse(userId, out long realUserId))
                                        await RealBot.SendTextMessageAsync(realUserId, $"❗️Получен **ответ** на ваш вопрос от {user.FirstName} {user.LastName}:\n{update.Message.Text}".RemoveBadSymbols(), cancellationToken: token, parseMode: ParseMode.MarkdownV2);
                                }

                                Logger.Write($"Recieved Message: [{user.FirstName} {user.LastName} (@{user.Username}) in {update.Message.Chat.Id}]: {update.Message.Text}", LogType.Info);
                            }
                        }
                        break;
                    }
                default:
                    Logger.Write("Recieved Update: " + update.Type, LogType.Info);
                    break;
            }
        }

        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            var message = exception switch
            {
                ApiRequestException apiRequestException
                    => $"[Telegram Bot Error]:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Logger.Write(message, LogType.Error);
            foreach (var debugChatID in Program.MainConfig.DebugChats)
                await RealBot.SendTextMessageAsync(debugChatID, message, cancellationToken: token);
        }
    }
}
