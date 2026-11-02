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

        // Windows 11-like palette: window < card < input, with an indigo accent taken from the icon.
        public static Color Back => IsDark ? Color.FromArgb(28, 28, 28) : Color.FromArgb(243, 243, 243);
        public static Color Card => IsDark ? Color.FromArgb(43, 43, 43) : Color.White;
        public static Color CardBorder => IsDark ? Color.FromArgb(56, 56, 56) : Color.FromArgb(229, 229, 229);
        public static Color Input => IsDark ? Color.FromArgb(52, 52, 52) : Color.FromArgb(251, 251, 251);
        public static Color InputHover => IsDark ? Color.FromArgb(62, 62, 62) : Color.FromArgb(243, 243, 243);
        public static Color InputPressed => IsDark ? Color.FromArgb(46, 46, 46) : Color.FromArgb(235, 235, 235);
        public static Color InputBorder => IsDark ? Color.FromArgb(75, 75, 75) : Color.FromArgb(208, 208, 208);
        public static Color Surface => Input;
        public static Color Text => IsDark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(27, 27, 27);
        public static Color SubtleText => IsDark ? Color.FromArgb(160, 160, 160) : Color.FromArgb(110, 110, 110);
        public static Color DisabledText => IsDark ? Color.FromArgb(105, 105, 105) : Color.FromArgb(170, 170, 170);
        public static Color ErrorText => IsDark ? Color.FromArgb(255, 110, 110) : Color.Firebrick;
        public static Color Border => InputBorder;
        public static Color Accent => IsDark ? Color.FromArgb(139, 128, 255) : Color.FromArgb(99, 91, 230);
        public static Color AccentFill => IsDark ? Color.FromArgb(101, 88, 222) : Color.FromArgb(99, 91, 230);
        public static Color Track => IsDark ? Color.FromArgb(90, 90, 90) : Color.FromArgb(200, 200, 200);
        public static Color ThumbOuter => IsDark ? Color.FromArgb(69, 69, 69) : Color.White;
        public static Color Link => IsDark ? Color.FromArgb(150, 140, 255) : Color.FromArgb(79, 70, 229);
        public static Color HeaderBack => Input;
        public static Color Selection => IsDark ? Color.FromArgb(62, 58, 100) : Color.FromArgb(226, 224, 252);

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
                case VibranceGoggles.UI.Card card:
                    card.BackColor = Card;
                    card.ForeColor = Text;
                    card.Invalidate();
                    break;
                case RoundFrame frame:
                    frame.BackColor = Input;
                    frame.Invalidate();
                    break;
                case RoundButton _:
                case ModernSlider _:
                    c.Invalidate();
                    break;
                case LinkLabel link:
                    link.LinkColor = link.ActiveLinkColor = link.VisitedLinkColor = Link;
                    break;
                case Label label:
                    label.ForeColor = Equals(label.Tag, Subtle) ? SubtleText : Equals(label.Tag, Error) ? ErrorText : Text;
                    break;

                case ComboBox combo:
                    combo.FlatStyle = IsDark ? FlatStyle.Flat : FlatStyle.Standard;
                    combo.BackColor = Surface;
                    combo.ForeColor = Text;
                    break;
                case TextBoxBase _:
                case NumericUpDown _:
                case ListBox _:
                    c.BackColor = Input;
                    c.ForeColor = Text;
                    break;
                case ListView list:
                    list.BackColor = Surface;
                    list.ForeColor = Text;
                    if (list.IsHandleCreated) SetWindowTheme(list.Handle, IsDark ? "DarkMode_Explorer" : "Explorer", null);
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
