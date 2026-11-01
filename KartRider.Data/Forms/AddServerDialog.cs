using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KartRider.Ui;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 新增 / 修改一条服务器地址：填 IP 和备注。
    /// 不做 .resx，控件全部在构造函数里创建。
    /// </summary>
    public class AddServerDialog : ChromeForm
    {
        private readonly FlatTextBox IpBox = new FlatTextBox();
        private readonly FlatTextBox NoteBox = new FlatTextBox();
        private readonly FlatButton OkBtn = new FlatButton();
        private readonly FlatButton CancelBtn = new FlatButton();
        private readonly List<ServerEntry> existing;

        /// <summary>用户填写的 IP（已 Trim）。</summary>
        public string ServerIP
        {
            get { return IpBox.Text.Trim(); }
        }

        /// <summary>用户填写的备注，可为空。</summary>
        public string ServerNote
        {
            get { return NoteBox.Text.Trim(); }
        }

        public AddServerDialog(string presetIp, string presetNote, IEnumerable<ServerEntry> existingEntries)
        {
            existing = existingEntries == null ? new List<ServerEntry>() : existingEntries.ToList();
            BuildUi();

            IpBox.Text = presetIp ?? string.Empty;
            NoteBox.Text = presetNote ?? string.Empty;
            IpBox.SelectAll();
        }

        private void BuildUi()
        {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.None;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(380, 240);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.F(9f);

            Icon = Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            BuildChrome("添加服务器", false);

            var card = new CardPanel
            {
                Name = "CardEntry",
                Canvas = Theme.Bg,
                Location = new Point(20, 58),
                Size = new Size(340, 104)
            };

            card.Controls.Add(MakeLabel("IP", new Point(20, 23)));
            card.Controls.Add(MakeLabel("备注", new Point(20, 63)));

            IpBox.Name = "IpBox";
            IpBox.Font = Theme.F(9.5f);
            IpBox.Surface = Theme.Surface;
            IpBox.MaxLength = 64;
            IpBox.Location = new Point(70, 16);
            IpBox.Size = new Size(250, 30);
            card.Controls.Add(IpBox);

            NoteBox.Name = "NoteBox";
            NoteBox.Font = Theme.F(9.5f);
            NoteBox.Surface = Theme.Surface;
            NoteBox.MaxLength = 24;
            NoteBox.Location = new Point(70, 56);
            NoteBox.Size = new Size(250, 30);
            card.Controls.Add(NoteBox);

            var hint = new Label
            {
                Text = "备注可留空，列表里会显示成「备注 (IP)」",
                AutoSize = true,
                Font = Theme.F(8.5f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.Bg,
                Location = new Point(24, 174)
            };

            CancelBtn.Name = "CancelBtn";
            CancelBtn.Text = "取消";
            CancelBtn.Style = FlatButtonStyle.Secondary;
            CancelBtn.Surface = Theme.Bg;
            CancelBtn.Radius = 8;
            CancelBtn.Font = Theme.F(9.5f);
            CancelBtn.Location = new Point(184, 194);
            CancelBtn.Size = new Size(84, 34);
            CancelBtn.DialogResult = DialogResult.Cancel;

            OkBtn.Name = "OkBtn";
            OkBtn.Text = "确定";
            OkBtn.Style = FlatButtonStyle.Primary;
            OkBtn.Surface = Theme.Bg;
            OkBtn.Radius = 8;
            OkBtn.Font = Theme.F(9.5f, FontStyle.Bold);
            OkBtn.Location = new Point(276, 194);
            OkBtn.Size = new Size(84, 34);
            OkBtn.Click += OkBtn_Click;

            AcceptButton = OkBtn;
            CancelButton = CancelBtn;

            Controls.Add(card);
            Controls.Add(hint);
            Controls.Add(CancelBtn);
            Controls.Add(OkBtn);
        }

        private static Label MakeLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = Theme.F(9.5f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Location = location
            };
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            string ip = ServerIP;
            if (!ServerAddress.IsValidIp(ip))
            {
                MessageBox.Show(this,
                    "服务器地址格式不正确。\r\n请输入 IPv4（如 192.168.1.2）或 IPv6（如 2001:db8::1）。",
                    "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                IpBox.Focus();
                IpBox.SelectAll();
                return;
            }

            ServerEntry dup = existing.FirstOrDefault(x => string.Equals(x.IP, ip, StringComparison.OrdinalIgnoreCase));
            if (dup != null && string.IsNullOrWhiteSpace(ServerNote))
            {
                // 已在列表里且这次没填备注：直接沿用原来那条，避免把旧备注清空
                NoteBox.Text = dup.Note ?? string.Empty;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
