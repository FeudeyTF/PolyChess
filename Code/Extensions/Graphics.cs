using System.Drawing.Drawing2D;
using System.Drawing;

namespace PolyChessTGBot.Extensions
{
    public static partial class Extensions
    {
        public static void DrawBox(this Graphics g, Font labelFont, string label, DateTime date, int x, int y, Font font, int width = 150)
        {
            var semiTransparentBlack = Color.FromArgb(125, 0, 0, 0);
            var semiTransparentWhite = Color.FromArgb(50, 255, 255, 255);
            var labelBackgroundColor = Color.FromArgb(37, 54, 58);
            var textColor = Color.White;

            var labelSize = g.MeasureString(label, labelFont);
            var labelPadding = 24;
            var labelWidth = (int)labelSize.Width + labelPadding;


            using var path = new GraphicsPath();
            var rect = new Rectangle(x, y + 10, width, 50);
            path.AddRoundedRect(rect, 5);


            using var labelPath = new GraphicsPath();
            var labelRect = new Rectangle(
                x + (width - labelWidth) / 2,
                y,
                labelWidth,
                20);
            labelPath.AddRoundedRect(labelRect, 5);


            using var bgBrush = new SolidBrush(semiTransparentBlack);
            using var labelBgBrush = new SolidBrush(semiTransparentWhite);
            g.FillPath(bgBrush, path);
            g.FillPath(labelBgBrush, labelPath);


            float labelX = labelRect.X + (labelRect.Width - labelSize.Width) / 2 + 1;
            float labelY = labelRect.Y + (labelRect.Height - labelSize.Height) / 2;
            g.DrawString(label, labelFont, new SolidBrush(labelBackgroundColor), labelX, labelY);


            var dateString = date.ToString("d MMMM yyyy");
            var dateSize = g.MeasureString(dateString, font);
            var dateX = rect.X + (rect.Width - dateSize.Width) / 2 + 1;
            var dateY = rect.Y + (rect.Height - dateSize.Height) / 2;
            g.DrawString(dateString, font, new SolidBrush(textColor), dateX, dateY);
        }

        public static void DrawTag(this Graphics g, string text, float x, float y, Font font)
        {
            var semiTransparentBlack = Color.FromArgb(125, 0, 0, 0);
            var textColor = Color.White;

            var size = g.MeasureString(text, font);
            int padding = 20;

            using var path = new GraphicsPath();
            var rect = new Rectangle((int)x, (int)y, (int)size.Width + padding, 25);
            path.AddRoundedRect(rect, 5);

            using var bgBrush = new SolidBrush(semiTransparentBlack);
            g.FillPath(bgBrush, path);

            float textX = x + padding / 2;
            float textY = y + (25 - size.Height) / 2;
            g.DrawString(text, font, new SolidBrush(textColor), textX, textY);
        }

        public static void DrawProgressBar(this Graphics graphics, Rectangle position, Color backgroundColor, Color foregroundColor, float progress, int radius = 10)
        {
            progress = Math.Min(1f, Math.Max(0f, progress));

            using GraphicsPath path = new();
            path.AddArc(position.X, position.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(position.Right - radius * 2, position.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(position.Right - radius * 2, position.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(position.X, position.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();

            graphics.FillPath(new SolidBrush(backgroundColor), path);

            if (progress > 0)
            {
                using GraphicsPath progressPath = new();
                var progressRect = new Rectangle(position.X, position.Y, (int)(position.Width * progress), position.Height);
                progressPath.AddArc(progressRect.X, progressRect.Y, radius * 2, radius * 2, 180, 90);
                progressPath.AddArc(Math.Min(progressRect.Right - radius * 2, position.Right - radius * 2), progressRect.Y, radius * 2, radius * 2, 270, 90);
                progressPath.AddArc(Math.Min(progressRect.Right - radius * 2, position.Right - radius * 2), progressRect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                progressPath.AddArc(progressRect.X, progressRect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                progressPath.CloseFigure();

                graphics.FillPath(new SolidBrush(foregroundColor), progressPath);
            }
        }

        public static Image Round(this Image image)
        {
            Image roundedImage = new Bitmap(image.Width, image.Height);
            GraphicsPath gp = new();
            gp.AddEllipse(new(0, 0, roundedImage.Width, roundedImage.Height));
            using (Graphics graphics = Graphics.FromImage(roundedImage))
            {
                graphics.SetClip(gp);
                graphics.DrawImage(image, Point.Empty);
            }
            return roundedImage;
        }


        public static void AddRoundedRect(this GraphicsPath path, Rectangle rect, int radius)
        {
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
        }
    }
}
