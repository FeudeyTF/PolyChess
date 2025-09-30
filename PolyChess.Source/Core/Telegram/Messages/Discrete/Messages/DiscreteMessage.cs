using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChess.Core.Telegram.Messages.Discrete.Messages
{
    internal class DiscreteMessage : ITelegramMessage
    {
        public readonly Func<DiscreteMessageEnteredArgs, Task> OnEntered;

        public readonly List<ITelegramMessage> Queries;

        private readonly Message[] _responses;

        private readonly DiscreteMessagesProvider _provider;

        private int _progress;

        public DiscreteMessage(DiscreteMessagesProvider provider, List<ITelegramMessage> queries, Func<DiscreteMessageEnteredArgs, Task> onEntered)
        {
            OnEntered = onEntered;
            Queries = queries;
            _responses = new Message[queries.Count];
            _provider = provider;
            _progress = 0;
        }

        public async Task<bool> HandleTelegramMessage(ITelegramBotClient client, Message message, CancellationToken token)
        {
            if (message.From == null)
                return false;
            if (_progress < Queries.Count)
            {
                _responses[_progress] = message;
                _progress++;
            }

            if (_progress == Queries.Count)
            {
                await OnEntered(new(_responses, client, message.Chat.Id, message.From, token));
                return true;
            }
            else
                await SendAsync(client, message.Chat.Id, token);
            return false;
        }

        public async Task SendAsync(ITelegramBotClient client, ChatId chatId, CancellationToken token)
        {
            if (_provider.TryAddMessage(this, chatId))
                await Queries[_progress].SendAsync(client, chatId, token);
        }

        public Task EditAsync(ITelegramBotClient client, Message oldMessage, CancellationToken token)
        {
            throw new Exception("Discrete message can't be edited");
        }
    }
}
