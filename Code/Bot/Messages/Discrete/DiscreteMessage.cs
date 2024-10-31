using PolyChessTGBot.Extensions;
using PolyChessTGBot.Hooks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot.Messages.Discrete
{
    public class DiscreteMessage
    {
        private static List<long> ActiveChannels;

        static DiscreteMessage()
        {
            ActiveChannels = [];
        }

        public static bool CanSendMessage(long channelId) => !ActiveChannels.Contains(channelId);

        private readonly Dictionary<long, ChannelInfo> Channels;

        private readonly Func<DecretiveMessageEnteredArgs, Task> OnEntered;

        private readonly List<string> Questions;

        public DiscreteMessage(List<string> questions, Func<DecretiveMessageEnteredArgs, Task> onFullEntered)
        {
            ActiveChannels = [];
            Channels = [];
            OnEntered = onFullEntered;
            Questions = questions;
            BotHooks.OnBotUpdate += HandleBotUpdate;
        }

        private async Task HandleBotUpdate(BotUpdateEventArgs args)
        {
            if(args.Update.Type == UpdateType.Message && args.Update.Message != null && Channels.TryGetValue(args.Update.Message.Chat.Id, out var info))
            {
                args.Handled = true;
                if (info.Add(args.Update.Message))
                    await CloseMessageRecieving(args.Update.Message.Chat.Id, info, args.Bot, args.Update.Message.Chat.Id);
                else
                    await args.Bot.SendMessage(Questions[info.ChannelProgress], args.Update.Message.Chat.Id);
            }
        }

        public async Task Send(long channelId)
            => await Send(Program.Bot.Telegram, channelId);

        public async Task Send(TelegramBotClient bot, long channelId)
        {
            if (CanSendMessage(channelId))
            {
                await bot.SendMessage(Questions[0], channelId);
                Channels.Add(channelId, new ChannelInfo(Questions.Count));
                ActiveChannels.Add(channelId);
            }
        }

        private async Task CloseMessageRecieving(long channelId, ChannelInfo info, TelegramBotClient bot, ChatId chat)
        {
            await OnEntered(new(info.Messages, bot, chat));
            Channels.Remove(channelId);
            ActiveChannels.Remove(channelId);
        }

        private  class ChannelInfo
        {
            public Message[] Messages;

            public int ChannelProgress;

            public int QuestionCount;

            public ChannelInfo(int questionCount)
            {
                Messages = new Message[questionCount];
                ChannelProgress = 0;
                QuestionCount = questionCount;
            }

            public bool Add(Message message)
            {
                if (ChannelProgress < QuestionCount)
                    Messages[ChannelProgress++] = message;
                if(ChannelProgress == QuestionCount)
                    return true;
                return false;
            }
        }
    }
}
