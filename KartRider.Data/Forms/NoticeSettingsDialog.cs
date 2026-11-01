using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using KartRider.Ui;
using Profile;

namespace KartRider
{
    /// <summary>
    /// 公告来源设置：填一个网址，或选一个本地 txt 文件。留空则使用 Profile\notice.txt（若存在）。
    /// </summary>
    public class NoticeSettingsDialog : ChromeForm
    {
        private readonly FlatTextBox SourceBox = new FlatTextBox();
        private readonly FlatButton PickBtn = new FlatButton();
        private readonly FlatButton SaveBtn = new FlatButton();
        private readonly FlatButton CancelBtn = new FlatButton();

        /// <summary>确定后的来源（可能为空）。</summary>
        public string NoticeSource
        {
            get { return SourceBox.Text.Trim(); }
        }

        public NoticeSettingsDialog(string current)
        {
            ShowInTaskbar = false;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(520, 268);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Bg;
            Font = Theme.F(9f);

            Icon = Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            BuildChrome("公告设置", false);

            var card = new CardPanel
            {
                Canvas = Theme.Bg,
                Location = new Point(20, 60),
                Size = new Size(480, 128)
            };

            card.Controls.Add(new Label
            {
                Text = "公告来源",
                AutoSize = true,
                Font = Theme.F(9.5f),
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.Surface,
                Location = new Point(20, 26)
            });

            SourceBox.Name = "SourceBox";
            SourceBox.Font = Theme.F(9.5f);
            SourceBox.Surface = Theme.Surface;
            SourceBox.Location = new Point(106, 21);
            SourceBox.Size = new Size(280, 30);
            SourceBox.Text = current ?? string.Empty;
            card.Controls.Add(SourceBox);

            PickBtn.Name = "PickBtn";
            PickBtn.Text = "选择文件";
            PickBtn.Style = FlatButtonStyle.Secondary;
            PickBtn.Surface = Theme.Surface;
            PickBtn.Radius = 6;
            PickBtn.Font = Theme.F(9f);
            PickBtn.Location = new Point(394, 21);
            PickBtn.Size = new Size(68, 30);
            PickBtn.Click += PickBtn_Click;
            card.Controls.Add(PickBtn);

            card.Controls.Add(new Label
            {
                Text = "支持 http(s) 网址或本地 txt。每条公告用单独一行 --- 分隔，第一行是标题（可带日期），其余是正文。",
                AutoSize = false,
                Size = new Size(438, 34),
                Font = Theme.F(8.5f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.Surface,
                Location = new Point(20, 62)
            });

            card.Controls.Add(new Label
            {
                Text = "留空则读取 Profile\\notice.txt（存在时）。",
                AutoSize = true,
                Font = Theme.F(8.5f),
                ForeColor = Theme.TextSecondary,
                BackColor = Theme.Surface,
                Location = new Point(20, 98)
            });

            CancelBtn.Name = "CancelBtn";
            CancelBtn.Text = "取消";
            CancelBtn.Style = FlatButtonStyle.Secondary;
            CancelBtn.Surface = Theme.Bg;
            CancelBtn.Radius = 8;
            CancelBtn.Font = Theme.F(9.5f);
            CancelBtn.Location = new Point(324, 210);
            CancelBtn.Size = new Size(84, 34);
            CancelBtn.DialogResult = DialogResult.Cancel;

            SaveBtn.Name = "SaveBtn";
            SaveBtn.Text = "保存";
            SaveBtn.Style = FlatButtonStyle.Primary;
            SaveBtn.Surface = Theme.Bg;
            SaveBtn.Radius = 8;
            SaveBtn.Font = Theme.F(9.5f, FontStyle.Bold);
            SaveBtn.Location = new Point(416, 210);
            SaveBtn.Size = new Size(84, 34);
            SaveBtn.Click += SaveBtn_Click;

            AcceptButton = SaveBtn;
            CancelButton = CancelBtn;

            Controls.Add(card);
            Controls.Add(CancelBtn);
            Controls.Add(SaveBtn);
        }

        private void PickBtn_Click(object sender, EventArgs e)
        {
            // 与主设置一致：延后到消息循环再弹系统对话框
            BeginInvoke((Action)(() =>
            {
                try
                {
                    using (var dialog = new OpenFileDialog())
                    {
                        dialog.Title = "选择公告文件";
                        dialog.Filter = "文本文件|*.txt;*.md|所有文件|*.*";
                        dialog.CheckFileExists = true;
                        dialog.RestoreDirectory = false;
                        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        dialog.InitialDirectory = Directory.Exists(pictures) ? pictures : AppDomain.CurrentDomain.BaseDirectory;

                        if (dialog.ShowDialog(this) == DialogResult.OK)
                        {
                            SourceBox.Text = dialog.FileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "选择文件时出错：" + ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }));
        }

        private void SaveBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
