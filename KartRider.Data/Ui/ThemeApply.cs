using System.Drawing;
using System.Windows.Forms;

namespace KartRider.Ui
{
    /// <summary>控件在配色里的角色，写在 Control.Tag 上，换肤时据此上色。</summary>
    public static class Roles
    {
        public const string Secondary = "secondary";
        public const string Accent = "accent";
    }

    /// <summary>
    /// 换肤：遍历控件树，把自绘控件刷新成当前色板，并统一处理标签颜色。
    /// 自绘控件都是"绘制时读 Theme"，这里只需要清掉缓存的颜色并重绘。
    /// </summary>
    public static class ThemeApply
    {
        public static void Apply(Control root)
        {
            if (root == null)
            {
                return;
            }

            foreach (Control control in Walk(root))
            {
                switch (control)
                {
                    case Label label:
                        // 标签统一透明底，颜色由角色决定（父容器负责绘制背景）
                        label.BackColor = Color.Transparent;
                        label.ForeColor = ColorFor(label.Tag as string);
                        break;
                    case CheckBox check:
                        check.BackColor = Color.Transparent;
                        check.ForeColor = Theme.TextPrimary;
                        break;
                    case FlatCheckBox flatCheck:
                        flatCheck.BackColor = Color.Transparent;
                        flatCheck.ForeColor = Theme.TextPrimary;
                        flatCheck.Invalidate();
                        break;
                    case FlatTextBox box:
                        box.BackColor = Theme.Field;
                        box.RefreshTheme();
                        break;
                    case FlatComboBox combo:
                        combo.RefreshTheme();
                        break;
                    case FlatSlider slider:
                        slider.Surface = Theme.Surface;
                        slider.Invalidate();
                        break;
                    case NoticeView notice:
                        notice.RefreshTheme();
                        break;
                    case FlatButton button:
                        button.Invalidate();
                        break;
                    case CardPanel card:
                        card.Invalidate();
                        break;
                    case GlassPanel glass:
                        glass.Invalidate();
                        break;
                    case WallpaperPanel wall:
                        wall.Fallback = Theme.Bg;
                        wall.Invalidate();
                        break;
                }
            }
        }

        private static Color ColorFor(string role)
        {
            if (role == Roles.Secondary)
            {
                return Theme.TextSecondary;
            }
            if (role == Roles.Accent)
            {
                return Theme.Accent;
            }
            return Theme.TextPrimary;
        }

        private static System.Collections.Generic.IEnumerable<Control> Walk(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (Control grand in Walk(child))
                {
                    yield return grand;
                }
            }
        }
    }
}
