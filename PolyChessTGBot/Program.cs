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

        static Program()
        {
            MainConfig = ConfigFile.Load("Main");
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
            Console.WriteLine($"{BotUser.FirstName} запущен!");

            await Task.Delay(-1);
        }

        //Если будет нужен ассинхронный код, то этот метод можно сделать ассинхронным
        private static Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    {
                        if(update.Message != null)
                        {
                            var user =  update.Message.From;
                            if(user != null)
                                Console.WriteLine($"[{user.FirstName} {user.LastName}]: {update.Message.Text}");
                        }
                        break;
                    }
            }

            return Task.CompletedTask;
        }

        //Если будет нужен ассинхронный код, то этот метод можно сделать ассинхронным
        private static Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            var message = exception switch
            {
                ApiRequestException apiRequestException
                    => $"[Telegram Bot Error]:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(message);
            return Task.CompletedTask;
        }
    }
}