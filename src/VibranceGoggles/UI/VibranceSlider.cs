using System;
using System.Windows.Forms;

namespace VibranceGoggles.UI
{
    /// <summary>Track bar + numeric box kept in sync, in percent, with a reset-to-100 % button.</summary>
    internal sealed class VibranceSlider : TableLayoutPanel
    {
        public const int Normal = 100;

        private readonly TrackBar _track;
        private readonly NumericUpDown _number;
        private readonly Button _reset;
        private bool _syncing;

        public event EventHandler ValueChanged;

        public VibranceSlider()
        {
            AutoSize = true;
            ColumnCount = 4;
            RowCount = 1;
            Margin = new Padding(0);
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _track = new TrackBar
            {
                Minimum = 0, Maximum = 200, Value = Normal,
                TickStyle = TickStyle.None, SmallChange = 1, LargeChange = 10,
                Dock = DockStyle.Fill, AutoSize = false, Height = Dpi.Px(30),
            };
            _number = new NumericUpDown
            {
                Minimum = 0, Maximum = 200, Value = Normal, Width = Dpi.Px(64),
                TextAlign = HorizontalAlignment.Right, Anchor = AnchorStyles.Left,
            };
            var percent = new Label { Text = "%", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 3, 0) };
            _reset = new Button
            {
                Text = "↺", Width = Dpi.Px(30), Height = Dpi.Px(26), Anchor = AnchorStyles.Left,
                Margin = new Padding(Dpi.Px(4), 0, 0, 0), Font = new System.Drawing.Font("Segoe UI Symbol", 10f),
            };
            new ToolTip().SetToolTip(_reset, S.ResetTip);

            Controls.Add(_track, 0, 0);
            Controls.Add(_number, 1, 0);
            Controls.Add(percent, 2, 0);
            Controls.Add(_reset, 3, 0);

            _track.ValueChanged += (s, e) => Sync(_track.Value);
            _number.ValueChanged += (s, e) => Sync((int)_number.Value);
            _reset.Click += (s, e) => Sync(Normal);
        }

        public int Value
        {
            get => _track.Value;
            set
            {
                _syncing = true;
                int v = Math.Max(0, Math.Min(Maximum, value));
                _track.Value = v;
                _number.Value = v;
                _syncing = false;
            }
        }

        public int Maximum
        {
            get => _track.Maximum;
            set
            {
                _syncing = true;
                int v = Math.Min(Value, value);
                _track.Maximum = value;
                _number.Maximum = value;
                _track.Value = v;
                _number.Value = v;
                _syncing = false;
            }
        }

        public new bool Enabled
        {
            get => _track.Enabled;
            set { _track.Enabled = value; _number.Enabled = value; _reset.Enabled = value; }
        }

        private void Sync(int value)
        {
            if (_syncing) return;
            _syncing = true;
            if (_track.Value != value) _track.Value = value;
            if ((int)_number.Value != value) _number.Value = value;
            _syncing = false;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
