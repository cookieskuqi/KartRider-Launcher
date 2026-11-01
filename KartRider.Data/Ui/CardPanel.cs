using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>圆角白卡片。子控件请把 BackColor 设成 <see cref="Theme.Surface"/>，否则会露出方角。</summary>
    public class CardPanel : Panel
    {
        private Color canvas = Theme.Bg;

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Theme.Surface;
            Font = Theme.F(9f);
        }

        /// <summary>
        /// 卡片外面那层的底色（跟着父容器走）。
        /// 设成 Color.Transparent 时不铺底，让父容器的背景图透上来。
        /// </summary>
        public Color Canvas
        {
            get { return canvas; }
            set
            {
                canvas = value;
                BackColor = value == Color.Transparent ? Color.Transparent : Theme.Surface;
                Invalidate();
            }
        }

        public int Radius { get; set; } = 10;

        public bool ShowBorder { get; set; } = true;

        /// <summary>
        /// 卡片表面的不透明度 0~255。255 = 纯色卡片（默认）；
        /// 有背景图时调低，让下面的图片透上来。
        /// </summary>
        public int SurfaceAlpha
        {
            get { return surfaceAlpha; }
            set
            {
                int next = value < 0 ? 0 : (value > 255 ? 255 : value);
                if (next == surfaceAlpha)
                {
                    return;
                }
                surfaceAlpha = next;
                Invalidate();
            }
        }

        private Bitmap frosted;
        private int surfaceAlpha = 255;

        /// <summary>毛玻璃底图（与卡片同尺寸）。设置后由卡片负责释放。</summary>
        public Bitmap Frosted
        {
            get { return frosted; }
            set
            {
                if (ReferenceEquals(frosted, value))
                {
                    return;
                }
                frosted?.Dispose();
                frosted = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            if (canvas != Color.Transparent)
            {
                using (var bg = new SolidBrush(canvas))
                {
                    g.FillRectangle(bg, ClientRectangle);
                }
            }

            var box = new Rectangle(0, 0, Width, Height);
            if (frosted != null)
            {
                // 模糊底图裁进圆角内，再盖一层卡片色，保证文字可读
                GraphicsState state = g.Save();
                using (GraphicsPath clip = Theme.Round(box, Radius))
                {
                    g.SetClip(clip);
                    g.DrawImageUnscaled(frosted, 0, 0);
                }
                g.Restore(state);
            }

            Theme.Fill(g, box, Radius, Color.FromArgb(surfaceAlpha, Theme.Surface));
            if (ShowBorder)
            {
                Theme.Stroke(g, box, Radius, Theme.Border);
            }
            base.OnPaint(e);
        }
    }
}
