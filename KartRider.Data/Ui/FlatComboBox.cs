using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 自绘下拉框：圆角扁平，替掉系统 ComboBox 的凹凸边框。
    /// 交互与原生 DropDown 风格一致——点右侧箭头展开列表，点文字区直接输入。
    /// 对外暴露 Items / SelectedItem / SelectedIndex / Text / SelectedIndexChanged，
    /// 用法和 ComboBox 接近，方便直接替换。
    /// </summary>
    public class FlatComboBox : Control
    {
        private const int ArrowArea = 32;

        private readonly List<object> items = new List<object>();
        private readonly TextBox editor = new TextBox();
        private int index = -1;
        private bool hovered;
        private bool opened;
        private Color surface = Theme.Surface;

        private ToolStripDropDown drop;
        private ToolStripControlHost host;
        private ListBox list;

        public FlatComboBox()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);

            Font = Theme.F(9.5f);
            // 底色由父容器绘制，避免半透明卡片上出现方角色块
            BackColor = Color.Transparent;
            Size = new Size(160, 30);

            editor.BorderStyle = BorderStyle.None;
            editor.Font = Font;
            editor.BackColor = Theme.Field;
            editor.ForeColor = Theme.TextPrimary;
            editor.Location = new Point(10, 6);
            editor.Width = Width - 44;
            editor.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            editor.TextChanged += Editor_TextChanged;
            editor.KeyDown += (s, e) => OnKeyDown(e);
            editor.Leave += (s, e) => OnLeave(e);
            Controls.Add(editor);
        }

        /// <summary>下拉框所在卡片的底色。</summary>
        public Color Surface
        {
            get { return surface; }
            set { surface = value; Invalidate(); }
        }

        /// <summary>下拉列表的条目（存原始对象，显示时调用 ToString）。</summary>
        public IList Items
        {
            get { return items; }
        }

        public object SelectedItem
        {
            get { return index >= 0 && index < items.Count ? items[index] : null; }
        }

        public int SelectedIndex
        {
            get { return index; }
            set
            {
                int next = value >= 0 && value < items.Count ? value : -1;
                if (next == index)
                {
                    return;
                }
                index = next;
                if (index >= 0)
                {
                    // 只在真正选中某一项时改写文本，避免把用户正在输入的地址清空
                    editor.Text = Display(items[index]);
                }
                Invalidate();
                OnSelectedIndexChanged(EventArgs.Empty);
            }
        }

        public override string Text
        {
            get { return editor.Text; }
            set
            {
                string next = value ?? string.Empty;
                // 文本相同的时候 TextChanged 不会触发，这里必须自己再算一次选中项，
                // 否则"刷新列表并保持当前地址"之后 SelectedItem 会变成 null
                if (editor.Text != next)
                {
                    editor.Text = next;
                }
                int match = IndexOfDisplay(next);
                if (match != index)
                {
                    index = match;
                    Invalidate();
                }
            }
        }

        /// <summary>换肤后刷新内部编辑框的颜色。</summary>
        public void RefreshTheme()
        {
            editor.BackColor = Theme.Field;
            editor.ForeColor = Theme.TextPrimary;
            Invalidate();
        }

        public event EventHandler SelectedIndexChanged;

        private void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
        }

        private static string Display(object item)
        {
            return item == null ? string.Empty : item.ToString();
        }

        private int IndexOfDisplay(string text)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(Display(items[i]), text, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private void Editor_TextChanged(object sender, EventArgs e)
        {
            int match = IndexOfDisplay(editor.Text);
            if (match != index)
            {
                index = match;
                Invalidate();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            editor.Width = Math.Max(20, Width - 44);
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
            if (e.Button == MouseButtons.Left)
            {
                if (e.X >= Width - ArrowArea)
                {
                    if (opened)
                    {
                        drop?.Close();
                    }
                    else
                    {
                        OpenDropDown();
                    }
                }
                else
                {
                    editor.Focus();
                    editor.SelectionStart = editor.TextLength;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            var box = new Rectangle(0, 0, Width, Height);
            Theme.Fill(g, box, 6, Theme.Field);
            Theme.Stroke(g, box, 6, hovered || opened ? Theme.Accent : Theme.Border);

            // 右侧箭头
            int cx = Width - ArrowArea / 2 - 2;
            int cy = Height / 2;
            using (var pen = new Pen(Theme.TextSecondary, 1.6f))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
                g.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
            }
        }

        private void EnsureDropDown()
        {
            if (drop != null)
            {
                return;
            }

            list = new ListBox
            {
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28,
                IntegralHeight = false,
                Font = Font,
                BackColor = Theme.Surface,
                ForeColor = Theme.TextPrimary
            };
            list.DrawItem += List_DrawItem;
            list.MouseUp += List_MouseUp;

            host = new ToolStripControlHost(list)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };

            drop = new ToolStripDropDown
            {
                Padding = Padding.Empty,
                AutoClose = true,
                DropShadowEnabled = true,
                BackColor = Theme.Surface
            };
            drop.Items.Add(host);
            drop.Closed += (s, e) =>
            {
                opened = false;
                Invalidate();
            };
        }

        private void OpenDropDown()
        {
            EnsureDropDown();
            if (items.Count == 0)
            {
                return;
            }

            list.BeginUpdate();
            list.Items.Clear();
            foreach (object item in items)
            {
                list.Items.Add(Display(item));
            }
            list.EndUpdate();

            int height = Math.Min(items.Count, 8) * list.ItemHeight + 2;
            var size = new Size(Math.Max(Width, 200), height);
            list.Size = size;
            host.Size = size;
            list.SelectedIndex = index;

            opened = true;
            Invalidate();
            drop.Show(this, new Point(0, Height + 2));
        }

        private void List_MouseUp(object sender, MouseEventArgs e)
        {
            int hit = list.IndexFromPoint(e.Location);
            if (hit >= 0)
            {
                Commit(hit);
            }
            drop?.Close();
        }

        private void Commit(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex >= items.Count)
            {
                return;
            }
            index = itemIndex;
            editor.Text = Display(items[itemIndex]);
            Invalidate();
            OnSelectedIndexChanged(EventArgs.Empty);
        }

        private void List_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
            {
                return;
            }
            bool active = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color back = active ? Theme.AccentSoft : Theme.Surface;
            Color fore = active ? Theme.Accent : Theme.TextPrimary;

            using (var brush = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }
            var textRect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y, e.Bounds.Width - 18, e.Bounds.Height);
            TextRenderer.DrawText(e.Graphics, list.Items[e.Index].ToString(), Font, textRect, fore,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                drop?.Dispose();
                list?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
