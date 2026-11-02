using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace VibranceGoggles.UI
{
    /// <summary>Pick a running program (or type an .exe name) to add to the game list.</summary>
    internal sealed class AddGameDialog : Form
    {
        private readonly TextBox _name;
        private readonly ListBox _running;

        public string ProcessFile { get; private set; }

        public AddGameDialog()
        {
            AutoScaleMode = AutoScaleMode.None;
            Font = SystemFonts.MessageBoxFont;
            Text = S.AddTitle;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(Dpi.Px(12));

            var layout = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Dock = DockStyle.Fill };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi.Px(340)));
            layout.Controls.Add(new Label { Text = S.AddPrompt, AutoSize = true, Margin = new Padding(0, 0, 0, Dpi.Px(6)) });

            string[] programs = RunningWindowedPrograms();

            _name = new TextBox
            {
                BorderStyle = BorderStyle.None,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.CustomSource,
            };
            _name.AutoCompleteCustomSource.AddRange(programs);
            layout.Controls.Add(new RoundFrame(_name, 8) { Height = Dpi.Px(30), Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Dpi.Px(8)) });

            _running = new ListBox { BorderStyle = BorderStyle.None, IntegralHeight = false };
            _running.Items.AddRange(programs.Cast<object>().ToArray());
            _running.SelectedIndexChanged += (s, e) => { if (_running.SelectedItem != null) _name.Text = (string)_running.SelectedItem; };
            _running.DoubleClick += (s, e) => Accept();
            layout.Controls.Add(new RoundFrame(_running, 4) { Height = Dpi.Px(170), Dock = DockStyle.Fill, Margin = new Padding(0), Cursor = Cursors.Default });

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill, Margin = new Padding(0, Dpi.Px(12), 0, 0),
            };
            var cancel = new RoundButton { Text = S.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(Dpi.Px(90), Dpi.Px(30)) };
            var ok = new RoundButton { Text = S.Ok, AutoSize = true, Primary = true, MinimumSize = new Size(Dpi.Px(90), Dpi.Px(30)), Margin = new Padding(0, 0, Dpi.Px(6), 0) };
            ok.Click += (s, e) => Accept();
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            layout.Controls.Add(buttons);

            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.Apply(this);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Theme.Apply(this);
            _name.Focus();
        }

        private void Accept()
        {
            string name = Path.GetFileName(_name.Text.Trim());
            if (name.Length == 0 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                _name.Focus();
                return;
            }
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name += ".exe";
            ProcessFile = name;
            DialogResult = DialogResult.OK;
        }

        private static string[] RunningWindowedPrograms()
        {
            int self = Process.GetCurrentProcess().Id;
            return Process.GetProcesses()
                .Where(p => p.Id != self && SafeHasWindow(p))
                .Select(p => p.ProcessName + ".exe")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool SafeHasWindow(Process p)
        {
            try { return p.MainWindowHandle != IntPtr.Zero; }
            catch { return false; }
        }
    }
}
