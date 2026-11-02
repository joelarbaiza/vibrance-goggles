using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VibranceGoggles.UI
{
    /// <summary>Pick a running program (or type an .exe name) to add to the game list.</summary>
    internal sealed class AddGameDialog : Form
    {
        private readonly ComboBox _combo;

        public string ProcessFile { get; private set; }

        public AddGameDialog()
        {
            AutoScaleMode = AutoScaleMode.None;
            Font = System.Drawing.SystemFonts.MessageBoxFont;
            Text = S.AddTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(Dpi.Px(12));

            var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
            layout.Controls.Add(new Label { Text = S.AddPrompt, AutoSize = true, Margin = new Padding(0, 0, 0, 6) });

            _combo = new ComboBox
            {
                Width = Dpi.Px(320),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
            };
            _combo.Items.AddRange(RunningWindowedPrograms());
            layout.Controls.Add(_combo);

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft, AutoSize = true,
                Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0),
            };
            var cancel = new Button { Text = S.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
            var ok = new Button { Text = S.Ok, AutoSize = true };
            ok.Click += (s, e) => Accept();
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            layout.Controls.Add(buttons);

            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void Accept()
        {
            string name = Path.GetFileName(_combo.Text.Trim());
            if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _combo.Focus();
                return;
            }
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name += ".exe";
            ProcessFile = name;
            DialogResult = DialogResult.OK;
        }

        private static object[] RunningWindowedPrograms()
        {
            int self = Process.GetCurrentProcess().Id;
            return Process.GetProcesses()
                .Where(p => p.Id != self && SafeHasWindow(p))
                .Select(p => p.ProcessName + ".exe")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Cast<object>()
                .ToArray();
        }

        private static bool SafeHasWindow(Process p)
        {
            try { return p.MainWindowHandle != IntPtr.Zero; }
            catch { return false; }
        }
    }
}
