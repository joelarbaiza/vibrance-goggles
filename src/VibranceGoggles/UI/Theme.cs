using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VibranceGoggles.UI
{
    /// <summary>
    /// Light / dark colors that follow the Windows "app mode" setting
    /// (Settings → Personalization → Colors). Applied recursively to a form.
    /// </summary>
    internal static class Theme
    {
        public const string Subtle = "subtle"; // Control.Tag marker: secondary text.
        public const string Error = "error";   // Control.Tag marker: warning text.

        public static bool IsDark { get; private set; } = ReadIsDark();

        public static Color Back => IsDark ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
        public static Color Surface => IsDark ? Color.FromArgb(45, 45, 45) : SystemColors.Window;
        public static Color Text => IsDark ? Color.FromArgb(240, 240, 240) : SystemColors.ControlText;
        public static Color SubtleText => IsDark ? Color.FromArgb(160, 160, 160) : SystemColors.GrayText;
        public static Color ErrorText => IsDark ? Color.FromArgb(255, 110, 110) : Color.Firebrick;
        public static Color Border => IsDark ? Color.FromArgb(70, 70, 70) : SystemColors.ControlDark;
        public static Color Link => IsDark ? Color.FromArgb(110, 180, 255) : Color.FromArgb(0, 102, 204);
        public static Color HeaderBack => IsDark ? Color.FromArgb(38, 38, 38) : SystemColors.Control;
        public static Color RowHover => IsDark ? Color.FromArgb(58, 58, 58) : Color.FromArgb(232, 240, 250);
        public static Color RowSelected => IsDark ? Color.FromArgb(0, 84, 153) : Color.FromArgb(204, 228, 247);

        /// <summary>Re-reads the Windows setting; returns true if it changed.</summary>
        public static bool Refresh()
        {
            bool dark = ReadIsDark();
            if (dark == IsDark) return false;
            IsDark = dark;
            return true;
        }

        private static bool ReadIsDark()
        {
            // For screenshots/testing: VIBRANCE_GOGGLES_THEME=dark|light overrides Windows.
            string forced = Environment.GetEnvironmentVariable("VIBRANCE_GOGGLES_THEME");
            if (forced == "dark") return true;
            if (forced == "light") return false;

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                           @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
            }
            catch
            {
                return false;
            }
        }

        public static void Apply(Form form)
        {
            form.BackColor = Back;
            form.ForeColor = Text;
            ApplyTitleBar(form);
            foreach (Control c in form.Controls) ApplyTree(c);
            form.Invalidate(true);
        }

        public static void ApplyTitleBar(Form form)
        {
            if (!form.IsHandleCreated) return;
            int on = IsDark ? 1 : 0;
            // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 10 20H1+ / 11); 19 on older Windows 10 builds.
            if (DwmSetWindowAttribute(form.Handle, 20, ref on, sizeof(int)) != 0)
                DwmSetWindowAttribute(form.Handle, 19, ref on, sizeof(int));
        }

        private static void ApplyTree(Control c)
        {
            Style(c);
            foreach (Control child in c.Controls) ApplyTree(child);
        }

        public static void Style(Control c)
        {
            switch (c)
            {
                case LinkLabel link:
                    link.LinkColor = link.ActiveLinkColor = link.VisitedLinkColor = Link;
                    break;
                case Label label:
                    label.ForeColor = Equals(label.Tag, Subtle) ? SubtleText : Equals(label.Tag, Error) ? ErrorText : Text;
                    break;
                case Button button:
                    if (IsDark)
                    {
                        button.FlatStyle = FlatStyle.Flat;
                        button.BackColor = Surface;
                        button.ForeColor = Text;
                        button.FlatAppearance.BorderColor = Border;
                        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
                        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(75, 75, 75);
                    }
                    else
                    {
                        button.FlatStyle = FlatStyle.Standard;
                        button.UseVisualStyleBackColor = true;
                        button.ForeColor = SystemColors.ControlText;
                    }
                    break;
                case ComboBox combo:
                    combo.FlatStyle = IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                    combo.BackColor = Surface;
                    combo.ForeColor = Text;
                    break;
                case TextBoxBase _:
                case NumericUpDown _:
                    c.BackColor = Surface;
                    c.ForeColor = Text;
                    break;
                case ListView list:
                    list.BackColor = Surface;
                    list.ForeColor = Text;
                    if (list.IsHandleCreated) SetWindowTheme(list.Handle, IsDark ? "DarkMode_Explorer" : "Explorer", null);
                    break;
                case TrackBar track:
                    track.BackColor = Back;
                    break;
                case GroupBox group:
                    group.ForeColor = Text;
                    break;
                case CheckBox check:
                    check.ForeColor = Text;
                    break;
            }
        }

        /// <summary>Colors for the tray menu.</summary>
        public static ToolStripRenderer MenuRenderer() =>
            IsDark ? new ToolStripProfessionalRenderer(new DarkMenuColors()) : (ToolStripRenderer)new ToolStripProfessionalRenderer();

        private sealed class DarkMenuColors : ProfessionalColorTable
        {
            private static readonly Color Bg = Color.FromArgb(43, 43, 43);
            private static readonly Color Hover = Color.FromArgb(65, 65, 65);
            public override Color ToolStripDropDownBackground => Bg;
            public override Color ImageMarginGradientBegin => Bg;
            public override Color ImageMarginGradientMiddle => Bg;
            public override Color ImageMarginGradientEnd => Bg;
            public override Color MenuBorder => Color.FromArgb(70, 70, 70);
            public override Color MenuItemBorder => Hover;
            public override Color MenuItemSelected => Hover;
            public override Color SeparatorDark => Color.FromArgb(70, 70, 70);
            public override Color SeparatorLight => Bg;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string appName, string idList);
    }
}
