using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Hooks
{
    public class BotHooks
    {
        public static event Func<TelegramBotClient, Update, Task> OnBotUpdate;

        static BotHooks()
        {
            OnBotUpdate = (bot, args) => Task.CompletedTask;
        }

        internal static async Task InvokeOnBotUpdate(TelegramBotClient bot, Update update)
            => await OnBotUpdate(bot, update);
    }
}
