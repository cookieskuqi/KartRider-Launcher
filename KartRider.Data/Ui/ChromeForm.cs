using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>
    /// 无边框窗口基类：自绘标题栏（图标 + 标题 + 最小化/关闭），支持拖动、
    /// Win11 自动圆角与投影。子窗体把内容放在 y = ChromeHeight 以下即可。
    /// </summary>
    public class ChromeForm : Form
    {
        public const int ChromeHeight = 44;

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private const int CS_DROPSHADOW = 0x20000;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        protected GlassPanel TitleBar;
        protected Label TitleLabel;
        protected FlatButton MinimizeButton;
        protected FlatButton CloseButton;

        protected void BuildChrome(string title, bool showMinimize)
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.Bg;
            Font = Theme.F(9f);
            DoubleBuffered = true;

            TitleBar = new GlassPanel
            {
                Name = "ChromeTitleBar",
                BackColor = Theme.Surface,
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, ChromeHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            TitleBar.MouseDown += ChromeDrag;
            TitleBar.Paint += TitleBar_Paint;

            if (Icon != null)
            {
                var logo = new PictureBox
                {
                    Name = "ChromeLogo",
                    Location = new Point(16, 11),
                    Size = new Size(22, 22),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = Icon.ToBitmap(),
                    BackColor = Theme.Surface
                };
                logo.MouseDown += ChromeDrag;
                TitleBar.Controls.Add(logo);
            }

            TitleLabel = new Label
            {
                Name = "ChromeTitle",
                Text = title,
                AutoSize = true,
                Location = new Point(48, 14),
                Font = Theme.F(10f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface
            };
            TitleLabel.MouseDown += ChromeDrag;
            TitleBar.Controls.Add(TitleLabel);

            CloseButton = new FlatButton
            {
                Name = "ChromeClose",
                Text = "✕",
                Style = FlatButtonStyle.DangerGhost,
                Surface = Theme.Surface,
                Font = Theme.F(10f),
                Radius = 0,
                Size = new Size(44, ChromeHeight),
                Location = new Point(ClientSize.Width - 44, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false
            };
            CloseButton.Click += (s, e) => Close();
            TitleBar.Controls.Add(CloseButton);

            if (showMinimize)
            {
                MinimizeButton = new FlatButton
                {
                    Name = "ChromeMinimize",
                    Text = "—",
                    Style = FlatButtonStyle.Ghost,
                    Surface = Theme.Surface,
                    Font = Theme.F(10f),
                    Radius = 0,
                    Size = new Size(44, ChromeHeight),
                    Location = new Point(ClientSize.Width - 88, 0),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    TabStop = false
                };
                MinimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;
                TitleBar.Controls.Add(MinimizeButton);
            }

            Controls.Add(TitleBar);
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// 调整标题栏的透明程度：255 = 普通白色标题栏，越小越透（整窗背景图模式下用）。
        /// 同时把标题文字、图标和窗口按钮的底色跟着切到透明，否则会留白色方块。
        /// </summary>
        protected void ApplyChromeWash(int wash)
        {
            if (TitleBar == null)
            {
                return;
            }

            TitleBar.Wash = wash;
            Color surface = wash >= 255 ? Theme.Surface : Color.Transparent;

            if (TitleLabel != null)
            {
                TitleLabel.BackColor = surface;
            }
            if (CloseButton != null)
            {
                CloseButton.Surface = surface;
            }
            if (MinimizeButton != null)
            {
                MinimizeButton.Surface = surface;
            }
            foreach (Control child in TitleBar.Controls)
            {
                if (child is PictureBox)
                {
                    child.BackColor = surface;
                }
            }
        }

        /// <summary>标题栏底部那条 1px 分隔线。</summary>
        private void TitleBar_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, 0, TitleBar.Height - 1, TitleBar.Width, TitleBar.Height - 1);
            }
        }

        private void ChromeDrag(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            ReleaseCapture();
            // 交给系统处理，顺便得到贴边吸附/双击最大化的行为
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int preference = DWMWCP_ROUND;
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch
            {
                // Win10 及更早版本没有这个属性，忽略
            }
        }

        /// <summary>把窗口宽度变化同步到标题栏右侧按钮。</summary>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (TitleBar == null)
            {
                return;
            }
            TitleBar.Width = ClientSize.Width;
            if (CloseButton != null)
            {
                CloseButton.Location = new Point(ClientSize.Width - 44, 0);
            }
            if (MinimizeButton != null)
            {
                MinimizeButton.Location = new Point(ClientSize.Width - 88, 0);
            }
        }

        /// <summary>给卡片内的文字用：统一的次要说明样式。</summary>
        protected static Label Caption(string text, Point location)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = location,
                Font = Theme.F(9f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.Surface
            };
        }

        /// <summary>给卡片内的文字用：统一的主文本样式。</summary>
        protected static Label Body(string text, Point location, float size = 10.5f)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = location,
                Font = Theme.F(size, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface
            };
        }
    }
}
