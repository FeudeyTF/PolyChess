using PolyChessTGBot.Bot;
using PolyChessTGBot.Logs;
using PolyChessTGBot.Logs.LogTypes;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot
{
    public static class Program
    {
        public readonly static TelegramBotClient BotClient;

        public readonly static ReceiverOptions BotReceiverOptions;

        private static User? BotUser;

        public static ConfigFile MainConfig;

        public static readonly TextLog Logger;

        private static CommandRegistrator CommandRegistrator;

        static Program()
        {
            MainConfig = ConfigFile.Load("Main");
            CommandRegistrator = new();
            Logger = new(DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".log", MainConfig.LogsFolder);
            BotClient = new TelegramBotClient(MainConfig.BotToken);
            BotReceiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                    UpdateType.Message,
                },
                ThrowPendingUpdates = true,
            };
        }

        public async static Task Main(string[] args)
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            BotClient.StartReceiving(UpdateHandler, ErrorHandler, BotReceiverOptions, cancellationTokenSource.Token);
            BotUser = await BotClient.GetMeAsync();
            Logger.Write($"{BotUser.FirstName} запущен!", LogType.Info);
            CommandRegistrator.RegisterCommands<BotCommands>();
            await CommandRegistrator.RegisterCommandsInTelegram();
            await Task.Delay(-1);
        }

        private async static Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
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
                                        await BotClient.SendTextMessageAsync(realUserId, $"❗️Получен **ответ** на ваш вопрос от {user.FirstName} {user.LastName}:\n{update.Message.Text}".RemoveBadSymbols(), cancellationToken: token, parseMode: ParseMode.MarkdownV2);
                                }

                                Logger.Write($"Recieved Message: [{user.FirstName} {user.LastName} (@{user.Username})]: {update.Message.Text}", LogType.Info);
                            }
                        }
                        break;
                    }
            }
        }

        private async static Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            var message = exception switch
            {
                ApiRequestException apiRequestException
                    => $"[Telegram Bot Error]:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Logger.Write(message, LogType.Error);
            await Task.CompletedTask;
        }
    }
}