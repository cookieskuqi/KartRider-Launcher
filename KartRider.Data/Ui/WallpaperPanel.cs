using System;
using System.Drawing;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 内容区背景板。没设置背景图时就是普通浅灰底；设置了就按"铺满裁切"绘制，卡片浮在上面。
    /// <see cref="SeeThrough"/> 为 true 时不画底（整窗模式下让窗体的背景图透上来）。
    /// </summary>
    public class WallpaperPanel : Panel
    {
        private Image image;
        private string path = string.Empty;
        private int opacity = 100;

        public WallpaperPanel()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Theme.Bg;
            DoubleBuffered = true;
        }

        /// <summary>没有背景图时的底色。</summary>
        public Color Fallback { get; set; } = Theme.Bg;

        /// <summary>true 时不铺底，交给父容器（窗体）绘制背景。</summary>
        public bool SeeThrough { get; set; }

        /// <summary>背景图不透明度 0~100，越小越接近默认底色。</summary>
        public int Opacity
        {
            get { return opacity; }
            set
            {
                int next = Math.Max(0, Math.Min(100, value));
                if (next != opacity)
                {
                    opacity = next;
                    Invalidate();
                }
            }
        }

        public bool HasImage
        {
            get { return image != null; }
        }

        public string ImagePath
        {
            get { return path; }
        }

        /// <summary>当前加载的图片（没有则为 null）。</summary>
        public Image CurrentImage
        {
            get { return image; }
        }

        /// <summary>设置（或清除）背景图。传空字符串即恢复默认底色。</summary>
        public void SetImage(string file)
        {
            file = file ?? string.Empty;
            if (string.Equals(path, file, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            path = file;
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
            image = Theme.LoadImage(path);
            Invalidate(true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (image == null)
            {
                if (SeeThrough)
                {
                    base.OnPaintBackground(e);
                    return;
                }
                using (var brush = new SolidBrush(Fallback))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                return;
            }

            using (var brush = new SolidBrush(Fallback))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            Theme.DrawCover(e.Graphics, ClientRectangle, image, opacity);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && image != null)
            {
                image.Dispose();
                image = null;
            }
            base.Dispose(disposing);
        }
    }
}
