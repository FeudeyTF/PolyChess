using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChess.Core.Telegram
{
    internal interface ITelegramMessage
    {
        public Task SendAsync(ITelegramBotClient client, ChatId chatId, CancellationToken token);

        public Task EditAsync(ITelegramBotClient client, Message oldMessage, CancellationToken token);
    }
}
