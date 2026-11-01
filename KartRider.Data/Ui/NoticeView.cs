using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 公告列表的显示控件：自绘、可滚轮滚动、右上角一条细滚动条。
    /// 背景透明，由外层卡片负责上色，所以能跟着背景图/毛玻璃一起变。
    /// </summary>
    public class NoticeView : Control
    {
        private const int PadX = 6;
        private const int PadY = 4;
        private const int Gap = 12;
        private const int ScrollBarWidth = 4;

        private readonly List<NoticeItem> items = new List<NoticeItem>();
        private int scroll;
        private string emptyText = "还没有公告";

        public NoticeView()
        {
            SetStyle(ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Font = Theme.F(9f);
        }

        /// <summary>没有公告时显示的提示文字。</summary>
        public string EmptyText
        {
            get { return emptyText; }
            set { emptyText = value ?? string.Empty; Invalidate(); }
        }

        public void SetItems(List<NoticeItem> list)
        {
            items.Clear();
            if (list != null)
            {
                items.AddRange(list);
            }
            scroll = 0;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int width = Width - PadX * 2;
            if (items.Count == 0)
            {
                TextRenderer.DrawText(g, emptyText, Font, new Rectangle(PadX, PadY, width, Height - PadY * 2),
                    Theme.TextSecondary, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak);
                return;
            }

            Font titleFont = Theme.F(9.5f, FontStyle.Bold);
            Font metaFont = Theme.F(8f);

            int y = PadY - scroll;
            int total = 0;

            foreach (NoticeItem item in items)
            {
                // 标题（顺带把日期画在同一行前面）
                int titleHeight = TextRenderer.MeasureText(g, item.Title, titleFont,
                    new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;

                int bodyHeight = 0;
                if (!string.IsNullOrWhiteSpace(item.Body))
                {
                    bodyHeight = TextRenderer.MeasureText(g, item.Body, Font,
                        new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
                }

                int blockHeight = titleHeight + bodyHeight + (bodyHeight > 0 ? 4 : 0);
                if (y + blockHeight >= 0 && y <= Height)
                {
                    if (!string.IsNullOrEmpty(item.Date))
                    {
                        Font dateFont = metaFont;
                        int dateWidth = TextRenderer.MeasureText(g, item.Date, dateFont).Width + 6;
                        TextRenderer.DrawText(g, item.Date, dateFont,
                            new Rectangle(PadX, y + 2, dateWidth, titleHeight),
                            Theme.Accent, TextFormatFlags.Left | TextFormatFlags.Top);
                        TextRenderer.DrawText(g, item.Title, titleFont,
                            new Rectangle(PadX + dateWidth, y, width - dateWidth, titleHeight),
                            Theme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                    }
                    else
                    {
                        TextRenderer.DrawText(g, item.Title, titleFont,
                            new Rectangle(PadX, y, width, titleHeight),
                            Theme.TextPrimary, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                    }

                    if (bodyHeight > 0)
                    {
                        TextRenderer.DrawText(g, item.Body, Font,
                            new Rectangle(PadX, y + titleHeight + 4, width, bodyHeight),
                            Theme.TextSecondary, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
                    }
                }

                y += blockHeight + Gap;
                total = y + scroll;
            }

            DrawScrollBar(g, total);
        }

        private void DrawScrollBar(Graphics g, int contentHeight)
        {
            int viewHeight = Height - PadY * 2;
            if (contentHeight <= viewHeight)
            {
                return;
            }

            int barHeight = Math.Max(24, viewHeight * viewHeight / contentHeight);
            int maxScroll = contentHeight - viewHeight;
            int barTop = PadY + (int)((long)(viewHeight - barHeight) * scroll / Math.Max(1, maxScroll));

            var track = new Rectangle(Width - ScrollBarWidth - 2, PadY, ScrollBarWidth, viewHeight);
            Theme.Fill(g, track, ScrollBarWidth / 2, Theme.Border);
            Theme.Fill(g, new Rectangle(track.X, barTop, ScrollBarWidth, barHeight), ScrollBarWidth / 2, Theme.TextSecondary);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            ScrollBy(-e.Delta / 120 * 48);
            base.OnMouseWheel(e);
        }

        private void ScrollBy(int delta)
        {
            int contentHeight = MeasureContentHeight();
            int viewHeight = Height - PadY * 2;
            int max = Math.Max(0, contentHeight - viewHeight);
            int next = Math.Max(0, Math.Min(max, scroll + delta));
            if (next != scroll)
            {
                scroll = next;
                Invalidate();
            }
        }

        private int MeasureContentHeight()
        {
            if (items.Count == 0)
            {
                return 0;
            }

            using (Graphics g = CreateGraphics())
            {
                int width = Width - PadX * 2;
                Font titleFont = Theme.F(9.5f, FontStyle.Bold);
                int total = PadY * 2;
                foreach (NoticeItem item in items)
                {
                    int h = TextRenderer.MeasureText(g, item.Title, titleFont,
                        new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
                    if (!string.IsNullOrWhiteSpace(item.Body))
                    {
                        h += 4 + TextRenderer.MeasureText(g, item.Body, Font,
                            new Size(width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
                    }
                    total += h + Gap;
                }
                return total;
            }
        }
    }
}
