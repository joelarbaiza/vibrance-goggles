using System;
using System.Drawing;
using System.Windows.Forms;

namespace VibranceGoggles.UI
{
    /// <summary>Modern slider + numeric box kept in sync, in percent, with a reset-to-100 % button.</summary>
    internal sealed class VibranceSlider : TableLayoutPanel
    {
        public const int Normal = 100;

        private readonly ModernSlider _slider;
        private readonly TextBox _number;
        private readonly RoundFrame _numberFrame;
        private readonly RoundButton _reset;
        private bool _syncing;

        public event EventHandler ValueChanged;

        public VibranceSlider()
        {
            AutoSize = true;
            ColumnCount = 3;
            RowCount = 1;
            Margin = new Padding(0);
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _slider = new ModernSlider { Dock = DockStyle.Fill, Margin = new Padding(0, 0, Dpi.Px(8), 0) };

            _number = new TextBox { Text = Normal.ToString(), BorderStyle = BorderStyle.None, TextAlign = HorizontalAlignment.Center, MaxLength = 3 };
            _number.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };
            _number.KeyDown += OnNumberKeyDown;
            _number.Leave += (s, e) => CommitNumber();
            _numberFrame = new RoundFrame(_number, 8)
            {
                Width = Dpi.Px(64), Height = Dpi.Px(30), Anchor = AnchorStyles.Left, Margin = new Padding(0),
            };

            _reset = new RoundButton
            {
                Text = "↺", Width = Dpi.Px(34), Height = Dpi.Px(30), Anchor = AnchorStyles.Left,
                Margin = new Padding(Dpi.Px(6), 0, 0, 0), Padding = new Padding(0),
                Font = new Font("Segoe UI Symbol", 11f),
            };
            new ToolTip().SetToolTip(_reset, S.ResetTip);

            Controls.Add(_slider, 0, 0);
            Controls.Add(_numberFrame, 1, 0);
            Controls.Add(_reset, 2, 0);

            _slider.ValueChanged += (s, e) => Sync(_slider.Value);

            _reset.Click += (s, e) => Sync(Normal);
        }

        public int Value
        {
            get => _slider.Value;
            set
            {
                _syncing = true;
                int v = Math.Max(0, Math.Min(Maximum, value));
                _slider.Value = v;
                _number.Text = v.ToString();
                _syncing = false;
            }
        }

        public int Maximum
        {
            get => _slider.Maximum;
            set
            {
                _syncing = true;
                int v = Math.Min(Value, value);
                _slider.Maximum = value;
                _slider.Value = v;
                _number.Text = v.ToString();
                _syncing = false;
            }
        }

        public new bool Enabled
        {
            get => _slider.Enabled;
            set { _slider.Enabled = value; _number.Enabled = value; _reset.Enabled = value; }
        }

        private void Sync(int value)
        {
            if (_syncing) return;
            _syncing = true;
            value = Math.Max(0, Math.Min(Maximum, value));
            if (_slider.Value != value) _slider.Value = value;
            if (_number.Text != value.ToString()) _number.Text = value.ToString();
            _syncing = false;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CommitNumber()
        {
            if (int.TryParse(_number.Text, out int v)) Sync(v);
            else _number.Text = Value.ToString();
        }

        private void OnNumberKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter: CommitNumber(); e.SuppressKeyPress = true; break;
                case Keys.Up: Sync(Value + 1); e.Handled = true; break;
                case Keys.Down: Sync(Value - 1); e.Handled = true; break;
                case Keys.PageUp: Sync(Value + 10); e.Handled = true; break;
                case Keys.PageDown: Sync(Value - 10); e.Handled = true; break;
            }
        }
    }
}
