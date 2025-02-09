using PolyChessTGBot.Database;
using PolyChessTGBot.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PolyChessTGBot.Managers.Images
{
    internal class ImageManager
    {
        [SuppressMessage("Interoperability", "CA1416")]
        public static Image GenerateProfileImage(Image img, User user, LichessAPI.Types.User lichessUser, float progress, string team)
        {
            var width = 500;
            var height = 300;
            var margin = 20;
            var profilePhotoSize = 100;
            var spacing = 10;
            var littleSpacing = spacing / 2;
            FontFamily FontFamily = new("Arial");

            var backgroundColor1 = Color.FromArgb(37, 54, 58);
            var backgroundColor2 = Color.FromArgb(52, 119, 102);
            var semiTransparentBlack = Color.FromArgb(125, 0, 0, 0);
            var textColor = Color.White;
            var secondaryTextColor = Color.LightGray;
            var scoreColor = Color.Gold;

            Image result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (LinearGradientBrush gradientBrush = new(new Rectangle(0, 0, 500, 300), backgroundColor1, backgroundColor2, 225))
                {
                    g.FillRectangle(gradientBrush, 0, 0, 500, 300);
                }


                var leftColumn = margin;
                var rightColumn = profilePhotoSize + margin * 2;
                var contentWidth = width - rightColumn - margin;

                var frameRadius = 3;
                g.FillEllipse(Brushes.White, new Rectangle(leftColumn - frameRadius, margin - frameRadius, profilePhotoSize + 2 * frameRadius, profilePhotoSize + 2 * frameRadius));
                g.DrawImage(img.Round(), leftColumn, margin, profilePhotoSize, profilePhotoSize);

                using var nameFont = new Font(FontFamily, 18, FontStyle.Bold);
                using var scoreFont = new Font(FontFamily, 16, FontStyle.Bold);
                using var normalFont = new Font(FontFamily, 12);
                using var yearFont = new Font(FontFamily, 24, FontStyle.Bold);
                using var yearLabelFont = new Font("Arial", 12);

                var yearText = user.Year.ToString();
                var yearSize = g.MeasureString(yearText, yearFont);
                var yearX = leftColumn + (profilePhotoSize - yearSize.Width) / 2;
                var yearY = margin + profilePhotoSize + spacing * 2;
                g.DrawString(yearText, yearFont, new SolidBrush(textColor), yearX, yearY);

                var labelText = "Курс";
                var labelSize = g.MeasureString(labelText, yearLabelFont);
                var labelX = leftColumn + (profilePhotoSize - labelSize.Width) / 2;
                g.DrawString(labelText, yearLabelFont, new SolidBrush(textColor), labelX, yearY + yearSize.Height + littleSpacing);

                g.DrawString(user.Name, nameFont, new SolidBrush(textColor), rightColumn, margin);

                var scoreText = lichessUser.Perfomance.Max(value => value.Value.Rating).ToString();
                var scoreSize = g.MeasureString(scoreText, scoreFont);
                g.DrawString(scoreText, scoreFont, new SolidBrush(scoreColor),
                    width - margin - scoreSize.Width, margin);

                g.DrawString(user.LichessName, normalFont, new SolidBrush(secondaryTextColor),
                    rightColumn, margin + nameFont.Height + spacing);

                var progressY = margin + nameFont.Height + normalFont.Height + spacing * 3;
                g.DrawString("Прогресс по зачёту", normalFont, new SolidBrush(secondaryTextColor),
                    rightColumn, progressY);

                var progressBarY = progressY + normalFont.Height + spacing;
                Rectangle progressRect = new(rightColumn, progressBarY, contentWidth, 20);
                g.DrawProgressBar(progressRect, semiTransparentBlack, textColor, progress);

                var datesY = progressBarY + progressRect.Height + spacing * 3;
                var dateBoxWidth = (contentWidth - spacing) / 2;
                g.DrawBox(new Font(FontFamily, 10, FontStyle.Bold), "Присоединился", lichessUser.RegisterDate, rightColumn, datesY, normalFont, dateBoxWidth);
                g.DrawBox(new Font(FontFamily, 10, FontStyle.Bold), "Последний вход", lichessUser.LastSeenDate, rightColumn + dateBoxWidth + spacing, datesY, normalFont, dateBoxWidth);

                var teamsY = datesY + 80;

                g.DrawString("Команды:", normalFont, new SolidBrush(secondaryTextColor), leftColumn, teamsY);
                g.DrawTag(team, leftColumn, teamsY - 3, normalFont);
            }
            return result;
        }
    }
}
