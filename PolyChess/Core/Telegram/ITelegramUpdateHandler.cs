using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChess.Core.Telegram
{
    internal interface ITelegramUpdateHandler
    {
        public UpdateType Type { get; }

        public Task<bool> HandleUpdate(ITelegramBotClient client, Update update, CancellationToken token);
    }
}
