using System;
using System.Drawing;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 玻璃面板：在背景图之上盖一层白色蒙版，用来做"整窗背景"模式下的标题栏与侧边栏。
    /// <see cref="Wash"/> = 255 时就是普通白色面板；越小越透，能看到后面的背景图。
    /// </summary>
    public class GlassPanel : Panel
    {
        private int wash = 255;

        public GlassPanel()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Theme.Surface;
            DoubleBuffered = true;
        }

        /// <summary>白色蒙版的不透明度 0~255。0 表示完全透明，255 表示纯白。</summary>
        public int Wash
        {
            get { return wash; }
            set
            {
                int next = value < 0 ? 0 : (value > 255 ? 255 : value);
                if (next == wash)
                {
                    return;
                }
                wash = next;
                BackColor = wash >= 255 ? Theme.Surface : Color.Transparent;
                Invalidate(true);
            }
        }

        private Bitmap frosted;

        /// <summary>
        /// 毛玻璃底图（与控件同尺寸）。设置后由本控件负责释放，调用方只负责给新图。
        /// </summary>
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
                Invalidate(true);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (frosted != null)
            {
                e.Graphics.DrawImageUnscaled(frosted, 0, 0);
            }
            else if (wash >= 255)
            {
                using (var brush = new SolidBrush(Theme.Surface))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                return;
            }
            else
            {
                // BackColor 为透明时，这里会把父容器（含背景图）先画进来，再叠蒙版
                base.OnPaintBackground(e);
            }

            if (wash < 255)
            {
                using (var brush = new SolidBrush(Color.FromArgb(Math.Max(0, wash), Theme.GlassTint)))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }
    }
}
