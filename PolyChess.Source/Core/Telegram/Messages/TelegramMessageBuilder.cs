using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChess.Core.Telegram.Messages
{
    internal class TelegramMessageBuilder : ITelegramMessage
    {
        public const int MaxCaptionLength = 1024;

        public const int MaxTextLength = 4096;

        public string? Text { get; set; }

        public int ThreadId { get; set; }

        public ParseMode ParseMode { get; set; }

        public ReplyParameters? ReplyParameters { get; set; }

        public MessageEntity[]? Entities { get; set; }

        public LinkPreviewOptions? LinkPreviewOptions { get; set; }

        public bool DisableWebPagePreview { get; set; }

        public bool DisableNotification { get; set; }

        public bool ProtectContent { get; set; }

        public bool AllowSendingWithoutReply { get; set; }

        public bool DisableContentTypeDetection { get; set; }

        public ReplyMarkup? ReplyMarkup { get; set; }

        public InputFile? File { get; set; }

        public InputFile? Thumbnail { get; set; }

        public InputMedia? Media { get; set; }

        public List<IAlbumInputMedia>? MediaFiles { get; set; }

        public int ReplyToMessageId { get; set; }

        public string? MessageEffectId { get; set; }

        public string? BusinessConnectionId { get; set; }

        public bool AllowPaidBroadcast { get; set; }

        public long? DirectMessageTopicId { get; set; }

        public SuggestedPostParameters? SuggestedPostParameters { get; set; }

        public TelegramMessageBuilder(
            string? text = default,
            int threadId = default,
            ParseMode parseMode = ParseMode.Html,
            ReplyParameters? replyParameters = default,
            MessageEntity[]? entities = default,
            LinkPreviewOptions? linkPreviewOptions = default,
            bool disableWebPagePreview = default,
            bool disableNotification = default,
            bool protectContent = default,
            bool allowSendingWithoutReply = default,
            bool disableContentTypeDetection = default,
            ReplyMarkup? replyMarkup = default,
            InputFile? file = default,
            InputFile? thumbnail = default,
            InputMedia? media = default,
            List<IAlbumInputMedia>? mediaFiles = default,
            int replyToMessageId = default,
            string? messageEffectId = default,
            string? businessConnectionId = default,
            bool allowPaidBroadcast = default,
            long? directMessageTopicId = default,
            SuggestedPostParameters? suggestedPostParameters = default)
        {
            Text = text;
            ThreadId = threadId;
            ParseMode = parseMode;
            ReplyParameters = replyParameters;
            Entities = entities;
            LinkPreviewOptions = linkPreviewOptions;
            DisableWebPagePreview = disableWebPagePreview;
            DisableNotification = disableNotification;
            ProtectContent = protectContent;
            AllowSendingWithoutReply = allowSendingWithoutReply;
            DisableContentTypeDetection = disableContentTypeDetection;
            ReplyMarkup = replyMarkup;
            File = file;
            Thumbnail = thumbnail;
            Media = media;
            MediaFiles = mediaFiles;
            ReplyToMessageId = replyToMessageId;
            MessageEffectId = messageEffectId;
            BusinessConnectionId = businessConnectionId;
            AllowPaidBroadcast = allowPaidBroadcast;
            DirectMessageTopicId = directMessageTopicId;
            SuggestedPostParameters = suggestedPostParameters;
        }

        public TelegramMessageBuilder WithFile(Uri uri, InputFile? thumbnail = default)
          => WithFile(new InputFileUrl(uri), thumbnail);

        public TelegramMessageBuilder WithFile(Stream stream, string fileName, InputFile? thumbnail = default)
            => WithFile(new InputFileStream(stream, fileName), thumbnail);

        public TelegramMessageBuilder WithFile(string fileId, InputFile? thumbnail = default)
            => WithFile(new InputFileId(fileId), thumbnail);

        public TelegramMessageBuilder WithFile(InputFile file, InputFile? thumbnail = default)
        {
            File = file;
            Thumbnail = thumbnail;
            return this;
        }

        public TelegramMessageBuilder WithVideo(
            string fileId,
            string? caption = default,
            int width = default,
            int height = default,
            int duration = default,
            bool hasSpoiler = default,
            bool supportsStreaming = default,
            MessageEntity[]? captionEntities = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithVideo(
                new InputFileId(fileId),
                caption,
                width,
                height,
                duration,
                hasSpoiler,
                supportsStreaming,
                captionEntities,
                parseMode,
                thumbnail
            );

        public TelegramMessageBuilder WithVideo(
            InputFile file,
            string? caption = default,
            int width = default,
            int height = default,
            int duration = default,
            bool hasSpoiler = default,
            bool supportsStreaming = default,
            MessageEntity[]? captionEntities = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithMedia(new InputMediaVideo(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? Entities,
                ParseMode = parseMode ?? ParseMode,
                Duration = duration,
                HasSpoiler = hasSpoiler,
                Width = width,
                Height = height,
                Thumbnail = thumbnail ?? Thumbnail,
                SupportsStreaming = supportsStreaming
            });

        public TelegramMessageBuilder WithAnimation(
            InputFile file,
            string? caption = default,
            int width = default,
            int height = default,
            int duration = default,
            bool hasSpoiler = default,
            MessageEntity[]? captionEntities = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithMedia(new InputMediaAnimation(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? Entities,
                ParseMode = parseMode ?? ParseMode,
                Duration = duration,
                HasSpoiler = hasSpoiler,
                Width = width,
                Height = height,
                Thumbnail = thumbnail ?? Thumbnail
            });

        public TelegramMessageBuilder WithAudio(
            InputFile file,
            string? caption = default,
            string? title = default,
            string? performer = default,
            int duration = default,
            MessageEntity[]? captionEntities = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithMedia(new InputMediaAudio(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? Entities,
                ParseMode = parseMode ?? ParseMode,
                Duration = duration,
                Title = title,
                Performer = performer,
                Thumbnail = thumbnail ?? Thumbnail
            });

        public TelegramMessageBuilder WithPhoto(
            InputFile file,
            string? caption = default,
            MessageEntity[]? captionEntities = default,
            bool hasSpoiler = default,
            ParseMode? parseMode = default)
            => WithMedia(new InputMediaPhoto(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? Entities,
                ParseMode = parseMode ?? ParseMode,
                HasSpoiler = hasSpoiler
            });

        public TelegramMessageBuilder WithDocument(
            InputFile file,
            string? caption = default,
            MessageEntity[]? captionEntities = default,
            bool disableContentTypeDetection = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithMedia(new InputMediaDocument(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? Entities,
                ParseMode = parseMode ?? ParseMode,
                DisableContentTypeDetection = disableContentTypeDetection,
                Thumbnail = thumbnail,
            });

        public TelegramMessageBuilder WithMedia(InputMedia media)
        {
            Media = media;
            return this;
        }

        public TelegramMessageBuilder AddMedia<TMedia>(TMedia media) where TMedia : IAlbumInputMedia
        {
            MediaFiles ??= [];
            MediaFiles.Add(media);
            return this;
        }

        public TelegramMessageBuilder ReplyTo(int messageId)
        {
            ReplyToMessageId = messageId;
            return this;
        }

        public TelegramMessageBuilder WithParseMode(ParseMode mode)
        {
            ParseMode = mode;
            return this;
        }

        public TelegramMessageBuilder WithText(string text)
        {
            Text = text;
            return this;
        }

        public TelegramMessageBuilder WithoutNotification()
        {
            DisableNotification = true;
            return this;
        }

        public TelegramMessageBuilder WithProtectContent()
        {
            ProtectContent = true;
            return this;
        }

        public TelegramMessageBuilder WithoutWebPagePreview()
        {
            DisableWebPagePreview = true;
            return this;
        }

        public TelegramMessageBuilder WithSendingWithoutReply()
        {
            AllowSendingWithoutReply = true;
            return this;
        }

        public TelegramMessageBuilder WithoutContentTypeDetection()
        {
            DisableContentTypeDetection = true;
            return this;
        }

        public TelegramMessageBuilder AddEntity(MessageEntity entity)
        {
            Entities ??= [];
            Entities = [.. Entities, entity];
            return this;
        }

        public TelegramMessageBuilder WithThreadId(int threadId)
        {
            ThreadId = threadId;
            return this;
        }

        public TelegramMessageBuilder WithKeyboard(InlineKeyboardMarkup keyboard)
            => WithMarkup(keyboard);

        public TelegramMessageBuilder WithMarkup(ReplyMarkup markup)
        {
            ReplyMarkup = markup;
            return this;
        }

        public TelegramMessageBuilder AddKeyboard(List<InlineKeyboardButton> keyboard)
        {
            if (ReplyMarkup == null)
                ReplyMarkup = new InlineKeyboardMarkup(keyboard);
            else if (ReplyMarkup is InlineKeyboardMarkup keyboardMarkup)
                ReplyMarkup = new InlineKeyboardMarkup(keyboardMarkup.InlineKeyboard.Append(keyboard));
            return this;
        }

        public TelegramMessageBuilder AddButton(InlineKeyboardButton button)
        {
            if (ReplyMarkup == null)
                ReplyMarkup = new InlineKeyboardMarkup(button);
            else if (ReplyMarkup is InlineKeyboardMarkup keyboardMarkup)
                ReplyMarkup = new InlineKeyboardMarkup(keyboardMarkup.InlineKeyboard.Append([button]));
            return this;
        }

        public async Task SendAsync(ITelegramBotClient client, ChatId chatId, CancellationToken token)
        {
            if (token == default)
                Console.WriteLine($"Message with text '{Text}' sended with DEFAULT cancellation token");
            if (File != null)
            {
                if (Text == null || Text.Length <= MaxCaptionLength)
                    await client.SendDocument(chatId, File, Text, ParseMode, ReplyParameters, ReplyMarkup, Thumbnail, ThreadId, Entities, DisableContentTypeDetection, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                else
                {
                    var textLength = Text.Length;
                    int messageCount = textLength / MaxTextLength + 1;
                    for (int i = 0; i < messageCount - 1; i++)
                        await client.SendDocument(chatId, File, Text[(i * MaxTextLength)..(i + 2 * MaxTextLength)], ParseMode, ReplyParameters, ReplyMarkup, Thumbnail, ThreadId, Entities, DisableContentTypeDetection, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                    await client.SendDocument(chatId, File, Text[((messageCount - 1) * MaxTextLength)..], ParseMode, ReplyParameters, ReplyMarkup, Thumbnail, ThreadId, Entities, DisableContentTypeDetection, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                }
            }
            else if (MediaFiles != null)
            {
                await client.SendMediaGroup(chatId, MediaFiles, ReplyParameters, ThreadId, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, token);
            }
            else if (Media != null)
            {
                if (Media is InputMediaPhoto photo)
                    await client.SendPhoto(chatId, photo.Media, photo.Caption, photo.ParseMode, ReplyParameters, ReplyMarkup, ThreadId, photo.CaptionEntities, photo.ShowCaptionAboveMedia, photo.HasSpoiler, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                else if (Media is InputMediaAudio audio)
                    await client.SendAudio(chatId, audio.Media, audio.Caption, audio.ParseMode, ReplyParameters, ReplyMarkup, audio.Duration, audio.Performer, audio.Title, audio.Thumbnail, ThreadId, audio.CaptionEntities, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                else if (Media is InputMediaVideo video)
                    await client.SendVideo(chatId, video.Media, video.Caption, video.ParseMode, ReplyParameters, ReplyMarkup, video.Duration, video.Width, video.Height, video.Thumbnail, ThreadId, video.CaptionEntities, video.ShowCaptionAboveMedia, video.HasSpoiler, video.SupportsStreaming, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, video.Cover, video.StartTimestamp, DirectMessageTopicId, SuggestedPostParameters, token);
                else if (Media is InputMediaAnimation animation)
                    await client.SendAnimation(chatId, animation.Media, animation.Caption, animation.ParseMode, ReplyParameters, ReplyMarkup, animation.Duration, animation.Width, animation.Height, animation.Thumbnail, ThreadId, animation.CaptionEntities, animation.ShowCaptionAboveMedia, animation.HasSpoiler, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                else if (Media is InputMediaDocument document)
                    await client.SendDocument(chatId, document.Media, document.Caption, document.ParseMode, ReplyParameters, ReplyMarkup, document.Thumbnail, ThreadId, document.CaptionEntities, DisableContentTypeDetection, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
            }
            else
            {
                if (Text == null || Text.Length <= MaxTextLength)
                    await client.SendMessage(chatId, Text ?? "", ParseMode, ReplyParameters, ReplyMarkup, LinkPreviewOptions, ThreadId, Entities, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                else
                {
                    var textLength = Text.Length;
                    int messageCount = textLength / MaxTextLength + 1;
                    for (int i = 0; i < messageCount - 1; i++)
                        await client.SendMessage(chatId, Text[(i * MaxTextLength)..((i + 1) * MaxTextLength)], ParseMode, ReplyParameters, ReplyMarkup, LinkPreviewOptions, ThreadId, Entities, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                    await client.SendMessage(chatId, Text[((messageCount - 1) * MaxTextLength)..], ParseMode, ReplyParameters, ReplyMarkup, LinkPreviewOptions, ThreadId, Entities, DisableNotification, ProtectContent, MessageEffectId, BusinessConnectionId, AllowPaidBroadcast, DirectMessageTopicId, SuggestedPostParameters, token);
                }
            }

            if (File != null && File is InputFileStream streamFile)
            {
                streamFile.Content.Close();
                await streamFile.Content.DisposeAsync();
            }
        }

        public async Task EditAsync(ITelegramBotClient client, Message oldMessage, CancellationToken token)
        {
            if (token == default)
                Console.WriteLine($"Message with text '{Text}' sended with DEFAULT cancellation token");

            if (File != null)
                WithDocument(File);

            if (Media != null)
            {
                await client.EditMessageMedia(oldMessage.Chat.Id, oldMessage.MessageId, Media, ReplyMarkup is InlineKeyboardMarkup keyboard ? keyboard : default, BusinessConnectionId, token);
            }
            else if (Text != null)
            {
                InlineKeyboardMarkup? keyboard = ReplyMarkup is InlineKeyboardMarkup markup ? markup : default;
                if (Text != oldMessage.Text)
                {
                    await client.EditMessageText(oldMessage.Chat.Id, oldMessage.MessageId, Text, ParseMode, keyboard, LinkPreviewOptions, Entities, BusinessConnectionId, token);
                }
                else if (oldMessage.Caption != Text)
                {
                    await client.EditMessageCaption(oldMessage.Chat.Id, oldMessage.MessageId, Text, ParseMode, keyboard, Entities, default, BusinessConnectionId, token);
                }
            }
        }

        public static implicit operator TelegramMessageBuilder(string text)
            => new(text);

        public static implicit operator TelegramMessageBuilder(Message message)
        {
            TelegramMessageBuilder result = new(message.Text)
            {
                Entities = message.Entities,
                ThreadId = message.MessageThreadId ?? 0
            };
            if (message.Video != null)
            {
                var video = message.Video;
                InputFile? thumbnail = video.Thumbnail == null ? null : new InputFileId(video.Thumbnail.FileId);
                result.WithVideo(video.FileId, default, video.Width, video.Height, video.Duration, thumbnail: thumbnail);
            }
            return result;
        }
    }
}
