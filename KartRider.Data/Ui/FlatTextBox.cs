using System;
using System.Drawing;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 自绘输入框：外观和 <see cref="FlatComboBox"/> 保持一致（圆角、浅底、悬停加深边框），
    /// 内部仍然用原生 TextBox 处理输入法、选中、右键菜单等。
    /// </summary>
    public class FlatTextBox : Control
    {
        private readonly TextBox editor = new TextBox();
        private Color surface = Theme.Surface;
        private bool hovered;

        public FlatTextBox()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw, true);

            BackColor = Theme.Field;
            Size = new Size(160, 30);

            editor.BorderStyle = BorderStyle.None;
            editor.Font = Theme.F(9.5f);
            editor.BackColor = Theme.Field;
            editor.ForeColor = Theme.TextPrimary;
            editor.Location = new Point(10, 6);
            editor.Width = Width - 20;
            editor.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            editor.GotFocus += (s, e) => Invalidate();
            editor.LostFocus += (s, e) => Invalidate();
            Controls.Add(editor);
        }

        /// <summary>内部的原生 TextBox，需要 SelectAll / Focus 之类的操作时用。</summary>
        public TextBox Editor
        {
            get { return editor; }
        }

        public Color Surface
        {
            get { return surface; }
            set { surface = value; Invalidate(); }
        }

        public override string Text
        {
            get { return editor.Text; }
            set { editor.Text = value ?? string.Empty; }
        }

        public int MaxLength
        {
            get { return editor.MaxLength; }
            set { editor.MaxLength = value; }
        }

        public new void Focus()
        {
            editor.Focus();
        }

        /// <summary>换肤后刷新内部编辑框的颜色。</summary>
        public void RefreshTheme()
        {
            editor.BackColor = Theme.Field;
            editor.ForeColor = Theme.TextPrimary;
            Invalidate();
        }

        public void SelectAll()
        {
            editor.SelectAll();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            editor.Width = Math.Max(20, Width - 20);
            editor.Top = Math.Max(2, (Height - editor.Height) / 2);
            Invalidate();
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
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            editor.Focus();
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var bg = new SolidBrush(surface))
            {
                g.FillRectangle(bg, ClientRectangle);
            }

            var box = new Rectangle(0, 0, Width, Height);
            Theme.Fill(g, box, 6, Theme.Field);
            bool focused = editor.Focused;
            Theme.Stroke(g, box, 6, focused ? Theme.Accent : (hovered ? Theme.Accent : Theme.Border));
        }
    }
}
