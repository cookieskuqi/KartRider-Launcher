using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using KartRider.Ui;

namespace KartRider
{
    partial class Launcher
    {
        private void InitializeComponent()
        {
            SidePanel = new GlassPanel();
            ContentPanel = new WallpaperPanel();
            Start_Button = new FlatButton();
            Setting_Button = new FlatButton();
            button_ToggleTerminal = new FlatButton();
            ConsoleLogger = new FlatButton();
            Verify_Button = new FlatButton();
            SideFoot = new Label();

            CardServer = new CardPanel();
            ServerCaption = new Label();
            ServerValue = new Label();
            IPChange_Button = new FlatButton();

            CardInfo = new CardPanel();
            InfoCaption = new Label();
            label_Client = new Label();
            ClientVersion = new Label();
            Launcher_label = new Label();
            VersionLabel = new Label();
            InfoHint = new Label();
            StatusText = new Label();

            CardNotice = new CardPanel();
            NoticeCaption = new Label();
            NoticeEdit_Button = new FlatButton();
            NoticeBox = new NoticeView();

            SuspendLayout();

            Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(720, 584);
            Font = Theme.F(9f);
            BackColor = Theme.Bg;
            MaximizeBox = false;
            Name = "Launcher";
            Text = "跑跑卡丁车 单机启动器";
            FormClosing += OnFormClosing;
            Load += OnLoad;

            // 自绘标题栏（无边框 + 拖动 + 圆角）
            BuildChrome("跑跑卡丁车 · 单机启动器", true);

            // ---------- 左侧导航 ----------
            SidePanel.BackColor = Theme.Surface;
            SidePanel.Location = new Point(0, ChromeHeight);
            SidePanel.Name = "SidePanel";
            SidePanel.Size = new Size(190, 540);
            SidePanel.Paint += SidePanel_Paint;

            Start_Button.Name = "Start_Button";
            Start_Button.Text = "启动游戏";
            Start_Button.Style = FlatButtonStyle.Primary;
            Start_Button.Surface = Theme.Surface;
            Start_Button.Font = Theme.F(11f, FontStyle.Bold);
            Start_Button.Radius = 9;
            Start_Button.Location = new Point(14, 20);
            Start_Button.Size = new Size(162, 46);
            Start_Button.TextAlign = ContentAlignment.MiddleLeft;
            Start_Button.Click += Start_Button_Click;

            Setting_Button.Name = "Setting_Button";
            Setting_Button.Text = "设置";
            Setting_Button.Style = FlatButtonStyle.Ghost;
            Setting_Button.Surface = Theme.Surface;
            Setting_Button.Radius = 9;
            Setting_Button.Location = new Point(14, 76);
            Setting_Button.Size = new Size(162, 42);
            Setting_Button.TextAlign = ContentAlignment.MiddleLeft;
            Setting_Button.Click += Setting_Button_Click;

            button_ToggleTerminal.Name = "button_ToggleTerminal";
            button_ToggleTerminal.Text = "显示控制台";
            button_ToggleTerminal.Style = FlatButtonStyle.Ghost;
            button_ToggleTerminal.Surface = Theme.Surface;
            button_ToggleTerminal.Radius = 9;
            button_ToggleTerminal.Location = new Point(14, 124);
            button_ToggleTerminal.Size = new Size(162, 42);
            button_ToggleTerminal.TextAlign = ContentAlignment.MiddleLeft;
            button_ToggleTerminal.Click += button_ToggleTerminal_Click;

            ConsoleLogger.Name = "ConsoleLogger";
            ConsoleLogger.Text = "导出日志";
            ConsoleLogger.Style = FlatButtonStyle.Ghost;
            ConsoleLogger.Surface = Theme.Surface;
            ConsoleLogger.Radius = 9;
            ConsoleLogger.Location = new Point(14, 172);
            ConsoleLogger.Size = new Size(162, 42);
            ConsoleLogger.TextAlign = ContentAlignment.MiddleLeft;
            ConsoleLogger.Click += ConsoleLogger_Click;

            SideFoot.AutoSize = true;
            SideFoot.Text = "单机版 · 仅供个人使用";
            SideFoot.Font = Theme.F(8f);
            SideFoot.ForeColor = Theme.TextSecondary;
            Verify_Button.Name = "Verify_Button";
            Verify_Button.Text = "校验游戏文件";
            Verify_Button.Style = FlatButtonStyle.Ghost;
            Verify_Button.Surface = Theme.Surface;
            Verify_Button.Radius = 9;
            Verify_Button.Location = new Point(14, 220);
            Verify_Button.Size = new Size(162, 42);
            Verify_Button.TextAlign = ContentAlignment.MiddleLeft;
            Verify_Button.Font = Theme.F(9.5f);
            Verify_Button.Click += Verify_Button_Click;

            SideFoot.Tag = Roles.Secondary;
            SideFoot.BackColor = Theme.Surface;
            SideFoot.Location = new Point(16, 502);

            SidePanel.Controls.Add(Start_Button);
            SidePanel.Controls.Add(Setting_Button);
            SidePanel.Controls.Add(button_ToggleTerminal);
            SidePanel.Controls.Add(ConsoleLogger);
            SidePanel.Controls.Add(Verify_Button);
            SidePanel.Controls.Add(SideFoot);

            // ---------- 右侧内容 ----------
            ContentPanel.BackColor = Theme.Bg;
            ContentPanel.Location = new Point(190, ChromeHeight);
            ContentPanel.Name = "ContentPanel";
            ContentPanel.Size = new Size(530, 540);

            CardServer.Name = "CardServer";
            CardServer.Canvas = Theme.Bg;
            CardServer.Location = new Point(24, 20);
            CardServer.Size = new Size(482, 116);

            ServerCaption.Text = "当前服务器";
            ServerCaption.AutoSize = true;
            ServerCaption.Font = Theme.F(9f);
            ServerCaption.ForeColor = Theme.TextSecondary;
            ServerCaption.Tag = Roles.Secondary;
            ServerCaption.BackColor = Theme.Surface;
            ServerCaption.Location = new Point(22, 18);

            ServerValue.AutoSize = true;
            ServerValue.Text = "127.0.0.1:39311";
            ServerValue.Font = Theme.F(15f, FontStyle.Bold);
            ServerValue.ForeColor = Theme.TextPrimary;
            ServerValue.BackColor = Theme.Surface;
            ServerValue.Location = new Point(22, 42);

            IPChange_Button.Name = "IPChange_Button";
            IPChange_Button.Text = "切换服务器";
            IPChange_Button.Style = FlatButtonStyle.Secondary;
            IPChange_Button.Surface = Theme.Surface;
            IPChange_Button.Radius = 8;
            IPChange_Button.Location = new Point(366, 44);
            IPChange_Button.Size = new Size(94, 34);
            IPChange_Button.Click += Setting_Button_Click;

            CardServer.Controls.Add(ServerCaption);
            CardServer.Controls.Add(ServerValue);
            CardServer.Controls.Add(IPChange_Button);

            CardInfo.Name = "CardInfo";
            CardInfo.Canvas = Theme.Bg;
            CardInfo.Location = new Point(24, 152);
            CardInfo.Size = new Size(482, 180);

            InfoCaption.Text = "版本信息";
            InfoCaption.AutoSize = true;
            InfoCaption.Font = Theme.F(9f);
            InfoCaption.ForeColor = Theme.TextSecondary;
            InfoCaption.Tag = Roles.Secondary;
            InfoCaption.BackColor = Theme.Surface;
            InfoCaption.Location = new Point(22, 18);

            label_Client.AutoSize = true;
            label_Client.Text = "客户端版本";
            label_Client.Font = Theme.F(9f);
            label_Client.ForeColor = Theme.TextSecondary;
            label_Client.Tag = Roles.Secondary;
            label_Client.BackColor = Theme.Surface;
            label_Client.Location = new Point(22, 50);
            label_Client.Click += label_Client_Click;

            ClientVersion.AutoSize = true;
            ClientVersion.Text = "0";
            ClientVersion.Font = Theme.F(12f, FontStyle.Bold);
            ClientVersion.Tag = Roles.Accent;
            ClientVersion.ForeColor = Theme.Accent;
            ClientVersion.BackColor = Theme.Surface;
            ClientVersion.Cursor = Cursors.Hand;
            ClientVersion.Location = new Point(112, 44);
            ClientVersion.Click += label_Client_Click;

            Launcher_label.AutoSize = true;
            Launcher_label.Text = "启动器版本";
            Launcher_label.Font = Theme.F(9f);
            Launcher_label.ForeColor = Theme.TextSecondary;
            Launcher_label.Tag = Roles.Secondary;
            Launcher_label.BackColor = Theme.Surface;
            Launcher_label.Location = new Point(22, 88);
            Launcher_label.Click += GitHub_Click;

            VersionLabel.AutoSize = true;
            VersionLabel.Text = "0";
            VersionLabel.Font = Theme.F(12f, FontStyle.Bold);
            VersionLabel.Tag = Roles.Accent;
            VersionLabel.ForeColor = Theme.Accent;
            VersionLabel.BackColor = Theme.Surface;
            VersionLabel.Cursor = Cursors.Hand;
            VersionLabel.Location = new Point(112, 82);
            VersionLabel.Click += GitHub_Click;

            InfoHint.AutoSize = true;
            InfoHint.Text = "点击客户端版本号可检查游戏更新 · 点击启动器版本号打开项目主页";
            InfoHint.Font = Theme.F(8.5f);
            InfoHint.ForeColor = Theme.TextSecondary;
            InfoHint.Tag = Roles.Secondary;
            InfoHint.BackColor = Theme.Surface;
            InfoHint.Location = new Point(22, 126);

            CardInfo.Controls.Add(InfoCaption);
            CardInfo.Controls.Add(label_Client);
            CardInfo.Controls.Add(ClientVersion);
            CardInfo.Controls.Add(Launcher_label);
            CardInfo.Controls.Add(VersionLabel);
            CardInfo.Controls.Add(InfoHint);

            CardNotice.Name = "CardNotice";
            CardNotice.Canvas = Theme.Bg;
            CardNotice.Location = new Point(24, 344);
            CardNotice.Size = new Size(482, 176);

            NoticeCaption.Text = "公告";
            NoticeCaption.AutoSize = true;
            NoticeCaption.Font = Theme.F(9f);
            NoticeCaption.ForeColor = Theme.TextSecondary;
            NoticeCaption.Tag = Roles.Secondary;
            NoticeCaption.BackColor = Theme.Surface;
            NoticeCaption.Location = new Point(22, 16);

            NoticeEdit_Button.Name = "NoticeEdit_Button";
            NoticeEdit_Button.Text = "编辑";
            NoticeEdit_Button.Style = FlatButtonStyle.Ghost;
            NoticeEdit_Button.Surface = Theme.Surface;
            NoticeEdit_Button.Radius = 6;
            NoticeEdit_Button.Font = Theme.F(8.5f);
            NoticeEdit_Button.Location = new Point(414, 8);
            NoticeEdit_Button.Size = new Size(48, 26);
            NoticeEdit_Button.Click += NoticeEdit_Button_Click;

            NoticeBox.Name = "NoticeBox";
            NoticeBox.Location = new Point(16, 44);
            NoticeBox.Size = new Size(450, 118);
            NoticeBox.EmptyText = "还没有公告，点右上角「编辑」设置来源";

            CardNotice.Controls.Add(NoticeCaption);
            CardNotice.Controls.Add(NoticeEdit_Button);
            CardNotice.Controls.Add(NoticeBox);

            StatusText.AutoSize = true;
            StatusText.Text = "就绪";
            StatusText.Font = Theme.F(8.5f);
            StatusText.ForeColor = Theme.TextSecondary;
            StatusText.Tag = Roles.Secondary;
            StatusText.BackColor = Theme.Bg;
            StatusText.Location = new Point(26, 524);

            ContentPanel.Controls.Add(CardServer);
            ContentPanel.Controls.Add(CardInfo);
            ContentPanel.Controls.Add(CardNotice);
            ContentPanel.Controls.Add(StatusText);

            Controls.Add(SidePanel);
            Controls.Add(ContentPanel);

            ResumeLayout(false);
            PerformLayout();
        }

        private GlassPanel SidePanel;
        private WallpaperPanel ContentPanel;
        private FlatButton Start_Button;
        private FlatButton Setting_Button;
        private FlatButton button_ToggleTerminal;
        private FlatButton ConsoleLogger;
        private FlatButton Verify_Button;
        private Label SideFoot;

        private CardPanel CardServer;
        private Label ServerCaption;
        private Label ServerValue;
        private FlatButton IPChange_Button;

        private CardPanel CardInfo;
        private Label InfoCaption;
        private Label label_Client;
        private Label ClientVersion;
        private Label Launcher_label;
        private Label VersionLabel;
        private Label InfoHint;

        private Label StatusText;

        private CardPanel CardNotice;
        private Label NoticeCaption;
        private FlatButton NoticeEdit_Button;
        private NoticeView NoticeBox;
    }
}
