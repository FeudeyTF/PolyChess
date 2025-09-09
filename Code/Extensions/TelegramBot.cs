using PolyChessTGBot.Bot.Messages;
using System.Drawing;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PolyChessTGBot.Extensions
{
    public static partial class Extensions
    {
        public const int MAX_CAPTION_SIZE = 1024;

        public const int MAX_TEXT_SIZE = 4096;

        public static async Task SendMessage(this TelegramBotClient bot, TelegramMessageBuilder message, ChatId chatID)
        {
            if (message.CancellationToken == default)
                Console.WriteLine(message.Text + " NULL TOKEN!");
            if (message.File != null)
            {
                if (message.Text == null || message.Text.Length <= MAX_CAPTION_SIZE)
                    await bot.SendDocumentAsync(chatID, message.File, message.ThreadID, message.Thumbnail, message.Text, message.ParseMode, message.Entities, message.DisableContentTypeDetection, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
                else
                {
#if DEBUG
                    var textLength = message.Text.Length;
                    int messageCount = textLength / MAX_TEXT_SIZE + 1;
                    for (int i = 0; i < messageCount - 1; i++)
                        await bot.SendTextMessageAsync(chatID, message.Text[(i * MAX_TEXT_SIZE)..(i + 2 * MAX_TEXT_SIZE)], message.ThreadID, message.ParseMode, message.Entities, message.DisableWebPagePreview, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
                    await bot.SendDocumentAsync(chatID, message.File, message.ThreadID, message.Thumbnail, message.Text[((messageCount - 1) * MAX_TEXT_SIZE)..], message.ParseMode, message.Entities, message.DisableContentTypeDetection, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
#endif
                }
            }
            else if (message.MediaFiles != null)
            {
                await bot.SendMediaGroupAsync(chatID, message.MediaFiles, message.ThreadID, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.CancellationToken);
            }
            else if (message.Media != null)
            {
                if (message.Media is InputMediaPhoto photo)
                    await bot.SendPhotoAsync(chatID, photo.Media, message.ThreadID, photo.Caption, photo.ParseMode, photo.CaptionEntities, photo.HasSpoiler, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
                else if (message.Media is InputMediaAudio audio)
                    await bot.SendAudioAsync(chatID, audio.Media, message.ThreadID, audio.Caption, audio.ParseMode, audio.CaptionEntities, audio.Duration, audio.Performer, audio.Title, audio.Thumbnail, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
                else if (message.Media is InputMediaVideo video)
                    await bot.SendVideoAsync(chatID, video.Media, message.ThreadID, video.Duration, video.Width, video.Height, video.Thumbnail, video.Caption, video.ParseMode, video.CaptionEntities, video.HasSpoiler, video.SupportsStreaming, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
                else if (message.Media is InputMediaAnimation animation)
                    await bot.SendAnimationAsync(chatID, animation.Media, message.ThreadID, animation.Duration, animation.Width, animation.Height, animation.Thumbnail, animation.Caption, animation.ParseMode, animation.CaptionEntities, animation.HasSpoiler, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
                else if (message.Media is InputMediaDocument document)
                    await bot.SendDocumentAsync(chatID, document.Media, message.ThreadID, message.Thumbnail, message.Text, message.ParseMode, message.Entities, message.DisableContentTypeDetection, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
            }
            else
            {
                if (message.Text == null || message.Text.Length <= MAX_TEXT_SIZE)
                    await bot.SendTextMessageAsync(chatID, message.Text ?? "", message.ThreadID, message.ParseMode, message.Entities, message.DisableWebPagePreview, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
                else
                {
#if DEBUG
                    var textLength = message.Text.Length;
                    int messageCount = textLength / MAX_TEXT_SIZE + 1;
                    for (int i = 0; i < messageCount - 1; i++)
                        await bot.SendTextMessageAsync(chatID, message.Text[(i * MAX_TEXT_SIZE)..((i + 1) * MAX_TEXT_SIZE)], message.ThreadID, message.ParseMode, message.Entities, message.DisableWebPagePreview, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
                    await bot.SendTextMessageAsync(chatID, message.Text[((messageCount - 1) * MAX_TEXT_SIZE)..], message.ThreadID, message.ParseMode, message.Entities, message.DisableWebPagePreview, message.DisableNotification, message.ProtectContent, message.ReplyToMessageID, message.AllowSendingWithoutReply, message.ReplyMarkup, message.CancellationToken);
#endif                
                }
            }

            if (message.File != null && message.File is InputFileStream streamFile)
            {
                streamFile.Content.Close();
                await streamFile.Content.DisposeAsync();
            }
        }

        public static async Task EditMessage(this TelegramBotClient bot, TelegramMessageBuilder message, ChatId chatID, Message oldMessage)
        {
            if (message.CancellationToken == default)
                Console.WriteLine(message.Text + " NULL TOKEN!");
            if (message.File != null)
                message.WithDocument(message.File);

            if (message.Media != null)
            {
                await bot.EditMessageMediaAsync(chatID, oldMessage.MessageId, message.Media, message.ReplyMarkup is InlineKeyboardMarkup keyboard ? keyboard : default, message.CancellationToken);
            }
            else if (message.Text != null)
            {
                InlineKeyboardMarkup? keyboard = message.ReplyMarkup is InlineKeyboardMarkup markup ? markup : default;
                if (message.Text != oldMessage.Text)
                {
                    await bot.EditMessageTextAsync(chatID, oldMessage.MessageId, message.Text, message.ParseMode, message.Entities, message.DisableWebPagePreview, keyboard, message.CancellationToken);
                }
                else if (oldMessage.Caption != message.Text)
                {
                    await bot.EditMessageCaptionAsync(chatID, oldMessage.MessageId, message.Text, message.ParseMode, message.Entities, keyboard, message.CancellationToken);
                }
            }
        }

        public static async Task<List<Image>> GetUserProfilePhotos(this TelegramBotClient bot, long userId, CancellationToken token)
        {
            List<Image> result = [];
            var profilePhotos = await Program.Bot.Telegram.GetUserProfilePhotosAsync(userId, cancellationToken: token);
            var profilePhoto = profilePhotos.Photos.FirstOrDefault();
            if (profilePhoto != null)
            {
                foreach (var photo in profilePhoto)
                {
                    var file = await Program.Bot.Telegram.GetFileAsync(photo.FileId, token);
                    if (file != null && file.FilePath != null)
                    {
                        using (var stream = new MemoryStream(photo.Width * photo.Height))
                        {
                            await Program.Bot.Telegram.DownloadFileAsync(file.FilePath, stream, token);
                            result.Add(Image.FromStream(stream));
                        }
                    }
                }
            }
            return result;
        }
    }
}
