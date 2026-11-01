using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using KartRider.Common.Data;
using KartRider.Logging;
using KartRider.Ui;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 校验并修复游戏文件：对照服务器上的补丁清单逐个比对 MD5，
    /// 缺失或损坏的文件会自动重新下载。运行期间显示进度，结束后给出统计。
    /// </summary>
    public class VerifyDialog : ChromeForm
    {
        private readonly string gamePath;
        private readonly Label StatusLabel = new Label();
        private readonly Label ResultLabel = new Label();
        private readonly MarqueeBar Bar = new MarqueeBar();
        private readonly FlatButton CloseBtn = new FlatButton();
        private readonly bool autoStart;
        private bool running;

        /// <param name="autoStart">窗口显示后是否立刻开始校验（调试/预览时可传 false）。</param>
        public VerifyDialog(string kartRiderDirectory, bool autoStart = true)
        {
            gamePath = kartRiderDirectory;
            this.autoStart = autoStart;

            ShowInTaskbar = false;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(560, 300);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.F(9f);

            Icon = Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            BuildChrome("校验游戏文件", false);

            var card = new CardPanel
            {
                Canvas = Theme.Bg,
                Location = new Point(20, 60),
                Size = new Size(520, 128)
            };

            var intro = new Label
            {
                Text = "正在对照服务器上的文件清单逐个校验，缺失或损坏的文件会自动重新下载。\r\n" +
                       "文件较多时可能需要几分钟，期间请不要关闭窗口。",
                AutoSize = false,
                Size = new Size(482, 44),
                Font = Theme.F(9f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.Surface,
                Location = new Point(19, 16)
            };

            StatusLabel.AutoSize = false;
            StatusLabel.Size = new Size(482, 20);
            StatusLabel.Font = Theme.F(9.5f);
            StatusLabel.ForeColor = Theme.TextPrimary;
            StatusLabel.BackColor = Theme.Surface;
            StatusLabel.AutoEllipsis = true;
            StatusLabel.Text = "准备中…";
            StatusLabel.Location = new Point(19, 66);

            Bar.Location = new Point(19, 94);
            Bar.Size = new Size(482, 8);
            Bar.Surface = Theme.Surface;

            card.Controls.Add(intro);
            card.Controls.Add(StatusLabel);
            card.Controls.Add(Bar);

            ResultLabel.AutoSize = false;
            ResultLabel.Size = new Size(520, 48);
            ResultLabel.Font = Theme.F(9f);
            ResultLabel.ForeColor = Theme.TextSecondary;
            ResultLabel.BackColor = Theme.Bg;
            ResultLabel.Location = new Point(24, 196);

            CloseBtn.Name = "CloseBtn";
            CloseBtn.Text = "请稍候…";
            CloseBtn.Style = FlatButtonStyle.Primary;
            CloseBtn.Surface = Theme.Bg;
            CloseBtn.Radius = 8;
            CloseBtn.Font = Theme.F(9.5f, FontStyle.Bold);
            CloseBtn.Location = new Point(448, 246);
            CloseBtn.Size = new Size(92, 36);
            CloseBtn.Enabled = false;
            CloseBtn.Click += (s, e) => Close();

            Controls.Add(card);
            Controls.Add(ResultLabel);
            Controls.Add(CloseBtn);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (autoStart)
            {
                _ = RunAsync();
            }
        }

        private async Task RunAsync()
        {
            running = true;
            Bar.Running = true;

            try
            {
                var probe = await Task.Run(() => PatchManager.GetUpdateAsync());
                string updateUrl = probe.Item1;
                if (string.IsNullOrWhiteSpace(updateUrl))
                {
                    Finish(false, "无法从当前服务器获取补丁地址。\r\n请确认启动器已连上服务器（设置里的 IP / 端口）后再试。");
                    return;
                }

                IProgress<string> progress = new Progress<string>(text => StatusLabel.Text = text);
                PatchSummary summary = await Task.Run(() => new PatchManager()
                    .StartPatchAsync(updateUrl, gamePath, text => progress.Report(text)));

                RefreshClientVersion();

                string detail = summary == null
                    ? "校验完成。"
                    : $"共校验 {summary.Total} 个文件，修复 {summary.Downloaded} 个。";
                Finish(true, detail);
            }
            catch (Exception ex)
            {
                Log.Info($"[Verify] 校验失败: {ex}");
                Finish(false, "校验未完成：" + ex.Message);
            }
        }

        /// <summary>校验进行中不允许关窗口，避免后台任务仍在跑、界面却被释放。</summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (running && e.CloseReason == CloseReason.UserClosing)
            {
                MessageBox.Show(this, "正在校验文件，请等待完成后再关闭。", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }

        /// <summary>文件更新后 PIN 头里的客户端版本可能变了，重新读一次写回配置。</summary>
        private void RefreshClientVersion()
        {
            try
            {
                string pin = Path.Combine(gamePath, "KartRider.pin");
                if (!File.Exists(pin))
                {
                    return;
                }
                var file = new PINFile(pin);
                ProfileService.SettingConfig.ClientVersion = file.Header.MinorVersion;
                ProfileService.SettingConfig.LocaleID = file.Header.LocaleID;
                ProfileService.SettingConfig.nClientLoc = file.Header.Unk2;
                ProfileService.SaveSettings();
            }
            catch (Exception ex)
            {
                Log.Info($"[Verify] 读取版本失败: {ex.Message}");
            }
        }

        private void Finish(bool success, string message)
        {
            running = false;
            Bar.Running = false;
            StatusLabel.Text = success ? "校验完成" : "校验未完成";
            StatusLabel.ForeColor = success ? Theme.TextPrimary : Theme.Danger;
            ResultLabel.Text = message;
            ResultLabel.ForeColor = success ? Theme.TextSecondary : Theme.Danger;
            CloseBtn.Text = "关闭";
            CloseBtn.Enabled = true;
            CloseBtn.Focus();
        }

        /// <summary>不确定进度条：一条来回移动的色块，避免"卡住了"的错觉。</summary>
        private sealed class MarqueeBar : Control
        {
            private readonly Timer timer = new Timer { Interval = 30 };
            private int pos;
            private bool running;
            private bool started;
            private Color surface = Theme.Surface;

            public MarqueeBar()
            {
                SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor, true);
                BackColor = Color.Transparent;
                timer.Tick += (s, e) =>
                {
                    pos += 6;
                    if (pos > Width) pos = -Width / 3;
                    Invalidate();
                };
            }

            public Color Surface
            {
                get { return surface; }
                set { surface = value; Invalidate(); }
            }

            public bool Running
            {
                get { return running; }
                set
                {
                    running = value;
                    if (value) { started = true; timer.Start(); } else timer.Stop();
                    Invalidate();
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var track = new Rectangle(0, 0, Width, Height);
                Theme.Fill(g, track, Height / 2, Theme.Border);
                if (!running)
                {
                    // 跑完显示满格，还没开始则留空轨道
                    if (started)
                    {
                        Theme.Fill(g, track, Height / 2, Theme.Accent);
                    }
                    return;
                }

                int blockWidth = Math.Max(40, Width / 3);
                int x = pos;
                var block = new Rectangle(x, 0, blockWidth, Height);
                using (var clip = Theme.Round(track, Height / 2))
                {
                    GraphicsState state = g.Save();
                    g.SetClip(clip);
                    Theme.Fill(g, block, Height / 2, Theme.Accent);
                    g.Restore(state);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) timer.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}
