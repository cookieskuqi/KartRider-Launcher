using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 自绘滑块，用于调透明度这类 0~100 的数值。原生 TrackBar 太"系统"，和整体风格不搭。
    /// </summary>
    public class FlatSlider : Control
    {
        private const int KnobRadius = 7;
        private const int TrackHeight = 4;

        private int value = 100;
        private bool dragging;

        public FlatSlider()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);
            Surface = Theme.Surface;
            Size = new Size(180, 26);
            Cursor = Cursors.Hand;
        }

        /// <summary>控件所在卡片的底色。</summary>
        public Color Surface { get; set; } = Theme.Surface;

        public int Minimum { get; set; } = 0;

        public int Maximum { get; set; } = 100;

        public int Value
        {
            get { return value; }
            set
            {
                int next = Math.Max(Minimum, Math.Min(Maximum, value));
                if (next == this.value)
                {
                    return;
                }
                this.value = next;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;

        /// <summary>换肤后刷新底色。</summary>
        public void RefreshTheme()
        {
            Surface = Theme.Surface;
            Invalidate();
        }

        private Rectangle TrackBounds
        {
            get
            {
                return new Rectangle(KnobRadius, Height / 2 - TrackHeight / 2, Math.Max(1, Width - KnobRadius * 2), TrackHeight);
            }
        }

        private int KnobX
        {
            get
            {
                Rectangle track = TrackBounds;
                double ratio = Maximum == Minimum ? 0 : (double)(value - Minimum) / (Maximum - Minimum);
                return track.Left + (int)Math.Round(track.Width * ratio);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var bg = new SolidBrush(Surface))
            {
                g.FillRectangle(bg, ClientRectangle);
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle track = TrackBounds;
            using (var back = new SolidBrush(Theme.Border))
            {
                g.FillRectangle(back, track);
            }

            int knobX = KnobX;
            var filled = new Rectangle(track.Left, track.Top, Math.Max(0, knobX - track.Left), track.Height);
            if (filled.Width > 0)
            {
                using (var accent = new SolidBrush(Theme.Accent))
                {
                    g.FillRectangle(accent, filled);
                }
            }

            // 滑块：白圆 + 主题色描边
            var knob = new Rectangle(knobX - KnobRadius, Height / 2 - KnobRadius, KnobRadius * 2, KnobRadius * 2);
            using (var white = new SolidBrush(Color.White))
            {
                g.FillEllipse(white, knob);
            }
            using (var pen = new Pen(dragging ? Theme.AccentHover : Theme.Accent, 2f))
            {
                g.DrawEllipse(pen, knob);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                SetValueFromX(e.X);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                SetValueFromX(e.X);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        private void SetValueFromX(int x)
        {
            Rectangle track = TrackBounds;
            double ratio = track.Width <= 0 ? 0 : (double)(x - track.Left) / track.Width;
            ratio = Math.Max(0, Math.Min(1, ratio));
            Value = Minimum + (int)Math.Round((Maximum - Minimum) * ratio);
            Invalidate();
        }
    }
}
