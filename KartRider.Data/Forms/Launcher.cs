using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Profile;
using KartRider.Logging;
using KartRider.Ui;

namespace KartRider
{
    public partial class Launcher : ChromeForm
    {
        public string kartRiderDirectory;
        public static string KartRider;
        public static string pinFile;
        public static string pinFileBak;

        public Launcher()
        {
            RestorePinFile();
            this.InitializeComponent();
        }

        /// <summary>
        /// 恢复备份的 PIN 文件
        /// </summary>
        /// <returns>是否恢复成功</returns>
        private bool RestorePinFile()
        {
            if (string.IsNullOrEmpty(pinFileBak) || string.IsNullOrEmpty(pinFile))
                return false;

            if (!File.Exists(pinFileBak))
            {
                Log.Info($"[PIN] 备份文件不存在，无法恢复");
                return false;
            }

            try
            {
                if (File.Exists(pinFile))
                    File.Delete(pinFile);
                File.Move(pinFileBak, pinFile);
                return true;
            }
            catch (Exception ex)
            {
                Log.Info($"[PIN] 恢复 PIN 文件失败: {ex.Message}");
                return false;
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            RestorePinFile();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ClientVersion.Text = ProfileService.SettingConfig.ClientVersion.ToString();
            Log.Info($"ClientVersion: {ClientVersion.Text}");
            Log.Info($"程序编译时间: {CompileTime.Time}");
            VersionLabel.Text = CompileTime.Time;
            Log.Info($"Process: {KartRider}");
            RefreshServerInfo();
            ApplyTheme();
            RefreshBackground();
            RefreshNotice();
            button_ToggleTerminal.Text = Program.isVisible ? "隐藏控制台" : "显示控制台";

            string[] UserPaths = Directory.GetDirectories(FileName.ProfileDir);
            foreach (string userPath in UserPaths)
            {
                string UserName = Path.GetFileName(userPath);
                ClientManager.GetUserNO(UserName);
            }
        }

        /// <summary>把当前生效的服务器地址显示到主界面卡片上。</summary>
        private void RefreshServerInfo()
        {
            string ip = ProfileService.SettingConfig.ServerIP;
            ServerValue.Text = string.IsNullOrWhiteSpace(ip)
                ? "未设置"
                : ip + ":" + ProfileService.SettingConfig.ServerPort;
            StatusText.Text = LanIpGetter.IsIPv6(ip) ? "就绪 · IPv6 端口转发模式" : "就绪";
        }

        /// <summary>整窗模式下载入的背景图（内容区模式为 null）。</summary>
        private Image windowWallpaper;
        private int windowWallpaperOpacity = 100;

        /// <summary>
        /// 按配置刷新背景：可选"只铺内容区"或"铺满整窗"，并可叠加毛玻璃。
        /// 整窗模式下窗体自己画背景图，标题栏与侧栏切成半透明玻璃。
        /// </summary>
        private void RefreshBackground()
        {
            string path = ProfileService.SettingConfig.LauncherBackground;
            bool windowMode = string.Equals(
                ProfileService.SettingConfig.LauncherBackgroundMode, "window", StringComparison.OrdinalIgnoreCase);
            int opacity = Math.Max(0, Math.Min(100, ProfileService.SettingConfig.LauncherBackgroundOpacity));

            if (windowMode)
            {
                SetWindowWallpaper(path, opacity);
                ContentPanel.SeeThrough = windowWallpaper != null;
                ContentPanel.SetImage(string.Empty);
                ContentPanel.Opacity = opacity;
            }
            else
            {
                SetWindowWallpaper(null, opacity);
                ContentPanel.SeeThrough = false;
                ContentPanel.SetImage(path);
                ContentPanel.Opacity = opacity;
            }

            bool hasImage = windowMode ? windowWallpaper != null : ContentPanel.HasImage;
            bool frosted = ApplyFrosted(path, windowMode, hasImage);

            // 有背景图时卡片不再铺底色（否则圆角外会留下方形色块），状态文字也让位给图片
            Color canvas = hasImage ? Color.Transparent : Theme.Bg;
            CardServer.Canvas = canvas;
            CardInfo.Canvas = canvas;
            CardNotice.Canvas = canvas;
            StatusText.Visible = !hasImage;

            // 有背景图时卡片半透明，让图片透上来；具体多实由设置里的滑块决定
            int cardPercent = Math.Max(0, Math.Min(100, ProfileService.SettingConfig.LauncherCardOpacity));
            int cardAlpha = hasImage ? (int)Math.Round(cardPercent * 255 / 100.0) : 255;
            CardServer.SurfaceAlpha = cardAlpha;
            CardInfo.SurfaceAlpha = cardAlpha;
            CardNotice.SurfaceAlpha = cardAlpha;

            // 整窗模式：标题栏、侧栏做成玻璃，导航按钮跟着透明
            int wash = 255;
            if (hasImage)
            {
                wash = frosted ? 120 : 178;
            }
            ApplyChromeWash(wash);
            SidePanel.Wash = wash;
            Color surface = hasImage ? Color.Transparent : Theme.Surface;
            Start_Button.Surface = surface;
            Setting_Button.Surface = surface;
            button_ToggleTerminal.Surface = surface;
            ConsoleLogger.Surface = surface;
            SideFoot.BackColor = surface;

            ContentPanel.BackColor = (windowMode && hasImage) ? Color.Transparent : Theme.Bg;

            // 公告区是 RichTextBox（为了能选中/复制），只能是不透明底色：
            // 取它所在区域背景图的平均色，再按卡片透明度与卡片色合成，让接缝尽量看不出来
            Image backdropImage = windowMode ? windowWallpaper : ContentPanel.CurrentImage;
            Color noticeBack = Theme.Surface;
            if (hasImage && backdropImage != null)
            {
                Rectangle areaBounds;
                Rectangle noticeArea;
                if (windowMode)
                {
                    areaBounds = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                    noticeArea = new Rectangle(
                        ContentPanel.Left + CardNotice.Left + NoticeBox.Left,
                        ContentPanel.Top + CardNotice.Top + NoticeBox.Top,
                        NoticeBox.Width, NoticeBox.Height);
                }
                else
                {
                    areaBounds = new Rectangle(0, 0, ContentPanel.Width, ContentPanel.Height);
                    noticeArea = new Rectangle(
                        CardNotice.Left + NoticeBox.Left,
                        CardNotice.Top + NoticeBox.Top,
                        NoticeBox.Width, NoticeBox.Height);
                }

                Color average = Theme.AverageColor(backdropImage, areaBounds, noticeArea);
                if (average.A > 0)
                {
                    noticeBack = Theme.Blend(average, Theme.Surface, cardAlpha);
                }
            }
            NoticeBox.SetBackdrop(noticeBack);

            Invalidate(true);
            SidePanel.Invalidate(true);
        }

        /// <summary>
        /// 生成毛玻璃垫图：整窗模式按窗口尺寸，内容区模式按内容区尺寸，
        /// 再按各面板的位置裁出对应区域。关掉毛玻璃时清空。
        /// </summary>
        private bool ApplyFrosted(string path, bool windowMode, bool hasImage)
        {
            if (!ProfileService.SettingConfig.FrostedGlass || !hasImage || string.IsNullOrWhiteSpace(path))
            {
                TitleBar.Frosted = null;
                SidePanel.Frosted = null;
                CardServer.Frosted = null;
                CardInfo.Frosted = null;
                return false;
            }

            Image source = Theme.LoadImage(path);
            if (source == null)
            {
                return false;
            }

            bool ok = false;
            try
            {
                Size baseSize = windowMode ? ClientSize : ContentPanel.Size;
                using (Bitmap frostedBase = Theme.BuildFrosted(source, baseSize))
                {
                    if (frostedBase != null)
                    {
                        if (windowMode)
                        {
                            TitleBar.Frosted = Crop(frostedBase, new Rectangle(0, 0, ClientSize.Width, ChromeHeight));
                            SidePanel.Frosted = Crop(frostedBase, new Rectangle(SidePanel.Left, SidePanel.Top, SidePanel.Width, SidePanel.Height));
                            CardServer.Frosted = Crop(frostedBase, new Rectangle(
                                ContentPanel.Left + CardServer.Left, ContentPanel.Top + CardServer.Top, CardServer.Width, CardServer.Height));
                            CardInfo.Frosted = Crop(frostedBase, new Rectangle(
                                ContentPanel.Left + CardInfo.Left, ContentPanel.Top + CardInfo.Top, CardInfo.Width, CardInfo.Height));
                        }
                        else
                        {
                            TitleBar.Frosted = null;
                            SidePanel.Frosted = null;
                            CardServer.Frosted = Crop(frostedBase, CardServer.Bounds);
                            CardInfo.Frosted = Crop(frostedBase, CardInfo.Bounds);
                        }
                        ok = true;
                    }
                }
            }
            finally
            {
                source.Dispose();
            }
            return ok;
        }

        /// <summary>裁一块模糊底图；越界部分留透明，尺寸始终等于目标控件。</summary>
        private static Bitmap Crop(Bitmap source, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return null;
            }

            Rectangle safe = Rectangle.Intersect(rect, new Rectangle(0, 0, source.Width, source.Height));
            if (safe.Width <= 0 || safe.Height <= 0)
            {
                return null;
            }

            var result = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(result))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                g.DrawImage(source, new Rectangle(0, 0, rect.Width, rect.Height),
                    new Rectangle(safe.X, safe.Y, safe.Width, safe.Height), GraphicsUnit.Pixel);
            }
            return result;
        }

        private void SetWindowWallpaper(string path, int opacity)
        {
            windowWallpaperOpacity = opacity;
            if (windowWallpaper != null)
            {
                windowWallpaper.Dispose();
                windowWallpaper = null;
            }
            windowWallpaper = Theme.LoadImage(path);
            Invalidate(true);
        }

        /// <summary>按当前色板重新上色（深色模式切换时调用）。</summary>
        private void ApplyTheme()
        {
            BackColor = Theme.Bg;
            ThemeApply.Apply(this);

            // 卡片里的按钮不归导航那套逻辑管，这里单独跟随色板
            IPChange_Button.Surface = Theme.Surface;

            Invalidate(true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (windowWallpaper == null)
            {
                base.OnPaintBackground(e);
                return;
            }

            using (var brush = new SolidBrush(Theme.Bg))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
            Theme.DrawCover(e.Graphics, ClientRectangle, windowWallpaper, windowWallpaperOpacity);
        }

        /// <summary>拉取并刷新公告（网络在后台，回来后回到 UI 线程）。</summary>
        private async void RefreshNotice()
        {
            string source = ProfileService.SettingConfig.NoticeSource;
            if (string.IsNullOrWhiteSpace(source))
            {
                // 没配来源时给一份示例文件，免得公告区永远是空的
                NoticeService.EnsureTemplate();
            }
            string resolved = NoticeService.ResolveSource(source);

            if (string.IsNullOrWhiteSpace(resolved))
            {
                NoticeBox.SetItems(null);
                NoticeBox.EmptyText = "还没有公告，点右上角「编辑」设置来源";
                return;
            }

            NoticeBox.EmptyText = "正在读取公告…";
            NoticeBox.SetItems(null);

            bool ok = false;
            try
            {
                ok = await Task.Run(() => NoticeService.LoadAsync(source));
            }
            catch (Exception ex)
            {
                Log.Info($"[Notice] 读取异常: {ex.Message}");
            }

            if (IsDisposed || Disposing)
            {
                return;
            }

            var items = NoticeService.Parse(NoticeService.RawText);
            NoticeBox.SetItems(items);
            if (items.Count == 0)
            {
                NoticeBox.EmptyText = ok ? "暂无公告内容" : "公告读取失败，点右上角「编辑」检查来源";
            }
            else if (NoticeService.FromCache)
            {
                NoticeBox.EmptyText = string.Empty;
                StatusText.Text = "公告为离线缓存";
            }
        }

        private void NoticeEdit_Button_Click(object sender, EventArgs e)
        {
            using (var dialog = new NoticeSettingsDialog(ProfileService.SettingConfig.NoticeSource))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                ProfileService.SettingConfig.NoticeSource = dialog.NoticeSource;
                ProfileService.SaveSettings();
            }
            RefreshNotice();
        }

        /// <summary>左侧导航与内容区之间的 1px 分隔线。</summary>
        private void SidePanel_Paint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawLine(pen, SidePanel.Width - 1, 0, SidePanel.Width - 1, SidePanel.Height);
            }
        }

        private void Start_Button_Click(object sender, EventArgs e)
        {
            // if (Process.GetProcessesByName("KartRider").Length != 0)
            // {
            //     LauncherSystem.MessageBoxType1();
            // }
            // else
            // {
            var thread = new Thread(() =>
            {
                try
                {
                    LaunchGame();
                }
                catch (Exception ex)
                {
                    Log.Info($"启动游戏失败: {ex.Message}");
                }
            })
            {
                IsBackground = true,
                Name = "GameLauncherThread"
            };
            thread.Start();
            // }
        }

        /// <summary>
        /// 启动游戏的核心逻辑
        /// </summary>
        private void LaunchGame()
        {
            if (string.IsNullOrEmpty(pinFile))
            {
                Log.Info($"[PIN] 文件不存在，无法启动游戏");
                return;
            }

            var Game = new StartGame();
            Game.Start(kartRiderDirectory, pinFile, pinFileBak);
        }

        private void Setting_Button_Click(object sender, EventArgs e)
        {
            Program.SettingDlg = new Setting();
            Program.SettingDlg.ShowDialog(this);
            // 设置里可能换了服务器或背景图，回来后刷新
            RefreshServerInfo();
            ApplyTheme();
            RefreshBackground();
        }

        private void GitHub_Click(object sender, EventArgs e)
        {
            // 本项目自己的仓库主页（原版是 yanygm.github.io/Launcher_V2）
            string url = "https://github.com/cookieskuqi/KartRider-Launcher";
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Info($"错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 标签点击事件处理器（同步包装）
        /// </summary>
        private async void label_Client_Click(object sender, EventArgs e)
        {
            try
            {
                await label_Client_ClickAsync(sender, e);
            }
            catch (Exception ex)
            {
                Log.Info($"检查更新时出错: {ex.Message}");
                MessageBox.Show($"检查更新失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 异步执行检查更新逻辑
        /// </summary>
        private async Task label_Client_ClickAsync(object sender, EventArgs e)
        {
            var data = await global::KartRider.Update.GetUpdateAsync();
            if (data == null)
            {
                MessageBox.Show("获取游戏版本失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 弹出“是否”确认框
            DialogResult result = MessageBox.Show(
                $"当前版本为：P{ClientVersion.Text}\n最新版本为：{data.version}\n是否需要更新？",
                "确认操作",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            // 根据用户选择执行对应逻辑
            if (result == DialogResult.Yes)
            {
                RestorePinFile();
                LauncherSystem.CheckGame(kartRiderDirectory);
            }
        }

        private void button_ToggleTerminal_Click(object sender, EventArgs e)
        {
            Program.isVisible = !Program.isVisible;
            // 控制台默认不创建；第一次点"显示控制台"时才 AllocConsole
            IntPtr handle = Program.consoleHandle;
            if (Program.isVisible && handle == IntPtr.Zero)
            {
                handle = Program.EnsureConsole();
            }
            if (handle != IntPtr.Zero)
            {
                Program.ShowWindow(handle, Program.isVisible ? Program.SW_SHOW : Program.SW_HIDE);
            }
            button_ToggleTerminal.Text = Program.isVisible ? "隐藏控制台" : "显示控制台";
            ProfileService.SettingConfig.Console = Program.isVisible;
            ProfileService.SaveSettings();
        }

        private void ConsoleLogger_Click(object sender, EventArgs e)
        {
            CachedConsoleWriter.SaveToFile();
            CachedConsoleWriter.cachedWriter.ClearCache();
        }
    }
}