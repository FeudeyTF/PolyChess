using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Hooks
{
    public class BotHooks
    {
        public static event Func<BotUpdateEventArgs, Task> OnBotUpdate;

        static BotHooks()
        {
            OnBotUpdate = (args) => Task.CompletedTask;
        }

        internal static async Task InvokeOnBotUpdate(BotUpdateEventArgs args)
            => await OnBotUpdate(args);
    }

    public class BotUpdateEventArgs
    {
        public bool Handled;

        public TelegramBotClient Bot;

        public Update Update;

        public BotUpdateEventArgs(TelegramBotClient bot, Update update)
        {
            Bot = bot;
            Update = update;
        }
    }
}
