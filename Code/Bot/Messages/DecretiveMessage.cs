using PolyChessTGBot.Extensions;
using PolyChessTGBot.Hooks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PolyChessTGBot.Bot.Messages
{
    public class DecretiveMessage
    {
        private static List<long> ActiveChannels;

        static DecretiveMessage()
        {
            ActiveChannels = [];
        }

        public static bool CanSendMessage(long channelId) => !ActiveChannels.Contains(channelId);

        private readonly Dictionary<long, ChannelInfo> Channels;

        private readonly Action<DecretiveMessageFullyEnteredArgs> OnEntered;

        private readonly List<string> Questions;

        public DecretiveMessage(List<string> questions, Action<DecretiveMessageFullyEnteredArgs> onFullEntered)
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
                    CloseMessageRecieving(args.Update.Message.Chat.Id, info, args.Bot, args.Update.Message.Chat.Id);
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

        private void CloseMessageRecieving(long channelId, ChannelInfo info, TelegramBotClient bot, ChatId chat)
        {
            OnEntered(new(info.Messages, bot, chat));
            Channels.Remove(channelId);
            ActiveChannels.Remove(channelId);
        }

        private class ChannelInfo
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

    public class DecretiveMessageFullyEnteredArgs
    {
        public Message[] Answears;

        public TelegramBotClient Bot;

        public ChatId ChatID;

        public DecretiveMessageFullyEnteredArgs(Message[] answears, TelegramBotClient bot, ChatId chat)
        {
            Answears = answears;
            Bot = bot;
            ChatID = chat;
        }
    }
}
