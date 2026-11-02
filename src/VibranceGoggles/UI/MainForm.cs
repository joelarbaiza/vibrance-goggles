using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceGoggles.Gpu;

namespace VibranceGoggles.UI
{
    internal sealed class MainForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HotkeyId = 0x5647; // "VG"
        private const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000;

        private readonly AppSettings _settings;
        private readonly GpuManager _gpu;
        private readonly VibranceEngine _engine;

        private readonly NotifyIcon _tray;
        private readonly ToolStripMenuItem _trayToggle;
        private readonly ContextMenuStrip _trayMenu;
        private readonly VibranceSlider _desktopSlider;
        private readonly VibranceSlider _gameSlider;
        private readonly ListView _gameList;
        private readonly Button _removeButton;
        private readonly CheckBox _chkEnabled, _chkFocused, _chkStartup;
        private readonly HotkeyBox _hotkeyBox;
        private readonly Label _hotkeyStatus;
        private readonly Label _status;

        private bool _loading;
        private bool _exiting;

        /// <summary>Set when the app should start again after exiting (language change).</summary>
        public bool RestartRequested { get; private set; }

        public MainForm(AppSettings settings, GpuManager gpu)
        {
            _settings = settings;
            _gpu = gpu;
            _engine = new VibranceEngine(settings, gpu);

            SuspendLayout();
            AutoScaleMode = AutoScaleMode.None; // Sizes below go through Dpi.Px instead.
            Text = S.AppName;
            Icon = AppIcons.Current;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            StartPosition = FormStartPosition.Manual; // We center it ourselves, every time it is shown.
            Padding = new Padding(Dpi.Px(10));
            Font = SystemFonts.MessageBoxFont;

            var root = new TableLayoutPanel { ColumnCount = 1, AutoSize = true };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dpi.Px(430)));

            // --- Graphics adapter -----------------------------------------------------------------
            var gpuLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(Dpi.Px(420), 0),
                Margin = new Padding(3, 0, 3, 8),
                Text = gpu.HasGpu ? $"{S.Graphics}: {gpu.Description}" : S.NoGpu,
                Tag = gpu.HasGpu ? Theme.Subtle : Theme.Error,
            };
            root.Controls.Add(gpuLabel);

            // --- Desktop ------------------------------------------------------------------------------
            _desktopSlider = new VibranceSlider { Dock = DockStyle.Fill };
            root.Controls.Add(Group(S.Desktop, _desktopSlider));

            // --- Games --------------------------------------------------------------------------------
            _gameList = new ListView
            {
                View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable, Height = Dpi.Px(130), Dock = DockStyle.Fill,
                CheckBoxes = true, OwnerDraw = true, // Owner draw only to color the header in dark mode.
            };
            _gameList.Columns.Add(S.Game, Dpi.Px(290));
            _gameList.Columns.Add(S.Vibrance, Dpi.Px(90), HorizontalAlignment.Right);
            _gameList.DrawColumnHeader += DrawListHeader;
            // Columns always fill the list, so no unpainted (white) header area is left on the right.
            _gameList.ClientSizeChanged += (s, e) =>
                _gameList.Columns[0].Width = Math.Max(Dpi.Px(120), _gameList.ClientSize.Width - _gameList.Columns[1].Width);
            // Rows are painted with theme colors: the default hover highlight is light with dark text,
            // which is unreadable in dark mode.
            _gameList.DrawItem += (s, e) => { };
            _gameList.DrawSubItem += DrawGameCell;
            _gameList.MouseMove += (s, e) => SetHoverRow(_gameList.GetItemAt(e.X, e.Y));
            _gameList.MouseLeave += (s, e) => SetHoverRow(null);
            var gamesHint = new Label { Text = S.GamesHint, AutoSize = true, Tag = Theme.Subtle, Margin = new Padding(3, 2, 3, 0) };

            var addButton = new Button { Text = S.Add, AutoSize = true };
            _removeButton = new Button { Text = S.Remove, AutoSize = true, Enabled = false };
            var gameButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4) };
            gameButtons.Controls.Add(addButton);
            gameButtons.Controls.Add(_removeButton);

            _gameSlider = new VibranceSlider { Dock = DockStyle.Fill, Enabled = false };
            var gamesPanel = Stack(_gameList, gamesHint, gameButtons,
                new Label { Text = S.SelectedGame, AutoSize = true, Margin = new Padding(3, 4, 3, 0) }, _gameSlider);
            root.Controls.Add(Group(S.Games, gamesPanel));

            // --- Options ------------------------------------------------------------------------------
            _chkEnabled = new CheckBox { Text = S.Enabled, AutoSize = true };
            _chkFocused = new CheckBox { Text = S.OnlyFocused, AutoSize = true };
            _chkStartup = new CheckBox { Text = S.StartWithWindows, AutoSize = true };

            var languageBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Dpi.Px(200) };
            languageBox.Items.Add(S.LanguageAuto);
            languageBox.Items.AddRange(S.NativeNames);
            int langIndex = Array.IndexOf(S.Codes, _settings.Language ?? "");
            languageBox.SelectedIndex = langIndex >= 0 ? langIndex + 1 : 0;
            languageBox.SelectedIndexChanged += (s, e) =>
            {
                _settings.Language = languageBox.SelectedIndex == 0 ? "" : S.Codes[languageBox.SelectedIndex - 1];
                _settings.Save();
                RestartRequested = true; // Rebuild the UI in the new language.
                ExitApp();
            };
            var languageRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) };
            languageRow.Controls.Add(new Label { Text = S.Language + ":", AutoSize = true, Margin = new Padding(3, 6, 3, 0) });
            languageRow.Controls.Add(languageBox);

            _hotkeyBox = new HotkeyBox { Width = Dpi.Px(170) };
            _hotkeyStatus = new Label { AutoSize = true, Tag = Theme.Subtle, Text = S.HotkeyHint };
            var hotkeyRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 0) };
            hotkeyRow.Controls.Add(new Label { Text = S.Hotkey + ":", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 3, 0) });
            hotkeyRow.Controls.Add(_hotkeyBox);

            root.Controls.Add(Group(S.Options,
                Stack(_chkEnabled, _chkFocused, _chkStartup, hotkeyRow, _hotkeyStatus, languageRow)));

            // --- Status -------------------------------------------------------------------------------
            _status = new Label { AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Margin = new Padding(3, 6, 3, 0), Font = new Font(Font, FontStyle.Bold) };
            root.Controls.Add(_status);

            // --- Footer: repo link + version --------------------------------------------------------
            var star = new LinkLabel { Text = S.Star, AutoSize = true, Anchor = AnchorStyles.Left, LinkBehavior = LinkBehavior.HoverUnderline };
            star.LinkClicked += (s, e) => OpenRepo();
            var version = new Label
            {
                Text = $"v{Application.ProductVersion.Split('+')[0]} · @joelarbaiza",
                AutoSize = true, Anchor = AnchorStyles.Right, Tag = Theme.Subtle,
            };
            var footer = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, Dpi.Px(8), 0, 0) };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            footer.Controls.Add(star, 0, 0);
            footer.Controls.Add(version, 1, 0);
            root.Controls.Add(footer);

            Controls.Add(root);
            ResumeLayout(true);

            // --- Tray ---------------------------------------------------------------------------------
            _trayToggle = new ToolStripMenuItem();
            _trayToggle.Click += (s, e) => SetEnabled(!_settings.Enabled, notify: false);
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(S.Open, null, (s, e) => ShowCentered());
            trayMenu.Items.Add(_trayToggle);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(S.Exit, null, (s, e) => ExitApp());
            _trayMenu = trayMenu;
            _tray = new NotifyIcon { Icon = AppIcons.Current, Text = S.AppName, ContextMenuStrip = trayMenu, Visible = true };
            _tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowCentered(); };

            // --- Events -------------------------------------------------------------------------------
            _desktopSlider.ValueChanged += (s, e) => { if (_loading) return; _settings.DesktopVibrance = _desktopSlider.Value; SaveAndRefresh(); };
            _gameSlider.ValueChanged += (s, e) => OnGameVibranceChanged();
            _gameList.SelectedIndexChanged += (s, e) => OnGameSelected();
            addButton.Click += (s, e) => AddGame();
            _removeButton.Click += (s, e) => RemoveGame();
            _chkEnabled.CheckedChanged += (s, e) => { if (!_loading) SetEnabled(_chkEnabled.Checked, notify: false); };
            _chkFocused.CheckedChanged += (s, e) => { if (_loading) return; _settings.OnlyWhenFocused = _chkFocused.Checked; SaveAndRefresh(); };
            _chkStartup.CheckedChanged += (s, e) => { if (_loading) return; _settings.StartWithWindows = _chkStartup.Checked; ApplyAutostart(); _settings.Save(); };

            _hotkeyBox.HotkeyChanged += (s, e) => { _settings.Hotkey = (int)_hotkeyBox.Hotkey; _settings.Save(); RegisterToggleHotkey(); };
            _engine.StateChanged += (s, e) => UpdateStatus();
            _gameList.ItemChecked += (s, e) => OnGameChecked(e.Item);
            Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

            LoadValues();
            if (_settings.StartWithWindows) ApplyAutostart(); // Keep the path current if the exe moved.

            CreateHandle(); // Needed for the hotkey even while hidden in the tray.
            ApplyTheme();
            RegisterToggleHotkey();
            _engine.Start();
        }

        // =============================================================================== Window

        /// <summary>Show the window in the middle of the screen the mouse is on.</summary>
        public void ShowCentered()
        {
            if (!Visible)
            {
                Rectangle area = Screen.FromPoint(Cursor.Position).WorkingArea;
                Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
                Show();
            }
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!_settings.TrayHintShown)
                {
                    _tray.ShowBalloonTip(3000, S.AppName, S.TrayHint, ToolTipIcon.Info);
                    _settings.TrayHintShown = true;
                    _settings.Save();
                }
                return;
            }
            // Windows shutdown, sign-out, Task Manager: put normal colors back before leaving.
            if (!_exiting) _engine.Restore();
            base.OnFormClosing(e);
        }

        private void ExitApp()
        {
            _exiting = true;
            _engine.Restore();
            UnregisterHotKey(Handle, HotkeyId);
            _tray.Visible = false;
            _tray.Dispose();
            _engine.Dispose();
            Close();
            Application.Exit();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HotkeyId)
                SetEnabled(!_settings.Enabled, notify: true);
            base.WndProc(ref m);
        }

        // =============================================================================== Values

        private void LoadValues()
        {
            _loading = true;
            ApplyRange();
            _desktopSlider.Value = _settings.DesktopVibrance;
            _chkEnabled.Checked = _settings.Enabled;
            _chkFocused.Checked = _settings.OnlyWhenFocused;
            _chkStartup.Checked = _settings.StartWithWindows;

            _hotkeyBox.Hotkey = (Keys)_settings.Hotkey;

            _gameList.Items.Clear();
            foreach (GameEntry game in _settings.Games) _gameList.Items.Add(ItemFor(game));
            _loading = false;
            UpdateToggleTexts();
        }

        private void ApplyRange()
        {
            int max = _gpu.MaxVibrance;
            _desktopSlider.Maximum = max;
            _gameSlider.Maximum = max;
            foreach (GameEntry g in _settings.Games) g.Vibrance = Math.Min(g.Vibrance, max);
            _settings.DesktopVibrance = Math.Min(_settings.DesktopVibrance, max);
            foreach (ListViewItem item in _gameList.Items) item.SubItems[1].Text = ((GameEntry)item.Tag).Vibrance + " %";
        }

        private void SaveAndRefresh()
        {
            _settings.Save();
            _engine.Refresh();
        }

        private void SetEnabled(bool enabled, bool notify)
        {
            _settings.Enabled = enabled;
            _loading = true;
            _chkEnabled.Checked = enabled;
            _loading = false;
            UpdateToggleTexts();
            SaveAndRefresh();
            if (notify) _tray.ShowBalloonTip(1500, S.AppName, enabled ? S.ToggledOn : S.ToggledOff, ToolTipIcon.None);
        }

        private void UpdateToggleTexts()
        {
            string hotkey = _settings.Hotkey != 0 ? $"\t{HotkeyBox.Describe((Keys)_settings.Hotkey)}" : "";
            _trayToggle.Text = (_settings.Enabled ? S.Disable : S.Enable) + hotkey;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            string text;
            if (!_settings.Enabled) text = $"{S.StatusDisabled} · {_settings.DesktopVibrance} %";
            else if (_engine.ActiveGame != null) text = $"▶ {_engine.ActiveGame.Process} · {_engine.ActiveGame.Vibrance} %";
            else text = $"{S.StatusDesktop} · {_settings.DesktopVibrance} %";

            _status.Text = text;
            string tip = $"{S.AppName} — {text}";
            _tray.Text = tip.Length > 63 ? tip.Substring(0, 63) : tip;
        }

        // =============================================================================== Games

        private static ListViewItem ItemFor(GameEntry game)
        {
            var item = new ListViewItem(new[] { game.Process, game.Vibrance + " %" }) { Tag = game, Checked = game.Enabled };
            StyleItem(item);
            return item;
        }

        private static void StyleItem(ListViewItem item)
        {
            bool enabled = ((GameEntry)item.Tag).Enabled;
            item.UseItemStyleForSubItems = true;
            item.ForeColor = enabled ? Theme.Text : Theme.SubtleText;
            item.BackColor = Theme.Surface;
        }

        private void OnGameChecked(ListViewItem item)
        {
            if (_loading || !(item.Tag is GameEntry game) || game.Enabled == item.Checked) return;
            game.Enabled = item.Checked;
            StyleItem(item);
            SaveAndRefresh();
        }

        private ListViewItem _hoverRow;

        private void SetHoverRow(ListViewItem item)
        {
            if (ReferenceEquals(item, _hoverRow)) return;
            ListViewItem old = _hoverRow;
            _hoverRow = item;
            if (old != null && old.ListView != null) _gameList.Invalidate(old.Bounds);
            if (item != null) _gameList.Invalidate(item.Bounds);
        }

        /// <summary>Paints one cell: theme background (normal / hover / selected), checkbox and text.</summary>
        private void DrawGameCell(object sender, DrawListViewSubItemEventArgs e)
        {
            Graphics g = e.Graphics;
            Color back = e.Item.Selected ? Theme.RowSelected
                       : ReferenceEquals(e.Item, _hoverRow) ? Theme.RowHover
                       : Theme.Surface;
            using (var b = new SolidBrush(back)) g.FillRectangle(b, e.Bounds);

            bool enabled = ((GameEntry)e.Item.Tag).Enabled;
            Color text = enabled ? Theme.Text : Theme.SubtleText;
            if (e.Item.Selected && !Theme.IsDark) text = SystemColors.ControlText;

            Rectangle textBounds = Rectangle.Inflate(e.Bounds, -Dpi.Px(6), 0);
            if (e.ColumnIndex == 0)
            {
                var state = e.Item.Checked
                    ? System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
                    : System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal;
                Size glyph = CheckBoxRenderer.GetGlyphSize(g, state);
                var at = new Point(e.Bounds.X + Dpi.Px(4), e.Bounds.Y + (e.Bounds.Height - glyph.Height) / 2);
                CheckBoxRenderer.DrawCheckBox(g, at, state);
                textBounds = new Rectangle(at.X + glyph.Width + Dpi.Px(6), e.Bounds.Y,
                                           e.Bounds.Right - at.X - glyph.Width - Dpi.Px(10), e.Bounds.Height);
            }

            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine |
                        (e.Header.TextAlign == HorizontalAlignment.Right ? TextFormatFlags.Right : TextFormatFlags.Left);
            TextRenderer.DrawText(g, e.SubItem.Text, _gameList.Font, textBounds, text, flags);
        }

        private void DrawListHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var back = new SolidBrush(Theme.HeaderBack))
            using (var line = new Pen(Theme.Border))
            {
                e.Graphics.FillRectangle(back, e.Bounds);
                e.Graphics.DrawLine(line, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                e.Graphics.DrawLine(line, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 5);
            }
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                        (e.Header.TextAlign == HorizontalAlignment.Right ? TextFormatFlags.Right : TextFormatFlags.Left);
            var bounds = Rectangle.Inflate(e.Bounds, -Dpi.Px(6), 0);
            TextRenderer.DrawText(e.Graphics, e.Header.Text, _gameList.Font, bounds, Theme.Text, flags);
        }

        // =============================================================================== Theme / links

        private void ApplyTheme()
        {
            if (IsDisposed || Disposing || _exiting) return;
            Theme.Apply(this);

            // Window and tray icon follow light/dark too (cached icons, no new handles per change).
            Icon icon = AppIcons.Current;
            if (!ReferenceEquals(Icon, icon))
            {
                Icon = icon;
                if (_tray != null) _tray.Icon = icon;
                Log.Write("Icon variant: " + (Theme.IsDark ? "dark" : "light"));
            }
            _trayMenu.Renderer = Theme.MenuRenderer();
            foreach (ToolStripItem i in _trayMenu.Items) i.ForeColor = Theme.IsDark ? Theme.Text : SystemColors.ControlText;
            foreach (ListViewItem item in _gameList.Items) StyleItem(item);
            _gameList.Invalidate();
        }

        private void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            // Raised on a system thread when Windows switches between light and dark.
            if (Theme.Refresh() && IsHandleCreated && !IsDisposed) BeginInvoke(new Action(ApplyTheme));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyTheme(); // Child handles exist now (list scrollbars, title bar).
            ActiveControl = null; // No focus rectangle on the first slider when the window opens.
        }

        private static void OpenRepo()
        {
            try { Process.Start("https://github.com/joelarbaiza/vibrance-goggles"); }
            catch (Exception ex) { Log.Write("Could not open browser: " + ex.Message); }
        }

        private GameEntry SelectedGame => _gameList.SelectedItems.Count > 0 ? (GameEntry)_gameList.SelectedItems[0].Tag : null;

        private void OnGameSelected()
        {
            GameEntry game = SelectedGame;
            _removeButton.Enabled = game != null;
            _gameSlider.Enabled = game != null;
            if (game != null)
            {
                _loading = true;
                _gameSlider.Value = game.Vibrance;
                _loading = false;
            }
        }

        private void OnGameVibranceChanged()
        {
            GameEntry game = SelectedGame;
            if (_loading || game == null) return;
            game.Vibrance = _gameSlider.Value;
            _gameList.SelectedItems[0].SubItems[1].Text = game.Vibrance + " %";
            SaveAndRefresh();
        }

        private void AddGame()
        {
            using (var dialog = new AddGameDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                GameEntry game = _settings.FindGame(System.IO.Path.GetFileNameWithoutExtension(dialog.ProcessFile));
                if (game == null)
                {
                    game = new GameEntry { Process = dialog.ProcessFile, Vibrance = Math.Min(150, _gameSlider.Maximum) };
                    _settings.Games.Add(game);
                    _gameList.Items.Add(ItemFor(game));
                    SaveAndRefresh();
                }

                foreach (ListViewItem item in _gameList.Items)
                {
                    if (item.Tag == game) { item.Selected = true; item.EnsureVisible(); }
                }
                _gameList.Focus();
            }
        }

        private void RemoveGame()
        {
            GameEntry game = SelectedGame;
            if (game == null) return;
            _settings.Games.Remove(game);
            _gameList.SelectedItems[0].Remove();
            SaveAndRefresh();
        }

        // =============================================================================== System

        private void ApplyAutostart()
        {
            try { Autostart.Apply(_settings.StartWithWindows); }
            catch (Exception ex) { Log.Write("Autostart failed: " + ex.Message); }
        }

        private void RegisterToggleHotkey()
        {
            UnregisterHotKey(Handle, HotkeyId);
            var keys = (Keys)_settings.Hotkey;
            bool ok = true;

            if (keys != Keys.None)
            {
                uint mods = MOD_NOREPEAT;
                if ((keys & Keys.Control) != 0) mods |= MOD_CONTROL;
                if ((keys & Keys.Alt) != 0) mods |= MOD_ALT;
                if ((keys & Keys.Shift) != 0) mods |= MOD_SHIFT;
                ok = RegisterHotKey(Handle, HotkeyId, mods, (uint)(keys & Keys.KeyCode));
            }

            _hotkeyStatus.Text = ok ? S.HotkeyHint : S.HotkeyBusy;
            _hotkeyStatus.Tag = ok ? Theme.Subtle : Theme.Error;
            Theme.Style(_hotkeyStatus);
            UpdateToggleTexts();
        }

        // =============================================================================== Layout helpers

        private static GroupBox Group(string title, Control content)
        {
            var box = new GroupBox
            {
                Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 8), Margin = new Padding(0, 0, 0, 8),
            };
            content.Dock = DockStyle.Fill;
            box.Controls.Add(content);
            return box;
        }

        private static TableLayoutPanel Stack(params Control[] controls)
        {
            var panel = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Margin = new Padding(0) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (Control c in controls) panel.Controls.Add(c);
            return panel;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
                _tray?.Dispose();
                _engine?.Dispose();
            }
            base.Dispose(disposing);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
