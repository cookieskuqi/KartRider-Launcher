using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 自绘复选框。系统 CheckBox 的勾在深色底上几乎看不见（勾是浅灰、方块是白），
    /// 这里统一画成"圆角方框 + 勾选时主题色填充 + 白色对勾"。
    /// </summary>
    public class FlatCheckBox : Control
    {
        private const int BoxSize = 15;
        private const int Gap = 8;

        private bool isChecked;
        private bool hovered;
        private bool pressed;

        public FlatCheckBox()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = Theme.TextPrimary;
            Font = Theme.F(9.5f);
            Size = new Size(100, 22);
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value)
                {
                    return;
                }
                isChecked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler CheckedChanged;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cy = Height / 2;
            var box = new Rectangle(0, cy - BoxSize / 2, BoxSize, BoxSize);

            Color fill = isChecked ? (pressed ? Theme.AccentDown : Theme.Accent) : Theme.Field;
            Color border = isChecked ? Theme.Accent : (hovered ? Theme.Accent : Theme.Border);

            Theme.Fill(g, box, 4, fill);
            Theme.Stroke(g, box, 4, border);

            if (isChecked)
            {
                using (var pen = new Pen(Color.White, 2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawLines(pen, new[]
                    {
                        new Point(box.X + 3, box.Y + 7),
                        new Point(box.X + 6, box.Y + 10),
                        new Point(box.X + 12, box.Y + 4)
                    });
                }
            }

            var textRect = new Rectangle(BoxSize + Gap, 0, Math.Max(0, Width - BoxSize - Gap), Height);
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (pressed && e.Button == MouseButtons.Left)
            {
                pressed = false;
                Checked = !Checked;
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }
    }
}
