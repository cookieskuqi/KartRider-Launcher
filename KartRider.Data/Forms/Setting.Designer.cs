using System.Drawing;
using System.Windows.Forms;
using KartRider.Ui;

namespace KartRider
{
    partial class Setting
    {
        private void InitializeComponent()
        {
            CardAccount = new CardPanel();
            CardServer = new CardPanel();
            CardGame = new CardPanel();

            Name_label = new Label();
            PlayerName = new FlatTextBox();

            IP_label = new Label();
            ServerIP = new FlatComboBox();
            IPAdd_Button = new FlatButton();
            IPDel_Button = new FlatButton();
            Port_label = new Label();
            ServerPort = new FlatTextBox();

            Version_label = new Label();
            Version_comboBox = new FlatComboBox();
            Speed_label = new Label();
            Speed_comboBox = new FlatComboBox();
            AiSpeed_label = new Label();
            AiSpeed_comboBox = new FlatComboBox();
            Proxy_label = new Label();
            Proxy_comboBox = new FlatComboBox();

            NgsOn = new FlatCheckBox();
            SoloRank = new FlatCheckBox();
            EnableMod = new FlatCheckBox();
            AutoUpdate = new FlatCheckBox();

            Done_Button = new FlatButton();
            SaveHint = new Label();

            CardAppearance = new CardPanel();
            BackgroundName = new Label();
            BgPick_Button = new FlatButton();
            BgClear_Button = new FlatButton();
            BgMode_Combo = new FlatComboBox();
            BgOpacity_Slider = new FlatSlider();
            BgCard_Slider = new FlatSlider();
            BgOpacity_Value = new Label();
            BgCard_Value = new Label();
            DarkModeCheck = new FlatCheckBox();
            FrostedCheck = new FlatCheckBox();

            SuspendLayout();

            var textFont = Theme.F(9.5f);

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(520, 818);
            Font = Theme.F(9f);
            BackColor = Theme.Bg;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Setting";
            StartPosition = FormStartPosition.CenterParent;
            Text = "设置";
            FormClosing += OnFormClosing;
            Load += OnLoad;

            Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            BuildChrome("设置", false);

            // ---------- 账号 ----------
            CardAccount.Name = "CardAccount";
            CardAccount.Canvas = Theme.Bg;
            CardAccount.Location = new Point(24, 56);
            CardAccount.Size = new Size(472, 84);

            CardAccount.Controls.Add(SectionTitle("账号", new Point(22, 16)));

            Name_label.Text = "昵称";
            Name_label.AutoSize = true;
            Name_label.Font = Theme.F(9.5f);
            Name_label.ForeColor = Theme.TextPrimary;
            Name_label.BackColor = Theme.Surface;
            Name_label.Location = new Point(22, 48);
            CardAccount.Controls.Add(Name_label);

            PlayerName.Name = "PlayerName";
            PlayerName.Font = textFont;
            PlayerName.Surface = Theme.Surface;
            PlayerName.Location = new Point(96, 42);
            PlayerName.Size = new Size(340, 30);
            PlayerName.TabIndex = 1;
            CardAccount.Controls.Add(PlayerName);

            // ---------- 服务器 ----------
            CardServer.Name = "CardServer";
            CardServer.Canvas = Theme.Bg;
            CardServer.Location = new Point(24, 152);
            CardServer.Size = new Size(472, 132);

            CardServer.Controls.Add(SectionTitle("服务器", new Point(22, 16)));

            IP_label.Text = "IP";
            IP_label.AutoSize = true;
            IP_label.Font = Theme.F(9.5f);
            IP_label.ForeColor = Theme.TextPrimary;
            IP_label.BackColor = Theme.Surface;
            IP_label.Location = new Point(22, 48);
            CardServer.Controls.Add(IP_label);

            ServerIP.Name = "ServerIP";
            ServerIP.Font = textFont;
            ServerIP.Surface = Theme.Surface;
            ServerIP.Location = new Point(96, 42);
            ServerIP.Size = new Size(230, 30);
            ServerIP.TabIndex = 2;
            ServerIP.Text = "127.0.0.1";
            CardServer.Controls.Add(ServerIP);

            IPAdd_Button.Name = "IPAdd_Button";
            IPAdd_Button.Text = "添加";
            IPAdd_Button.Style = FlatButtonStyle.Secondary;
            IPAdd_Button.Surface = Theme.Surface;
            IPAdd_Button.Radius = 6;
            IPAdd_Button.Font = Theme.F(9f);
            IPAdd_Button.Location = new Point(334, 42);
            IPAdd_Button.Size = new Size(52, 29);
            IPAdd_Button.TabIndex = 12;
            IPAdd_Button.Click += IPAdd_Button_Click;
            CardServer.Controls.Add(IPAdd_Button);

            IPDel_Button.Name = "IPDel_Button";
            IPDel_Button.Text = "删除";
            IPDel_Button.Style = FlatButtonStyle.Secondary;
            IPDel_Button.Surface = Theme.Surface;
            IPDel_Button.Radius = 6;
            IPDel_Button.Font = Theme.F(9f);
            IPDel_Button.Location = new Point(392, 42);
            IPDel_Button.Size = new Size(52, 29);
            IPDel_Button.TabIndex = 13;
            IPDel_Button.Click += IPDel_Button_Click;
            CardServer.Controls.Add(IPDel_Button);

            Port_label.Text = "端口";
            Port_label.AutoSize = true;
            Port_label.Font = Theme.F(9.5f);
            Port_label.ForeColor = Theme.TextPrimary;
            Port_label.BackColor = Theme.Surface;
            Port_label.Location = new Point(22, 90);
            CardServer.Controls.Add(Port_label);

            ServerPort.Name = "ServerPort";
            ServerPort.Font = textFont;
            ServerPort.Surface = Theme.Surface;
            ServerPort.Location = new Point(96, 84);
            ServerPort.Size = new Size(120, 30);
            ServerPort.TabIndex = 3;
            ServerPort.Text = "39311";
            CardServer.Controls.Add(ServerPort);

            var portHint = new Label
            {
                Text = "默认 39311，一般不用改",
                AutoSize = true,
                Font = Theme.F(8.5f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.Surface,
                Tag = Roles.Secondary,
                Location = new Point(226, 92)
            };
            CardServer.Controls.Add(portHint);

            // ---------- 游戏 ----------
            CardGame.Name = "CardGame";
            CardGame.Canvas = Theme.Bg;
            CardGame.Location = new Point(24, 296);
            CardGame.Size = new Size(472, 200);

            CardGame.Controls.Add(SectionTitle("游戏", new Point(22, 16)));

            Version_label.Text = "版本";
            Version_label.AutoSize = true;
            Version_label.Font = Theme.F(9.5f);
            Version_label.ForeColor = Theme.TextPrimary;
            Version_label.BackColor = Theme.Surface;
            Version_label.Location = new Point(22, 50);
            CardGame.Controls.Add(Version_label);

            Version_comboBox.Name = "Version_comboBox";
            Version_comboBox.Font = textFont;
            Version_comboBox.Surface = Theme.Surface;
            Version_comboBox.Location = new Point(96, 44);
            Version_comboBox.Size = new Size(150, 30);
            Version_comboBox.TabIndex = 4;
            CardGame.Controls.Add(Version_comboBox);

            Speed_label.Text = "速度";
            Speed_label.AutoSize = true;
            Speed_label.Font = Theme.F(9.5f);
            Speed_label.ForeColor = Theme.TextPrimary;
            Speed_label.BackColor = Theme.Surface;
            Speed_label.Location = new Point(22, 86);
            CardGame.Controls.Add(Speed_label);

            Speed_comboBox.Name = "Speed_comboBox";
            Speed_comboBox.Font = textFont;
            Speed_comboBox.Surface = Theme.Surface;
            Speed_comboBox.Location = new Point(96, 80);
            Speed_comboBox.Size = new Size(150, 30);
            Speed_comboBox.TabIndex = 5;
            CardGame.Controls.Add(Speed_comboBox);

            AiSpeed_label.Text = "Ai 速度";
            AiSpeed_label.AutoSize = true;
            AiSpeed_label.Font = Theme.F(9.5f);
            AiSpeed_label.ForeColor = Theme.TextPrimary;
            AiSpeed_label.BackColor = Theme.Surface;
            AiSpeed_label.Location = new Point(22, 122);
            CardGame.Controls.Add(AiSpeed_label);

            AiSpeed_comboBox.Name = "AiSpeed_comboBox";
            AiSpeed_comboBox.Font = textFont;
            AiSpeed_comboBox.Surface = Theme.Surface;
            AiSpeed_comboBox.Location = new Point(96, 116);
            AiSpeed_comboBox.Size = new Size(150, 30);
            AiSpeed_comboBox.TabIndex = 6;
            CardGame.Controls.Add(AiSpeed_comboBox);

            Proxy_label.Text = "代理";
            Proxy_label.AutoSize = true;
            Proxy_label.Font = Theme.F(9.5f);
            Proxy_label.ForeColor = Theme.TextPrimary;
            Proxy_label.BackColor = Theme.Surface;
            Proxy_label.Location = new Point(22, 158);
            CardGame.Controls.Add(Proxy_label);

            Proxy_comboBox.Name = "Proxy_comboBox";
            Proxy_comboBox.Font = textFont;
            Proxy_comboBox.Surface = Theme.Surface;
            Proxy_comboBox.Location = new Point(96, 152);
            Proxy_comboBox.Size = new Size(172, 30);
            Proxy_comboBox.TabIndex = 7;
            CardGame.Controls.Add(Proxy_comboBox);

            NgsOn.Name = "NgsOn";
            NgsOn.Text = "NgsOn";
            NgsOn.Font = Theme.F(9.5f);
            NgsOn.Location = new Point(290, 48);
            NgsOn.Size = new Size(86, 22);
            NgsOn.TabIndex = 8;
            CardGame.Controls.Add(NgsOn);

            SoloRank.Name = "SoloRank";
            SoloRank.Text = "单人排位";
            SoloRank.Font = Theme.F(9.5f);
            SoloRank.Location = new Point(290, 84);
            SoloRank.Size = new Size(86, 22);
            SoloRank.TabIndex = 9;
            CardGame.Controls.Add(SoloRank);

            EnableMod.Name = "EnableMod";
            EnableMod.Text = "启用 MOD";
            EnableMod.Font = Theme.F(9.5f);
            EnableMod.Location = new Point(290, 120);
            EnableMod.Size = new Size(106, 22);
            EnableMod.TabIndex = 10;
            CardGame.Controls.Add(EnableMod);

            AutoUpdate.Name = "AutoUpdate";
            AutoUpdate.Text = "自动更新游戏";
            AutoUpdate.Font = Theme.F(9.5f);
            AutoUpdate.Location = new Point(290, 156);
            AutoUpdate.Size = new Size(118, 22);
            AutoUpdate.TabIndex = 11;
            CardGame.Controls.Add(AutoUpdate);

            // ---------- 外观 ----------
            CardAppearance = new CardPanel();
            CardAppearance.Name = "CardAppearance";
            CardAppearance.Canvas = Theme.Bg;
            CardAppearance.Location = new Point(24, 508);
            CardAppearance.Size = new Size(472, 240);
            CardAppearance.Controls.Add(SectionTitle("外观", new Point(22, 16)));

            var bgLabel = new Label
            {
                Text = "背景图",
                AutoSize = true,
                Font = Theme.F(9.5f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Location = new Point(22, 50)
            };
            CardAppearance.Controls.Add(bgLabel);

            BackgroundName.Tag = Roles.Secondary;
            BackgroundName.AutoSize = false;
            BackgroundName.Text = "未设置（默认浅灰底）";
            BackgroundName.Font = Theme.F(9f);
            BackgroundName.ForeColor = Theme.TextSecondary;
            BackgroundName.BackColor = Theme.Surface;
            BackgroundName.AutoEllipsis = true;
            BackgroundName.Size = new Size(218, 20);
            BackgroundName.Location = new Point(96, 54);
            CardAppearance.Controls.Add(BackgroundName);

            BgPick_Button.Name = "BgPick_Button";
            BgPick_Button.Text = "选择图片";
            BgPick_Button.Style = FlatButtonStyle.Secondary;
            BgPick_Button.Surface = Theme.Surface;
            BgPick_Button.Radius = 6;
            BgPick_Button.Font = Theme.F(9f);
            BgPick_Button.Location = new Point(322, 40);
            BgPick_Button.Size = new Size(62, 30);
            BgPick_Button.TabIndex = 15;
            BgPick_Button.Click += BgPick_Button_Click;
            CardAppearance.Controls.Add(BgPick_Button);

            BgClear_Button.Name = "BgClear_Button";
            BgClear_Button.Text = "清除";
            BgClear_Button.Style = FlatButtonStyle.Secondary;
            BgClear_Button.Surface = Theme.Surface;
            BgClear_Button.Radius = 6;
            BgClear_Button.Font = Theme.F(9f);
            BgClear_Button.Location = new Point(388, 40);
            BgClear_Button.Size = new Size(56, 30);
            BgClear_Button.TabIndex = 16;
            BgClear_Button.Click += BgClear_Button_Click;
            CardAppearance.Controls.Add(BgClear_Button);

            var modeLabel = new Label
            {
                Text = "显示范围",
                AutoSize = true,
                Font = Theme.F(9.5f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Location = new Point(22, 90)
            };
            CardAppearance.Controls.Add(modeLabel);

            BgMode_Combo.Name = "BgMode_Combo";
            BgMode_Combo.Font = textFont;
            BgMode_Combo.Surface = Theme.Surface;
            BgMode_Combo.Location = new Point(96, 85);
            BgMode_Combo.Size = new Size(170, 30);
            BgMode_Combo.TabIndex = 17;
            BgMode_Combo.SelectedIndexChanged += BgMode_Combo_SelectedIndexChanged;
            CardAppearance.Controls.Add(BgMode_Combo);

            var opacityLabel = new Label
            {
                Text = "图片透明度",
                AutoSize = true,
                Font = Theme.F(9.5f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Location = new Point(22, 134)
            };
            CardAppearance.Controls.Add(opacityLabel);

            BgOpacity_Slider.Name = "BgOpacity_Slider";
            BgOpacity_Slider.Surface = Theme.Surface;
            BgOpacity_Slider.Minimum = 0;
            BgOpacity_Slider.Maximum = 100;
            BgOpacity_Slider.Value = 100;
            BgOpacity_Slider.Location = new Point(96, 128);
            BgOpacity_Slider.Size = new Size(190, 26);
            BgOpacity_Slider.TabIndex = 18;
            BgOpacity_Slider.ValueChanged += BgOpacity_Slider_ValueChanged;
            CardAppearance.Controls.Add(BgOpacity_Slider);

            BgOpacity_Value.Tag = Roles.Secondary;
            BgOpacity_Value.AutoSize = true;
            BgOpacity_Value.Text = "100%";
            BgOpacity_Value.Font = Theme.F(9f);
            BgOpacity_Value.ForeColor = Theme.TextSecondary;
            BgOpacity_Value.BackColor = Theme.Surface;
            BgOpacity_Value.Location = new Point(296, 132);
            CardAppearance.Controls.Add(BgOpacity_Value);

            var cardOpacityLabel = new Label
            {
                Text = "卡片透明度",
                AutoSize = true,
                Font = Theme.F(9.5f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Location = new Point(22, 172)
            };
            CardAppearance.Controls.Add(cardOpacityLabel);

            BgCard_Slider.Name = "BgCard_Slider";
            BgCard_Slider.Surface = Theme.Surface;
            BgCard_Slider.Minimum = 0;
            BgCard_Slider.Maximum = 100;
            BgCard_Slider.Value = 84;
            BgCard_Slider.Location = new Point(96, 166);
            BgCard_Slider.Size = new Size(190, 26);
            BgCard_Slider.TabIndex = 21;
            BgCard_Slider.ValueChanged += BgCard_Slider_ValueChanged;
            CardAppearance.Controls.Add(BgCard_Slider);

            BgCard_Value.Tag = Roles.Secondary;
            BgCard_Value.AutoSize = true;
            BgCard_Value.Text = "84%";
            BgCard_Value.Font = Theme.F(9f);
            BgCard_Value.ForeColor = Theme.TextSecondary;
            BgCard_Value.BackColor = Theme.Surface;
            BgCard_Value.Location = new Point(296, 170);
            CardAppearance.Controls.Add(BgCard_Value);

            DarkModeCheck.Name = "DarkModeCheck";
            DarkModeCheck.Text = "深色模式";
            DarkModeCheck.Font = Theme.F(9.5f);
            DarkModeCheck.Location = new Point(96, 208);
            DarkModeCheck.Size = new Size(96, 22);
            DarkModeCheck.TabIndex = 19;
            DarkModeCheck.CheckedChanged += DarkModeCheck_CheckedChanged;
            CardAppearance.Controls.Add(DarkModeCheck);

            FrostedCheck.Name = "FrostedCheck";
            FrostedCheck.Text = "毛玻璃效果";
            FrostedCheck.Font = Theme.F(9.5f);
            FrostedCheck.Location = new Point(212, 208);
            FrostedCheck.Size = new Size(110, 22);
            FrostedCheck.TabIndex = 20;
            FrostedCheck.CheckedChanged += FrostedCheck_CheckedChanged;
            CardAppearance.Controls.Add(FrostedCheck);

            // ---------- 底部 ----------
            SaveHint.Tag = Roles.Secondary;
            SaveHint.AutoSize = true;
            SaveHint.Text = "改动会即时保存，关掉窗口即生效";
            SaveHint.Font = Theme.F(8.5f);
            SaveHint.ForeColor = Theme.TextSecondary;
            SaveHint.BackColor = Theme.Bg;
            SaveHint.Location = new Point(28, 772);

            Done_Button.Name = "Done_Button";
            Done_Button.Text = "完成";
            Done_Button.Style = FlatButtonStyle.Primary;
            Done_Button.Surface = Theme.Bg;
            Done_Button.Radius = 8;
            Done_Button.Font = Theme.F(10f, FontStyle.Bold);
            Done_Button.Location = new Point(404, 760);
            Done_Button.Size = new Size(92, 38);
            Done_Button.TabIndex = 14;
            Done_Button.Click += (s, e) => Close();

            Controls.Add(CardAccount);
            Controls.Add(CardServer);
            Controls.Add(CardGame);
            Controls.Add(CardAppearance);
            Controls.Add(SaveHint);
            Controls.Add(Done_Button);

            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>卡片里的小节标题，统一样式。</summary>
        private static Label SectionTitle(string text, Point location)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = Theme.F(10f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Location = location
            };
        }

        private CardPanel CardAccount;
        private CardPanel CardServer;
        private CardPanel CardGame;
        private CardPanel CardAppearance;
        private Label BackgroundName;
        private FlatButton BgPick_Button;
        private FlatButton BgClear_Button;
        private FlatComboBox BgMode_Combo;
        private FlatSlider BgOpacity_Slider;
        private FlatSlider BgCard_Slider;
        private Label BgOpacity_Value;
        private Label BgCard_Value;
        private FlatCheckBox DarkModeCheck;
        private FlatCheckBox FrostedCheck;

        private Label Name_label;
        private FlatTextBox PlayerName;

        private Label IP_label;
        private FlatComboBox ServerIP;
        private FlatButton IPAdd_Button;
        private FlatButton IPDel_Button;
        private Label Port_label;
        private FlatTextBox ServerPort;

        private Label Version_label;
        private FlatComboBox Version_comboBox;
        private Label Speed_label;
        private FlatComboBox Speed_comboBox;
        private Label AiSpeed_label;
        private FlatComboBox AiSpeed_comboBox;
        private Label Proxy_label;
        private FlatComboBox Proxy_comboBox;

        private FlatCheckBox NgsOn;
        private FlatCheckBox SoloRank;
        private FlatCheckBox EnableMod;
        private FlatCheckBox AutoUpdate;

        private FlatButton Done_Button;
        private Label SaveHint;
    }
}
