using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 公告显示控件。用 RichTextBox 实现，文字可以选中、复制（Ctrl+C / 右键复制），
    /// 正文里的网址会被自动识别成链接，点一下就打开浏览器。
    /// 底色由外部按背景图算好传进来，尽量和半透明卡片融为一体。
    /// </summary>
    public class NoticeView : RichTextBox
    {
        private readonly List<NoticeItem> items = new List<NoticeItem>();
        private string emptyText = "还没有公告";

        public NoticeView()
        {
            BorderStyle = BorderStyle.None;
            ReadOnly = true;
            WordWrap = true;
            DetectUrls = true;
            ScrollBars = RichTextBoxScrollBars.Vertical;
            HideSelection = false;
            ShortcutsEnabled = true;
            TabStop = false;
            Font = Theme.F(9f);
            ForeColor = Theme.TextPrimary;
            BackColor = Theme.Surface;
        }

        /// <summary>没有公告时显示的提示文字。</summary>
        public string EmptyText
        {
            get { return emptyText; }
            set
            {
                emptyText = value ?? string.Empty;
                Render();
            }
        }

        /// <summary>设置公告内容。</summary>
        public void SetItems(List<NoticeItem> list)
        {
            items.Clear();
            if (list != null)
            {
                items.AddRange(list);
            }
            Render();
        }

        /// <summary>换肤后重新渲染一次，让颜色跟着变。</summary>
        public void RefreshTheme()
        {
            ForeColor = Theme.TextPrimary;
            Render();
        }

        /// <summary>设置控件底色（通常是"背景图平均值 + 卡片色"的合成色）。</summary>
        public void SetBackdrop(Color color)
        {
            if (color.A == 0)
            {
                color = Theme.Surface;
            }
            if (BackColor != color)
            {
                BackColor = color;
            }
        }

        protected override void OnLinkClicked(LinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
            }
            catch
            {
                // 打不开就算了，不影响阅读
            }
            base.OnLinkClicked(e);
        }

        private void Render()
        {
            try
            {
                string rtf = BuildRtf();
                if (Rtf != rtf)
                {
                    Rtf = rtf;
                }
                SelectionStart = 0;
                SelectionLength = 0;
                ScrollToCaret();
            }
            catch (Exception)
            {
                // RTF 拼错时退回纯文本，至少内容还在
                Text = emptyText;
            }
        }

        private string BuildRtf()
        {
            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil\fcharset134 Microsoft YaHei UI;}}");
            sb.Append(@"{\colortbl ;");
            AppendColor(sb, Theme.TextPrimary);   // 1
            AppendColor(sb, Theme.TextSecondary); // 2
            AppendColor(sb, Theme.Accent);        // 3
            sb.Append('}');

            if (items.Count == 0)
            {
                sb.Append(@"\pard\sl260\cf2\fs18 ").Append(Escape(emptyText)).Append(@"\par");
                sb.Append('}');
                return sb.ToString();
            }

            foreach (NoticeItem item in items)
            {
                sb.Append(@"\pard\sl260 ");
                if (!string.IsNullOrEmpty(item.Date))
                {
                    sb.Append(@"{\cf3\fs16 ").Append(Escape(item.Date)).Append("}  ");
                }
                sb.Append(@"{\b\cf1\fs19 ").Append(Escape(item.Title)).Append(@"}\par");

                if (!string.IsNullOrWhiteSpace(item.Body))
                {
                    sb.Append(@"\pard\sl260{\cf2\fs18 ").Append(Escape(item.Body)).Append(@"}\par");
                }
                sb.Append(@"\pard\par");
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendColor(StringBuilder sb, Color color)
        {
            sb.Append(@"\red").Append(color.R)
              .Append(@"\green").Append(color.G)
              .Append(@"\blue").Append(color.B)
              .Append(';');
        }

        /// <summary>RTF 转义：反斜杠、花括号要转义，非 ASCII 写成 \uNNNN?，换行写成 \line。</summary>
        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(text.Length + 16);
            foreach (char c in text)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append(@"\\");
                        break;
                    case '{':
                        sb.Append(@"\{");
                        break;
                    case '}':
                        sb.Append(@"\}");
                        break;
                    case '\r':
                        break;
                    case '\n':
                        sb.Append(@"\line ");
                        break;
                    default:
                        if (c < 128)
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            sb.Append(@"\u").Append((int)c).Append('?');
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
