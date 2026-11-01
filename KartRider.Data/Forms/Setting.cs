using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExcData;
using Profile;
using KartRider.Logging;
using KartRider.Ui;

namespace KartRider
{
    public partial class Setting : ChromeForm
    {
        public string[] AiSpeed = new string[] { "简单", "困难", "地狱" };

        public string[] urls = new string[] { "https://v6.gh-proxy.org/", "https://gh-proxy.com/", "https://cdn.gh-proxy.org/", "https://ghproxy.net/", "https://git.myany.uk/", "" };

        /// <summary>已保存服务器地址的上限，防止 Settings.json 无限膨胀。</summary>
        private const int MaxSavedServers = 20;

        /// <summary>为 true 时表示正在程序内部回填控件值，此时要忽略控件事件，避免误触发重连。</summary>
        private bool suppressEvents;

        /// <summary>后台重启监听的进行中标志，避免连点下拉框时叠出多轮启停。</summary>
        private bool restarting;

        /// <summary>本次打开设置期间地址发生过变化：关闭窗口时按需补一次版本检查。</summary>
        private bool patchCheckPending;

        /// <summary>拖动不透明度滑块时防抖，避免每一格都写一次配置文件。</summary>
        private readonly Timer bgSaveTimer = new Timer();

        public Setting()
        {
            InitializeComponent();
            bgSaveTimer.Interval = 250;
            bgSaveTimer.Tick += BgSaveTimer_Tick;

            // 除了"选择图片"，也支持把图片直接拖到窗口上（不依赖系统文件对话框）。
            // 注册失败（例如线程不是 STA）只影响拖拽，不该把整个程序带崩。
            try
            {
                AllowDrop = true;
                DragEnter += Setting_DragEnter;
                DragDrop += Setting_DragDrop;
            }
            catch (Exception ex)
            {
                Log.Info($"[Setting] 拖拽注册失败（不影响其他功能）: {ex.Message}");
            }
        }

        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

        private static bool IsImageFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            string ext = System.IO.Path.GetExtension(path);
            return Array.Exists(ImageExtensions, x => string.Equals(x, ext, StringComparison.OrdinalIgnoreCase));
        }

        private void Setting_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null && Array.Exists(files, IsImageFile))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Setting_DragDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0)
            {
                return;
            }
            string file = Array.Find(files, IsImageFile);
            if (string.IsNullOrEmpty(file))
            {
                return;
            }

            ProfileService.SettingConfig.LauncherBackground = file;
            ProfileService.SaveSettings();
            ApplyTheme();
            RefreshAppearance();
            Log.Info($"[Setting] 拖入背景图: {file}");
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            ProfileService.SettingConfig.Name = PlayerName.Text;
            ProfileService.SettingConfig.NgsOn = NgsOn.Checked;
            ProfileService.SettingConfig.SoloRank = SoloRank.Checked;
            ProfileService.SettingConfig.EnableMod = EnableMod.Checked;
            ProfileService.SettingConfig.AutoUpdate = AutoUpdate.Checked;

            // IP / 端口放在最后：校验通过才写入，真的变了才重启监听
            ApplyEndpoint(restartIfChanged: true, showInvalidMessage: false);
            ProfileService.SaveSettings();

            // 版本检查会弹「与服务器版本不一致，是否更新」的模态框，
            // 放在切换地址的瞬间弹会和当前这个模态设置窗打架（表现为整个启动器未响应），
            // 所以统一挪到关窗口时执行。
            if (patchCheckPending)
            {
                patchCheckPending = false;
                PatchManager.StartUpdateAsync(Program.LauncherDlg.kartRiderDirectory);
            }
        }

        /// <summary>
        /// 把界面上的 IP / 端口写进配置；地址有变化时按原有分支重启本地监听。
        /// 「选中下拉框」「手输回车」「添加后选中」「关窗口」几条路径都走这里，保证行为一致。
        /// </summary>
        private void ApplyEndpoint(bool restartIfChanged, bool showInvalidMessage)
        {
            if (suppressEvents)
            {
                return;
            }

            ushort port;
            if (!ushort.TryParse((ServerPort.Text ?? string.Empty).Trim(), out port) || port == 0)
            {
                if (showInvalidMessage)
                {
                    MessageBox.Show(this, "端口必须是 1~65535 之间的数字。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            string ip = CurrentIp();
            if (!ServerAddress.IsValidIp(ip))
            {
                if (showInvalidMessage)
                {
                    MessageBox.Show(this,
                        "服务器地址格式不正确。\r\n请输入 IPv4（如 192.168.1.2）或 IPv6（如 2001:db8::1）。",
                        "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // 回到当前生效值，避免界面停在一个非法值上
                    suppressEvents = true;
                    ServerIP.Text = ProfileService.SettingConfig.ServerIP;
                    suppressEvents = false;
                }
                return;
            }

            bool changed = !string.Equals(ip, ProfileService.SettingConfig.ServerIP, StringComparison.OrdinalIgnoreCase)
                || port != ProfileService.SettingConfig.ServerPort;

            ProfileService.SettingConfig.ServerIP = ip;
            ProfileService.SettingConfig.ServerPort = port;
            RememberServer(ip, null);
            ProfileService.SaveSettings();

            if (changed)
            {
                patchCheckPending = true;
                if (restartIfChanged)
                {
                    RequestRestart();
                }
            }
        }

        /// <summary>取当前要用的 IP：优先下拉里选中的那条，其次是手输的文本。</summary>
        private string CurrentIp()
        {
            ServerEntry selected = ServerIP.SelectedItem as ServerEntry;
            string typed = (ServerIP.Text ?? string.Empty).Trim();
            if (selected != null)
            {
                // 文本仍和选中项对得上，说明没被手改过
                if (typed.Length == 0
                    || string.Equals(typed, selected.ToString(), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(typed, selected.IP, StringComparison.OrdinalIgnoreCase))
                {
                    return selected.IP;
                }
            }
            return typed;
        }

        /// <summary>排队重启：已经在重启就只记下来，结束后再用最新配置跑一轮。</summary>
        private void RequestRestart()
        {
            if (restarting)
            {
                return;
            }
            StartRestartWorker();
        }

        /// <summary>
        /// 在后台线程重启本地监听。
        /// 原实现把 Stop/Start 直接放在控件的 Leave/SelectedIndexChanged 里同步执行，
        /// 一旦里面有任何阻塞（绑定端口、拉公网 IP 等），整个设置窗和启动器都会变成「未响应」。
        /// </summary>
        private void StartRestartWorker()
        {
            restarting = true;
            string appliedIp = ProfileService.SettingConfig.ServerIP;
            ushort appliedPort = ProfileService.SettingConfig.ServerPort;

            Task.Run(() =>
            {
                string error = null;
                try
                {
                    // 先停掉本地服务端释放端口，避免与 ClientStart 端口冲突
                    RouterListener.Stop();
                    if (LanIpGetter.IsIPv6(appliedIp))
                    {
                        TinyMapper.ClientStart();
                    }
                    else
                    {
                        RouterListener.Start();
                    }
                    Log.Info($"[Setting] 已切换服务器: {appliedIp}:{appliedPort}");
                }
                catch (SocketException)
                {
                    error = $"端口 {appliedPort} 被占用，本地服务端没能启动。\r\n关掉占用该端口的程序后再切换一次。";
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    Log.Info($"[Setting] 切换服务器后重启监听失败: {ex.Message}");
                }

                try
                {
                    BeginInvoke((Action)(() => AfterRestart(appliedIp, appliedPort, error)));
                }
                catch (InvalidOperationException)
                {
                    // 窗口已关（含 ObjectDisposedException）：没人接这一棒了，直接复位
                    restarting = false;
                }
            });
        }

        private void AfterRestart(string appliedIp, ushort appliedPort, string error)
        {
            restarting = false;

            if (!string.IsNullOrEmpty(error) && !IsDisposed && !Disposing)
            {
                MessageBox.Show(this, error, "切换服务器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // 重启期间又换了地址：用最新配置再跑一轮
            if (!IsDisposed && !Disposing
                && (!string.Equals(appliedIp, ProfileService.SettingConfig.ServerIP, StringComparison.OrdinalIgnoreCase)
                    || appliedPort != ProfileService.SettingConfig.ServerPort))
            {
                StartRestartWorker();
            }
        }

        /// <summary>把地址放进历史列表：去重后插到最前面，超出上限就丢掉最旧的。</summary>
        private static void RememberServer(string ip, string note)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                return;
            }
            ip = ip.Trim();

            List<ServerEntry> list = ProfileService.SettingConfig.SavedServers;
            if (list == null)
            {
                list = new List<ServerEntry>();
                ProfileService.SettingConfig.SavedServers = list;
            }

            ServerEntry existing = list.FirstOrDefault(x => string.Equals(x.IP, ip, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                list.Remove(existing);
                // note 为空表示这次没填备注，保留原来的
                if (!string.IsNullOrWhiteSpace(note))
                {
                    existing.Note = note.Trim();
                }
                list.Insert(0, existing);
            }
            else
            {
                list.Insert(0, new ServerEntry { IP = ip, Note = (note ?? string.Empty).Trim() });
            }

            if (list.Count > MaxSavedServers)
            {
                list.RemoveRange(MaxSavedServers, list.Count - MaxSavedServers);
            }
        }

        /// <summary>
        /// 清理列表里的空项和重复 IP（手改 Settings.json 时可能出现），保持原有顺序。
        /// 打开设置窗口时跑一次，保证下拉框里不会出现两条一样的地址。
        /// </summary>
        private static void NormalizeServers()
        {
            List<ServerEntry> list = ProfileService.SettingConfig.SavedServers;
            if (list == null)
            {
                ProfileService.SettingConfig.SavedServers = new List<ServerEntry>();
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cleaned = new List<ServerEntry>(list.Count);
            foreach (ServerEntry entry in list)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.IP))
                {
                    continue;
                }
                entry.IP = entry.IP.Trim();
                entry.Note = (entry.Note ?? string.Empty).Trim();
                if (seen.Add(entry.IP))
                {
                    cleaned.Add(entry);
                }
            }

            if (cleaned.Count > MaxSavedServers)
            {
                cleaned.RemoveRange(MaxSavedServers, cleaned.Count - MaxSavedServers);
            }
            ProfileService.SettingConfig.SavedServers = cleaned;
        }

        /// <summary>按下拉列表重建下拉项，并把选中项定位到 selectIp 那条。</summary>
        private void ReloadServerItems(string selectIp)
        {
            suppressEvents = true;
            try
            {
                ServerIP.Items.Clear();
                List<ServerEntry> list = ProfileService.SettingConfig.SavedServers ?? new List<ServerEntry>();
                foreach (ServerEntry entry in list)
                {
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.IP))
                    {
                        ServerIP.Items.Add(entry);
                    }
                }

                ServerEntry target = list.FirstOrDefault(x => string.Equals(x.IP, selectIp, StringComparison.OrdinalIgnoreCase));
                ServerIP.Text = target != null ? target.ToString() : (selectIp ?? string.Empty);
            }
            finally
            {
                suppressEvents = false;
            }
        }

        private void ServerIP_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressEvents || ServerIP.SelectedItem == null)
            {
                return;
            }
            // 选中历史地址：立即生效（写配置 + 后台重启监听）
            ApplyEndpoint(restartIfChanged: true, showInvalidMessage: false);
        }

        private void ServerIP_KeyDown(object sender, KeyEventArgs e)
        {
            // 注意：KartRider.Keys 会遮蔽 System.Windows.Forms.Keys，这里必须写全名
            if (e.KeyCode != System.Windows.Forms.Keys.Return)
            {
                return;
            }
            // 回车即保存：手输的地址也能进历史列表
            e.SuppressKeyPress = true;
            e.Handled = true;
            ApplyEndpoint(restartIfChanged: true, showInvalidMessage: true);
            ReloadServerItems(ProfileService.SettingConfig.ServerIP);
        }

        private void ServerIP_Leave(object sender, EventArgs e)
        {
            // 输入框失焦：手输的地址同样立即生效（只有真的变了才会重连）
            ApplyEndpoint(restartIfChanged: true, showInvalidMessage: false);
            ReloadServerItems(ProfileService.SettingConfig.ServerIP);
        }

        /// <summary>添加一条服务器地址（IP + 备注）。</summary>
        private void IPAdd_Button_Click(object sender, EventArgs e)
        {
            string presetIp = CurrentIp();
            if (!ServerAddress.IsValidIp(presetIp))
            {
                presetIp = ProfileService.SettingConfig.ServerIP;
            }
            ServerEntry selected = ServerIP.SelectedItem as ServerEntry;

            using (var dialog = new AddServerDialog(
                presetIp ?? string.Empty,
                selected == null ? string.Empty : selected.Note,
                ProfileService.SettingConfig.SavedServers))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                RememberServer(dialog.ServerIP, dialog.ServerNote);
                ProfileService.SaveSettings();
                ReloadServerItems(dialog.ServerIP);
                Log.Info($"[Setting] 已保存服务器: {dialog.ServerIP}（{(string.IsNullOrWhiteSpace(dialog.ServerNote) ? "无备注" : dialog.ServerNote)}）");
                // 添加完直接切过去
                ApplyEndpoint(restartIfChanged: true, showInvalidMessage: false);
            }
        }

        /// <summary>从历史列表里删掉选中的那条（不会改动当前连接）。</summary>
        private void IPDel_Button_Click(object sender, EventArgs e)
        {
            ServerEntry entry = ServerIP.SelectedItem as ServerEntry;
            if (entry == null)
            {
                MessageBox.Show(this, "请先在列表里选择要删除的地址。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            List<ServerEntry> list = ProfileService.SettingConfig.SavedServers;
            if (list == null || !list.Any(x => string.Equals(x.IP, entry.IP, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(this, $"{entry.IP} 不在已保存的地址列表里。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            list.RemoveAll(x => string.Equals(x.IP, entry.IP, StringComparison.OrdinalIgnoreCase));
            ProfileService.SaveSettings();
            ReloadServerItems(ProfileService.SettingConfig.ServerIP);
            Log.Info($"[Setting] 已从服务器列表移除: {entry.IP}");
        }

        /// <summary>选一张图片作为启动器内容区背景。</summary>
        private void BgPick_Button_Click(object sender, EventArgs e)
        {
            // 不要在手势事件里直接弹系统文件对话框：按钮的鼠标处理还没结束就进模态循环，
            // 两者容易互相等待（表现就是"点了没反应 / 卡死"）。排到消息队列里再弹。
            Log.Info("[Setting] 点击了「选择图片」");
            try
            {
                BeginInvoke((Action)PickBackgroundImage);
            }
            catch (InvalidOperationException)
            {
                // 窗口还没创建句柄时无法 BeginInvoke，直接调用即可
                PickBackgroundImage();
            }
        }

        private void PickBackgroundImage()
        {
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = "选择启动器背景图";
                    dialog.Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*";
                    dialog.CheckFileExists = true;
                    // 不要让它恢复"上次用过的目录"：如果那是个已断开的网络位置，
                    // 资源管理器会在枚举时卡很久，表现就是点了按钮像死机。
                    dialog.RestoreDirectory = false;

                    string startFolder = null;
                    string current = ProfileService.SettingConfig.LauncherBackground;
                    if (!string.IsNullOrWhiteSpace(current) && System.IO.File.Exists(current))
                    {
                        string folder = System.IO.Path.GetDirectoryName(current);
                        if (!string.IsNullOrWhiteSpace(folder) && System.IO.Directory.Exists(folder))
                        {
                            startFolder = folder;
                        }
                    }
                    if (string.IsNullOrEmpty(startFolder))
                    {
                        // 默认落到本机图片目录，没有就用启动器所在目录
                        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                        startFolder = System.IO.Directory.Exists(pictures) ? pictures : AppDomain.CurrentDomain.BaseDirectory;
                    }

                    dialog.InitialDirectory = startFolder;
                    if (!string.IsNullOrWhiteSpace(current))
                    {
                        dialog.FileName = System.IO.Path.GetFileName(current);
                    }

                    Log.Info("[Setting] 打开文件对话框，起始目录=" + startFolder);
                    DialogResult picked = dialog.ShowDialog(this);
                    Log.Info("[Setting] 文件对话框返回: " + picked);
                    if (picked != DialogResult.OK)
                    {
                        return;
                    }

                    ProfileService.SettingConfig.LauncherBackground = dialog.FileName;
                    ProfileService.SaveSettings();
                    ApplyTheme();
                    RefreshAppearance();
                    Log.Info($"[Setting] 已设置背景图: {dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                Log.Info($"[Setting] 选择背景图失败: {ex}");
                MessageBox.Show(this, "选择图片时出错：" + ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>清除背景图，恢复默认浅灰底。</summary>
        private void BgClear_Button_Click(object sender, EventArgs e)
        {
            ProfileService.SettingConfig.LauncherBackground = string.Empty;
            ProfileService.SaveSettings();
            ApplyTheme();
            RefreshAppearance();
            Log.Info("[Setting] 已清除背景图");
        }

        /// <summary>把外观相关的配置回填到控件上。</summary>
        private void RefreshAppearance()
        {
            string path = ProfileService.SettingConfig.LauncherBackground;
            bool hasImage = !string.IsNullOrWhiteSpace(path);
            BackgroundName.Text = hasImage ? System.IO.Path.GetFileName(path) : "未设置（默认浅灰底）";
            BackgroundName.ForeColor = hasImage ? Theme.TextPrimary : Theme.TextSecondary;

            suppressEvents = true;
            try
            {
                if (BgMode_Combo.Items.Count == 0)
                {
                    BgMode_Combo.Items.Add("右侧内容区");
                    BgMode_Combo.Items.Add("铺满整个窗口");
                }
                bool windowMode = string.Equals(
                    ProfileService.SettingConfig.LauncherBackgroundMode, "window", StringComparison.OrdinalIgnoreCase);
                BgMode_Combo.SelectedIndex = windowMode ? 1 : 0;

                int opacity = Math.Max(0, Math.Min(100, ProfileService.SettingConfig.LauncherBackgroundOpacity));
                BgOpacity_Slider.Value = opacity;
                BgOpacity_Value.Text = opacity + "%";

                int cardOpacity = Math.Max(0, Math.Min(100, ProfileService.SettingConfig.LauncherCardOpacity));
                BgCard_Slider.Value = cardOpacity;
                BgCard_Value.Text = cardOpacity + "%";

                DarkModeCheck.Checked = ProfileService.SettingConfig.DarkMode;
                FrostedCheck.Checked = ProfileService.SettingConfig.FrostedGlass;
            }
            finally
            {
                suppressEvents = false;
            }
        }

        private void BgMode_Combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (suppressEvents)
            {
                return;
            }
            ProfileService.SettingConfig.LauncherBackgroundMode = BgMode_Combo.SelectedIndex == 1 ? "window" : "content";
            ProfileService.SaveSettings();
        }

        private void BgOpacity_Slider_ValueChanged(object sender, EventArgs e)
        {
            BgOpacity_Value.Text = BgOpacity_Slider.Value + "%";
            if (suppressEvents)
            {
                return;
            }
            // 拖拽时会连续触发，停下来 250ms 再落盘
            bgSaveTimer.Stop();
            bgSaveTimer.Start();
        }

        private void BgCard_Slider_ValueChanged(object sender, EventArgs e)
        {
            BgCard_Value.Text = BgCard_Slider.Value + "%";
            if (suppressEvents)
            {
                return;
            }
            bgSaveTimer.Stop();
            bgSaveTimer.Start();
        }

        private void BgSaveTimer_Tick(object sender, EventArgs e)
        {
            bgSaveTimer.Stop();
            ProfileService.SettingConfig.LauncherBackgroundOpacity = BgOpacity_Slider.Value;
            ProfileService.SettingConfig.LauncherCardOpacity = BgCard_Slider.Value;
            ProfileService.SaveSettings();
        }

        private void DarkModeCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressEvents)
            {
                return;
            }
            ProfileService.SettingConfig.DarkMode = DarkModeCheck.Checked;
            ProfileService.SaveSettings();
            // 就地换肤，不用关窗口就能看到效果
            Theme.Apply(DarkModeCheck.Checked);
            ApplyTheme();
        }

        private void FrostedCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressEvents)
            {
                return;
            }
            ProfileService.SettingConfig.FrostedGlass = FrostedCheck.Checked;
            ProfileService.SaveSettings();
        }

        /// <summary>按当前色板重新上色。</summary>
        private void ApplyTheme()
        {
            BackColor = Theme.Bg;
            ThemeApply.Apply(this);

            CardAccount.Canvas = Theme.Bg;
            CardServer.Canvas = Theme.Bg;
            CardGame.Canvas = Theme.Bg;
            CardAppearance.Canvas = Theme.Bg;

            SaveHint.BackColor = Color.Transparent;
            Done_Button.Surface = Theme.Bg;
            IPAdd_Button.Surface = Theme.Surface;
            IPDel_Button.Surface = Theme.Surface;
            BgPick_Button.Surface = Theme.Surface;
            BgClear_Button.Surface = Theme.Surface;
            BgMode_Combo.Surface = Theme.Surface;
            BgOpacity_Slider.Surface = Theme.Surface;
            BgCard_Slider.Surface = Theme.Surface;

            ApplyChromeWash(255);

            // 逐个控件强制重绘，否则切主题后会留下旧配色的残影
            RepaintAll(this);
        }

        private static void RepaintAll(Control root)
        {
            root.Invalidate(true);
            foreach (Control child in root.Controls)
            {
                RepaintAll(child);
            }
            root.Update();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            ProfileService.LoadSettings();
            PlayerName.Text = ProfileService.SettingConfig.Name;
            ServerIP.Text = ProfileService.SettingConfig.ServerIP;
            ServerPort.Text = ProfileService.SettingConfig.ServerPort.ToString();
            NgsOn.Checked = ProfileService.SettingConfig.NgsOn;
            SoloRank.Checked = ProfileService.SettingConfig.SoloRank;
            EnableMod.Checked = ProfileService.SettingConfig.EnableMod;
            AutoUpdate.Checked = ProfileService.SettingConfig.AutoUpdate;
            foreach (string key in SpeedType.speedNames[ProfileService.SettingConfig.Version].Keys)
            {
                Speed_comboBox.Items.Add(key);
            }
            foreach (string key in SpeedType.speedNames.Keys)
            {
                Version_comboBox.Items.Add(key);
            }
            foreach (string key in AiSpeed)
            {
                AiSpeed_comboBox.Items.Add(key);
            }
            foreach (string key in urls)
            {
                Proxy_comboBox.Items.Add(key);
            }
            Speed_comboBox.Text = SpeedType.speedNames[ProfileService.SettingConfig.Version].FirstOrDefault(x => x.Value == ProfileService.SettingConfig.SpeedType).Key;
            Speed_comboBox.SelectedIndexChanged += Speed_comboBox_SelectedIndexChanged;
            Version_comboBox.Text = ProfileService.SettingConfig.Version;
            Version_comboBox.SelectedIndexChanged += Version_comboBox_SelectedIndexChanged;
            AiSpeed_comboBox.Text = ProfileService.SettingConfig.AiSpeedType;
            AiSpeed_comboBox.SelectedIndexChanged += AiSpeed_comboBox_SelectedIndexChanged;
            Proxy_comboBox.Text = ProfileService.SettingConfig.Proxy;
            Proxy_comboBox.SelectedIndexChanged += Proxy_comboBox_SelectedIndexChanged;

            // IP 下拉框：先把当前生效地址记进列表（保证第一次打开就有内容、能直接编辑备注），
            // 再填充下拉项，最后才挂事件——避免加载阶段就触发重连
            NormalizeServers();
            RememberServer(ProfileService.SettingConfig.ServerIP, null);
            ProfileService.SaveSettings();
            ReloadServerItems(ProfileService.SettingConfig.ServerIP);
            ApplyTheme();
            RefreshAppearance();
            ServerIP.SelectedIndexChanged += ServerIP_SelectedIndexChanged;
            ServerIP.KeyDown += ServerIP_KeyDown;
            ServerIP.Leave += ServerIP_Leave;
        }

        private void Version_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Version_comboBox.SelectedItem != null)
            {
                string selectedVersion = Version_comboBox.SelectedItem.ToString();
                if (SpeedType.speedNames.ContainsKey(selectedVersion))
                {
                    ProfileService.SettingConfig.Version = selectedVersion;
                    Speed_comboBox.Items.Clear();
                    foreach (string key in SpeedType.speedNames[selectedVersion].Keys)
                    {
                        Speed_comboBox.Items.Add(key);
                    }
                    if (Speed_comboBox.Items.Count > 0)
                    {
                        Speed_comboBox.SelectedIndex = 0;
                        ProfileService.SettingConfig.SpeedType = SpeedType.speedNames[selectedVersion][Speed_comboBox.Text];
                    }
                    ProfileService.SaveSettings();
                }
                else
                {
                    Log.Info($"Invalid speed type selected.");
                }
            }
        }

        private void Speed_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Speed_comboBox.SelectedItem != null)
            {
                string selectedSpeed = Speed_comboBox.SelectedItem.ToString();
                if (SpeedType.speedNames[ProfileService.SettingConfig.Version].ContainsKey(selectedSpeed))
                {
                    ProfileService.SettingConfig.SpeedType = SpeedType.speedNames[ProfileService.SettingConfig.Version][selectedSpeed];
                    ProfileService.SaveSettings();
                }
                else
                {
                    Log.Info($"Invalid speed type selected.");
                }
            }
        }

        private void AiSpeed_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AiSpeed_comboBox.SelectedItem != null)
            {
                string selectedAiSpeed = AiSpeed_comboBox.SelectedItem.ToString();
                if (AiSpeed.Contains(selectedAiSpeed))
                {
                    ProfileService.SettingConfig.AiSpeedType = selectedAiSpeed;
                    ProfileService.SaveSettings();
                }
                else
                {
                    Log.Info($"Invalid AiSpeed type selected.");
                }
            }
        }

        private void Proxy_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Proxy_comboBox.SelectedItem != null)
            {
                string selectedProxy = Proxy_comboBox.SelectedItem.ToString();
                if (urls.Contains(selectedProxy))
                {
                    ProfileService.SettingConfig.Proxy = selectedProxy;
                    ProfileService.SaveSettings();
                }
                else
                {
                    Log.Info($"Invalid Proxy type selected.");
                }
            }
        }
    }
}
