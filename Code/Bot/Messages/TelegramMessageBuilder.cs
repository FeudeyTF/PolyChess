using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;

namespace PolyChessTGBot.Bot.Messages
{
    public class TelegramMessageBuilder
    {
        public string Text { get; set; }

        public int? ThreadID { get; set; }

        public ParseMode ParseMode { get; set; }

        public List<MessageEntity>? Entities { get; set; }

        public bool DisableWebPagePreview { get; set; }

        public bool DisableNotification { get; set; }

        public bool ProtectContent { get; set; }

        public bool AllowSendingWithoutReply { get; set; }

        public bool DisableContentTypeDetection { get; set; }

        public IReplyMarkup? ReplyMarkup { get; set; }

        public InputFile? File { get; set; }

        public InputFile? Thumbnail { get; set; }

        public InputMedia? Media { get; set; }

        public List<IAlbumInputMedia>? MediaFiles { get; set; }

        public int? ReplyToMessageID { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public TelegramMessageBuilder(
            string text,
            int? threadId = default,
            ParseMode parseMode = ParseMode.Html,
            List<MessageEntity>? entities = default,
            bool disableWebPagePreview = default,
            bool disableNotification = default,
            bool protectContent = default,
            bool allowSendingWithoutReply = default,
            IReplyMarkup? replyMarkup = default,
            InputFile? file = default,
            InputFile? thumbnail = default,
            InputMedia? media = default,
            List<IAlbumInputMedia>? mediaFiles = default,
            int? replyToMessageID = default,
            CancellationToken cancellationToken = default)
        {
            Text = text;
            ThreadID = threadId;
            ParseMode = parseMode;
            Entities = entities;
            DisableWebPagePreview = disableWebPagePreview;
            DisableNotification = disableNotification;
            ProtectContent = protectContent;
            AllowSendingWithoutReply = allowSendingWithoutReply;
            ReplyMarkup = replyMarkup;
            CancellationToken = cancellationToken;
            File = file;
            Media = media;
            MediaFiles = mediaFiles;
            Thumbnail = thumbnail;
            ReplyToMessageID = replyToMessageID;
        }

        public TelegramMessageBuilder WithFile(Uri uri, InputFile? thumbnail = default)
            => WithFile(new InputFileUrl(uri), thumbnail);

        public TelegramMessageBuilder WithFile(Stream stream, string fileName, InputFile? thumbnail = default)
            => WithFile(new InputFileStream(stream, fileName), thumbnail);

        public TelegramMessageBuilder WithFile(string fileID, InputFile? thumbnail = default)
            => WithFile(new InputFileId(fileID), thumbnail);

        public TelegramMessageBuilder WithFile(InputFile file, InputFile? thumbnail = default)
        {
            File = file;
            Thumbnail = thumbnail;
            return this;
        }

        public TelegramMessageBuilder WithVideo(
            string fileID,
            string? caption = default,
            int? width = default,
            int? height = default,
            int? duration = default,
            bool? hasSpoiler = default,
            bool? supportsStreaming = default,
            MessageEntity[]? captionEntities = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithVideo(
                new InputFileId(fileID),
                caption,
                width, 
                height, 
                duration, 
                hasSpoiler, 
                supportsStreaming,
                captionEntities,
                parseMode, thumbnail);

        public TelegramMessageBuilder WithVideo(
            InputFile file,
            string? caption = default,
            int? width = default,
            int? height = default,
            int? duration = default,
            bool? hasSpoiler = default,
            bool? supportsStreaming = default,
            MessageEntity[]? captionEntities = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithMedia(new InputMediaVideo(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? [.. Entities],
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
            int? width = default,
            int? height = default,
            int? duration = default,
            bool? hasSpoiler = default,
            MessageEntity[]? captionEntities = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithMedia(new InputMediaAnimation(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? [.. Entities],
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
            int? duration = default,
            MessageEntity[]? captionEntities = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithMedia(new InputMediaAudio(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? [.. Entities],
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
            bool? hasSpoiler = default,
            ParseMode? parseMode = default)
            => WithMedia(new InputMediaPhoto(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? [.. Entities],
                ParseMode = parseMode ?? ParseMode,
                HasSpoiler = hasSpoiler
            });

        public TelegramMessageBuilder WithDocument(
            InputFile file,
            string? caption = default,
            MessageEntity[]? captionEntities = default,
            bool? disableContentTypeDetection = default,
            ParseMode? parseMode = default,
            InputFile? thumbnail = default)
            => WithMedia(new InputMediaDocument(file)
            {
                Caption = caption ?? Text,
                CaptionEntities = captionEntities ?? [.. Entities],
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

        public TelegramMessageBuilder ReplyTo(int messageID)
        {
            ReplyToMessageID = messageID;
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
            Entities.Add(entity);
            return this;
        }

        public TelegramMessageBuilder WithThreadID(int threadID)
        {
            ThreadID = threadID;
            return this;
        }

        public TelegramMessageBuilder WithToken(CancellationToken token)
        {
            CancellationToken = token;
            return this;
        }

        public TelegramMessageBuilder WithMarkup(IReplyMarkup markup)
        {
            ReplyMarkup = markup;
            return this;
        }

        public static implicit operator TelegramMessageBuilder(string text)
            => new(text);
    }
}
