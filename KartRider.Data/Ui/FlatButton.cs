using System;
using System.Drawing;
using System.Windows.Forms;

namespace KartRider.Ui
{
    public enum FlatButtonStyle
    {
        /// <summary>主按钮：主题色纯色块，白字。</summary>
        Primary,
        /// <summary>次按钮：白底 + 细边框。</summary>
        Secondary,
        /// <summary>透明按钮：平时和背景同色，悬停才浮出浅灰底，用于导航项。</summary>
        Ghost,
        /// <summary>关闭按钮：悬停变红。</summary>
        DangerGhost
    }

    /// <summary>
    /// 自绘圆角按钮。WinForms 自带按钮没法做圆角悬停态，这里全部自己画。
    /// 实现 IButtonControl 才能被窗体设为 AcceptButton / CancelButton。
    /// </summary>
    public class FlatButton : Control, IButtonControl
    {
        private FlatButtonStyle style = FlatButtonStyle.Secondary;
        private bool hovered;
        private bool pressed;
        private bool selected;
        private int radius = 8;
        private Color surface = Color.White;
        private DialogResult dialogResult = DialogResult.None;
        private ContentAlignment textAlign = ContentAlignment.MiddleCenter;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);
            Font = Theme.F(9.5f);
            Cursor = Cursors.Hand;
            ForeColor = Theme.TextPrimary;
            // 底色交给父容器画（卡片可能是半透明的），自己只画圆角块，避免四个角留白框
            BackColor = Color.Transparent;
        }

        /// <summary>按钮所处容器的底色：控件先铺这一层，再画自己的圆角块，避免出现方角。</summary>
        public Color Surface
        {
            get { return surface; }
            set { surface = value; Invalidate(); }
        }

        public FlatButtonStyle Style
        {
            get { return style; }
            set { style = value; Invalidate(); }
        }

        public int Radius
        {
            get { return radius; }
            set { radius = value; Invalidate(); }
        }

        /// <summary>导航项是否处于选中状态（Ghost 样式下会显示主题色底）。</summary>
        public bool Selected
        {
            get { return selected; }
            set { selected = value; Invalidate(); }
        }

        /// <summary>文字对齐方式。导航项用 MiddleLeft，普通按钮保持居中。</summary>
        public ContentAlignment TextAlign
        {
            get { return textAlign; }
            set { textAlign = value; Invalidate(); }
        }

        /// <summary>配合窗体的 AcceptButton / CancelButton 使用。</summary>
        public DialogResult DialogResult
        {
            get { return dialogResult; }
            set { dialogResult = value; }
        }

        public void NotifyDefault(bool value)
        {
            // 自绘按钮不区分默认按钮的外观
        }

        public void PerformClick()
        {
            if (CanSelect)
            {
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (dialogResult != DialogResult.None)
            {
                Form form = FindForm();
                if (form != null)
                {
                    form.DialogResult = dialogResult;
                }
            }
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
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle box = new Rectangle(0, 0, Width, Height);
            Color fill;
            Color text;
            bool border = false;

            switch (style)
            {
                case FlatButtonStyle.Primary:
                    fill = pressed ? Theme.AccentDown : (hovered ? Theme.AccentHover : Theme.Accent);
                    text = Color.White;
                    break;
                case FlatButtonStyle.DangerGhost:
                    fill = hovered ? Theme.Danger : surface;
                    text = hovered ? Color.White : Theme.TextSecondary;
                    break;
                case FlatButtonStyle.Ghost:
                    if (selected)
                    {
                        fill = Theme.AccentSoft;
                        text = Theme.Accent;
                    }
                    else
                    {
                        fill = (hovered || pressed) ? Theme.HoverBg : surface;
                        text = Theme.TextPrimary;
                    }
                    break;
                default:
                    fill = (hovered || pressed) ? Theme.HoverBg : surface;
                    text = Theme.TextPrimary;
                    border = true;
                    break;
            }

            if (!Enabled)
            {
                fill = surface;
                text = Color.FromArgb(0xBF, 0xC5, 0xCE);
                border = style == FlatButtonStyle.Secondary;
            }

            if (fill.A > 0)
            {
                Theme.Fill(g, box, radius, fill);
            }
            if (border)
            {
                Theme.Stroke(g, box, radius, Theme.Border);
            }

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            Rectangle textBox = box;
            if (textAlign == ContentAlignment.MiddleLeft)
            {
                flags |= TextFormatFlags.Left;
                textBox = new Rectangle(20, 0, Width - 26, Height);
            }
            else
            {
                flags |= TextFormatFlags.HorizontalCenter;
            }
            TextRenderer.DrawText(g, Text, Font, textBox, text, flags);
        }
    }
}
