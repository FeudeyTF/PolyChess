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

        private async Task HandleBotUpdate(TelegramBotClient bot, Update update)
        {
            if(update.Type == UpdateType.Message && update.Message != null && Channels.TryGetValue(update.Message.Chat.Id, out var info))
            {
                if (info.Add(update.Message))
                    CloseMessageRecieving(update.Message.Chat.Id, info, bot, update.Message.Chat.Id);
                else
                {
                    await bot.SendMessage(Questions[info.ChannelProgress], update.Message.Chat.Id);
                }
            }
        }

        public async Task Send(long channelId)
            => await Send(Program.Bot.Telegram, channelId);

        public async Task Send(TelegramBotClient bot, long channelId)
        {
            await bot.SendMessage(Questions[0], channelId);
            Channels.Add(channelId, new ChannelInfo(Questions.Count));
            ActiveChannels.Add(channelId);
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
