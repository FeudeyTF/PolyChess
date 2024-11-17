using PolyChessTGBot.Bot.Messages;
using PolyChessTGBot.Bot.Messages.Discrete;
using PolyChessTGBot.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Buttons
{
    internal class ButtonInteractArgs
    {
        public TelegramBotClient Bot => Program.Bot.Telegram;

        public string ButtonID;

        public CallbackQuery Query;

        private readonly TelegramButtonData Data;

        public readonly CancellationToken Token;

        public ButtonInteractArgs(string id, CallbackQuery query, TelegramButtonData data, CancellationToken token)
        {
            ButtonID = id;
            Data = data;
            Query = query;
            Token = token;
        }

        public int GetNumber(string parameter)
            => Data.GetNumber(parameter);

        public long GetLongNumber(string parameter)
            => Data.GetLongNumber(parameter);

        public float GetFloat(string parameter)
            => Data.GetFloat(parameter);

        public string GetString(string parameter)
            => Data.GetString(parameter);

        public async Task Reply(TelegramMessageBuilder message)
        {
            if (Query.Message != null)
                await Bot.SendMessage(message.ReplyTo(Query.Message.MessageId).WithToken(Token), Query.Message.Chat.Id);
        }

        public async Task Reply(IEnumerable<string> text, string separator = "\n")
            => await Reply(string.Join(separator, text));

        public async Task SendDiscreteMessage(long channelId, List<TelegramMessageBuilder> queries, Func<DiscreteMessageEnteredArgs, Task> onEntered, Func<DiscreteMessageNextSendedArgs, Task>? onNextSended = default, Func<DiscreteMessageNextRecievedArgs, Task>? onNextRecieved = default, params List<object> data)
            => await DiscreteMessage.Send(channelId, queries, onEntered, Token, onNextSended, onNextRecieved, data);
    }
}
