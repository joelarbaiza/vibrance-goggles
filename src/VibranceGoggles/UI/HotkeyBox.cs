using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VibranceGoggles.UI
{
    /// <summary>Text box that records a key combination instead of text.</summary>
    internal sealed class HotkeyBox : TextBox
    {
        private Keys _hotkey;

        public event EventHandler HotkeyChanged;

        public HotkeyBox()
        {
            ReadOnly = true;
            ShortcutsEnabled = false;
            BackColor = SystemColors.Window;
            Cursor = Cursors.Hand;
            TextAlign = HorizontalAlignment.Center;
        }

        public Keys Hotkey
        {
            get => _hotkey;
            set { _hotkey = value; Text = Describe(value); }
        }

        protected override bool IsInputKey(Keys keyData) => true; // Capture Tab, arrows, etc.

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.SuppressKeyPress = true;
            e.Handled = true;

            Keys key = e.KeyCode;
            if (key == Keys.Back || key == Keys.Delete || key == Keys.Escape)
            {
                SetHotkey(Keys.None);
                return;
            }

            if (IsModifier(key))
            {
                Text = Describe(e.Modifiers) + " + …"; // Waiting for the main key.
                return;
            }

            // A single letter or number would fire while typing in games: require a modifier for those.
            bool needsModifier = (key >= Keys.A && key <= Keys.Z) || (key >= Keys.D0 && key <= Keys.D9) || key == Keys.Space;
            if (needsModifier && e.Modifiers == Keys.None)
            {
                Text = Describe(_hotkey);
                return;
            }

            SetHotkey(key | e.Modifiers);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (Text.EndsWith("…")) Text = Describe(_hotkey);
            base.OnKeyUp(e);
        }

        protected override void OnLeave(EventArgs e)
        {
            Text = Describe(_hotkey);
            base.OnLeave(e);
        }

        private void SetHotkey(Keys value)
        {
            Hotkey = value;
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        }

        private static bool IsModifier(Keys key) =>
            key == Keys.ControlKey || key == Keys.ShiftKey || key == Keys.Menu ||
            key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.LShiftKey ||
            key == Keys.RShiftKey || key == Keys.LMenu || key == Keys.RMenu || key == Keys.LWin || key == Keys.RWin;

        public static string Describe(Keys keys)
        {
            if (keys == Keys.None) return S.HotkeyNone;
            var parts = new List<string>();
            if ((keys & Keys.Control) != 0) parts.Add("Ctrl");
            if ((keys & Keys.Alt) != 0) parts.Add("Alt");
            if ((keys & Keys.Shift) != 0) parts.Add("Shift");
            Keys code = keys & Keys.KeyCode;
            if (code != Keys.None && !IsModifier(code)) parts.Add(KeyName(code));
            return string.Join(" + ", parts);
        }

        private static string KeyName(Keys code)
        {
            if (code >= Keys.D0 && code <= Keys.D9) return ((int)code - (int)Keys.D0).ToString();
            if (code >= Keys.NumPad0 && code <= Keys.NumPad9) return "Num " + ((int)code - (int)Keys.NumPad0);
            switch (code)
            {
                case Keys.Oemtilde: return "`";
                case Keys.OemMinus: return "-";
                case Keys.Oemplus: return "=";
                case Keys.Next: return "PageDown";
                case Keys.Prior: return "PageUp";
                default: return code.ToString();
            }
        }
    }
}
