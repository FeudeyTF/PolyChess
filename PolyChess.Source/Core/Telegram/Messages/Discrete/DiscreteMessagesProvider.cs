using PolyChess.Core.Telegram.Messages.Discrete.Messages;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChess.Core.Telegram.Messages.Discrete
{
    internal class DiscreteMessagesProvider
    {
        private readonly Dictionary<ChatId, DiscreteMessage> _activeMessages;

        private readonly ITelegramProvider _telegramProvider;

        public DiscreteMessagesProvider(ITelegramProvider provider)
        {
            _activeMessages = [];
            _telegramProvider = provider;
            _telegramProvider.OnMessage += HandleTelegramMessage;
        }

        public bool TryAddMessage(DiscreteMessage message, ChatId chatId)
        {
            return _activeMessages.TryAdd(chatId, message);
        }

        private async Task HandleTelegramMessage(ITelegramBotClient client, Message message, CancellationToken token)
        {
            if (_activeMessages.TryGetValue(message.Chat.Id, out var dMessage))
            {
                var lastMessageSended = await dMessage.HandleTelegramMessage(client, message, token);
                if (lastMessageSended)
                    _activeMessages.Remove(message.Chat.Id);
            }
        }
    }
}
