using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 统一配色与字体。支持浅色 / 深色两套色板（<see cref="Apply"/> 切换），
    /// 自绘控件都在绘制时读取这些颜色，所以切完刷一下就能整体换肤。
    /// </summary>
    public static class Theme
    {
        public static bool IsDark { get; private set; }

        public static Color Bg { get; private set; } = Color.FromArgb(0xF3, 0xF5, 0xF9);
        public static Color Surface { get; private set; } = Color.White;
        public static Color Border { get; private set; } = Color.FromArgb(0xE4, 0xE7, 0xED);

        public static Color TextPrimary { get; private set; } = Color.FromArgb(0x2B, 0x2F, 0x36);
        public static Color TextSecondary { get; private set; } = Color.FromArgb(0x8A, 0x91, 0x9C);

        public static Color Accent { get; private set; } = Color.FromArgb(0x4C, 0x9E, 0xEA);
        public static Color AccentHover { get; private set; } = Color.FromArgb(0x3D, 0x8E, 0xDB);
        public static Color AccentDown { get; private set; } = Color.FromArgb(0x2F, 0x7F, 0xC8);
        public static Color AccentSoft { get; private set; } = Color.FromArgb(0xE9, 0xF2, 0xFD);

        public static Color Danger { get; private set; } = Color.FromArgb(0xE5, 0x48, 0x4D);
        public static Color HoverBg { get; private set; } = Color.FromArgb(0xF0, 0xF3, 0xF8);

        /// <summary>输入框/下拉框的底色：比卡片略深一点，白卡片上才看得出边界。</summary>
        public static Color Field { get; private set; } = Color.FromArgb(0xF5, 0xF7, 0xFB);

        /// <summary>玻璃面板的蒙版色：深色模式用黑，浅色用白。</summary>
        public static Color GlassTint
        {
            get { return IsDark ? Color.Black : Color.White; }
        }

        public const string FontName = "Microsoft YaHei UI";

        /// <summary>切换色板。调用后需要让各窗口重新应用颜色并重绘。</summary>
        public static void Apply(bool dark)
        {
            IsDark = dark;
            if (dark)
            {
                Bg = Color.FromArgb(0x1E, 0x21, 0x26);
                Surface = Color.FromArgb(0x27, 0x2B, 0x31);
                Border = Color.FromArgb(0x3A, 0x40, 0x48);
                TextPrimary = Color.FromArgb(0xE6, 0xE9, 0xEF);
                TextSecondary = Color.FromArgb(0x95, 0x9D, 0xA9);
                Accent = Color.FromArgb(0x5A, 0xA9, 0xF0);
                AccentHover = Color.FromArgb(0x74, 0xBA, 0xF7);
                AccentDown = Color.FromArgb(0x45, 0x92, 0xD6);
                AccentSoft = Color.FromArgb(0x26, 0x3B, 0x52);
                Danger = Color.FromArgb(0xE5, 0x48, 0x4D);
                HoverBg = Color.FromArgb(0x33, 0x38, 0x40);
                Field = Color.FromArgb(0x1B, 0x1E, 0x23);
            }
            else
            {
                Bg = Color.FromArgb(0xF3, 0xF5, 0xF9);
                Surface = Color.White;
                Border = Color.FromArgb(0xE4, 0xE7, 0xED);
                TextPrimary = Color.FromArgb(0x2B, 0x2F, 0x36);
                TextSecondary = Color.FromArgb(0x8A, 0x91, 0x9C);
                Accent = Color.FromArgb(0x4C, 0x9E, 0xEA);
                AccentHover = Color.FromArgb(0x3D, 0x8E, 0xDB);
                AccentDown = Color.FromArgb(0x2F, 0x7F, 0xC8);
                AccentSoft = Color.FromArgb(0xE9, 0xF2, 0xFD);
                Danger = Color.FromArgb(0xE5, 0x48, 0x4D);
                HoverBg = Color.FromArgb(0xF0, 0xF3, 0xF8);
                Field = Color.FromArgb(0xF5, 0xF7, 0xFB);
            }
        }

        public static Font F(float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(FontName, size, style, GraphicsUnit.Point);
        }

        /// <summary>
        /// 读图片：先整块读进内存再解码，Image.FromStream 才不会占着文件句柄。
        /// 读不到就返回 null（调用方回落到默认底色）。
        /// </summary>
        public static Image LoadImage(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return null;
                }
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var buffer = new MemoryStream())
                {
                    fs.CopyTo(buffer);
                    buffer.Position = 0;
                    return Image.FromStream(buffer);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 按"铺满裁切"（cover）绘制背景图，并按 opacity（0~100）向页面底色淡出。
        /// </summary>
        public static void DrawCover(Graphics g, Rectangle bounds, Image image, int opacity)
        {
            if (image == null || bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            Rectangle dest = CoverRect(bounds, image.Width, image.Height);

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(image, dest);

            int alpha = (int)((100 - Math.Max(0, Math.Min(100, opacity))) * 255 / 100);
            if (alpha > 0)
            {
                using (var fade = new SolidBrush(Color.FromArgb(alpha, Bg)))
                {
                    g.FillRectangle(fade, bounds);
                }
            }
        }

        private static Rectangle CoverRect(Rectangle bounds, int imageWidth, int imageHeight)
        {
            float scale = Math.Max((float)bounds.Width / imageWidth, (float)bounds.Height / imageHeight);
            int w = (int)Math.Ceiling(imageWidth * scale);
            int h = (int)Math.Ceiling(imageHeight * scale);
            return new Rectangle(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
        }

        /// <summary>
        /// 生成整窗尺寸的"毛玻璃"底图：先按 cover 缩放，再缩到 1/12 后用双线性放大回来，
        /// 得到近似高斯的模糊效果（纯 GDI+，不依赖系统动画特效）。
        /// </summary>
        public static Bitmap BuildFrosted(Image image, Size size)
        {
            if (image == null || size.Width <= 0 || size.Height <= 0)
            {
                return null;
            }

            try
            {
                int smallW = Math.Max(1, size.Width / 12);
                int smallH = Math.Max(1, size.Height / 12);

                using (var big = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics gb = Graphics.FromImage(big))
                    {
                        gb.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        gb.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        gb.DrawImage(image, CoverRect(new Rectangle(0, 0, size.Width, size.Height), image.Width, image.Height));
                    }

                    using (var small = new Bitmap(smallW, smallH, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics gs = Graphics.FromImage(small))
                        {
                            gs.InterpolationMode = InterpolationMode.Bilinear;
                            gs.DrawImage(big, new Rectangle(0, 0, smallW, smallH));
                        }

                        var result = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
                        using (Graphics gr = Graphics.FromImage(result))
                        {
                            gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            gr.DrawImage(small, new Rectangle(0, 0, size.Width, size.Height));
                        }
                        return result;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>圆角矩形路径。半径过大时自动收缩，避免短控件画歪。</summary>
        public static GraphicsPath Round(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0 || rect.Width <= 0 || rect.Height <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void Fill(Graphics g, Rectangle rect, int radius, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = Round(rect, radius))
            using (var b = new SolidBrush(color))
            {
                g.FillPath(b, p);
            }
        }

        public static void Stroke(Graphics g, Rectangle rect, int radius, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            using (GraphicsPath p = Round(r, radius))
            using (var pen = new Pen(color, 1f))
            {
                g.DrawPath(pen, p);
            }
        }
    }
}
