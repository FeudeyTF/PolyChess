using PolyChessTGBot.Extensions;
using PolyChessTGBot.Hooks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot.Messages.Discrete
{
    public class DiscreteMessage : IDisposable
    {
        private static Dictionary<long, DiscreteMessage> ActiveChannels;

        static DiscreteMessage()
        {
            ActiveChannels = [];
        }

        public static bool CanSendMessage(long channelId) => !ActiveChannels.ContainsKey(channelId);

        /// <summary>
        /// Отправляет разделённое сообщение, создавая новый объект. 
        /// </summary>
        public static async Task Send(long channelId, List<TelegramMessageBuilder> questions, Func<DiscreteMessageEnteredArgs, Task> onEntered, params List<object> data)
        {
            if(CanSendMessage(channelId))
            {
                DiscreteMessage msg = new(questions, onEntered);
                await msg.Send(channelId, data);
                ActiveChannels.Add(channelId, msg);
            }
        }

        private readonly Dictionary<long, ChannelInfo> Channels;

        private readonly Func<DiscreteMessageEnteredArgs, Task> OnEntered;

        private readonly List<TelegramMessageBuilder> Queries;

        public DiscreteMessage(List<string> queries, Func<DiscreteMessageEnteredArgs, Task> onEntered) : this([..queries.Select(q => new TelegramMessageBuilder(q))], onEntered) {}

        public DiscreteMessage(List<TelegramMessageBuilder> queries, Func<DiscreteMessageEnteredArgs, Task> onEntered)
        {
            ActiveChannels = [];
            Channels = [];
            OnEntered = onEntered;
            Queries = queries;
            BotHooks.OnBotUpdate += HandleBotUpdate;
        }

        private async Task HandleBotUpdate(BotUpdateEventArgs args)
        {
            if(args.Update.Type == UpdateType.Message && args.Update.Message != null && args.Update.Message.From != null && Channels.TryGetValue(args.Update.Message.Chat.Id, out var info))
            {
                args.Handled = true;
                if (info.Add(args.Update.Message))
                {
                    await OnEntered(new(info.Responses, args.Bot, args.Update.Message.Chat.Id, args.Update.Message.From, info.Data));
                    Channels.Remove(args.Update.Message.Chat.Id);
                    ActiveChannels.Remove(args.Update.Message.Chat.Id);
                }
                else
                    await args.Bot.SendMessage(Queries[info.Progress], args.Update.Message.Chat.Id);
            }
        }

        public async Task Send(long channelId, params List<object> data)
            => await Send(Program.Bot.Telegram, channelId, data);

        public async Task Send(TelegramBotClient bot, long channelId, params List<object> data)
        {
            if (CanSendMessage(channelId))
            {
                await bot.SendMessage(Queries[0], channelId);
                Channels.Add(channelId, new ChannelInfo(Queries.Count, data));
                ActiveChannels.Add(channelId, this);
            }
        }

        public void Dispose()
        {
            BotHooks.OnBotUpdate -= HandleBotUpdate;
            GC.SuppressFinalize(this);
        }

        private  class ChannelInfo
        {
            public Message[] Responses;

            public int Progress;

            public int QueriesCount;

            public List<object> Data;

            public ChannelInfo(int queriesCount, List<object> data)
            {
                Responses = new Message[queriesCount];
                Progress = 0;
                QueriesCount = queriesCount;
                Data = data;
            }

            public bool Add(Message message)
            {
                if (Progress < QueriesCount)
                    Responses[Progress++] = message;
                if(Progress == QueriesCount)
                    return true;
                return false;
            }
        }
    }
}
