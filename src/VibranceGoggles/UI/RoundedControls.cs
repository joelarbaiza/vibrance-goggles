using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace VibranceGoggles.UI
{
    /// <summary>Rounded-rectangle helpers shared by the custom controls.</summary>
    internal static class Shapes
    {
        public static GraphicsPath Rounded(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            if (d <= 0) { path.AddRectangle(r); return path; }
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>Fills the control with its parent's color, so the rounded corners blend in.</summary>
        public static void PaintParentBack(Control c, Graphics g)
        {
            using (var b = new SolidBrush(c.Parent?.BackColor ?? Theme.Back)) g.FillRectangle(b, c.ClientRectangle);
        }

        public static void Fill(Graphics g, Rectangle bounds, float radius, Color fill, Color? border = null)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new RectangleF(bounds.X + 0.5f, bounds.Y + 0.5f, bounds.Width - 1f, bounds.Height - 1f);
            using (GraphicsPath path = Rounded(r, radius))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                if (border.HasValue) using (var p = new Pen(border.Value)) g.DrawPath(p, path);
            }
        }
    }

    /// <summary>A rounded "card" with an optional title, replacing the old square GroupBox.</summary>
    internal sealed class Card : TableLayoutPanel
    {
        private readonly Label _title;

        public Card(string title, Control content)
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ColumnCount = 1;
            Dock = DockStyle.Fill;
            Padding = new Padding(Dpi.Px(12), Dpi.Px(8), Dpi.Px(12), Dpi.Px(8));
            Margin = new Padding(0, 0, 0, Dpi.Px(8));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            if (title != null)
            {
                _title = new Label { Text = title, AutoSize = true, Margin = new Padding(0, 0, 0, Dpi.Px(4)) };
                Controls.Add(_title);
            }
            content.Dock = DockStyle.Fill;
            Controls.Add(content);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_title != null) _title.Font = new Font(Font, FontStyle.Bold);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Shapes.PaintParentBack(this, e.Graphics);
            Shapes.Fill(e.Graphics, ClientRectangle, Dpi.Px(8), Theme.Card, Theme.CardBorder);
        }
    }

    /// <summary>Flat button with rounded corners and hover / pressed states.</summary>
    internal sealed class RoundButton : Button
    {
        private bool _hover, _pressed;

        public bool Primary { get; set; }

        public RoundButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Padding = new Padding(Dpi.Px(10), Dpi.Px(2), Dpi.Px(10), Dpi.Px(2));
            MinimumSize = new Size(0, Dpi.Px(30));
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = _pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Shapes.PaintParentBack(this, g);

            Color fill, border, text;
            if (!Enabled)
            {
                fill = Theme.Input; border = Theme.CardBorder; text = Theme.DisabledText;
            }
            else if (Primary)
            {
                fill = _pressed ? ControlPaint.Dark(Theme.AccentFill, 0.1f) : _hover ? ControlPaint.Light(Theme.AccentFill, 0.1f) : Theme.AccentFill;
                border = fill; text = Color.White;
            }
            else
            {
                fill = _pressed ? Theme.InputPressed : _hover ? Theme.InputHover : Theme.Input;
                border = Focused ? Theme.Accent : Theme.InputBorder; text = Theme.Text;
            }

            Shapes.Fill(g, ClientRectangle, Dpi.Px(6), fill, border);
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
    }

    /// <summary>Rounded border around a borderless input (text box, numeric box, list).</summary>
    internal sealed class RoundFrame : Panel
    {
        public Control Inner { get; }

        public RoundFrame(Control inner, int innerPadding = 6)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            Inner = inner;
            int p = Dpi.Px(innerPadding);
            Padding = new Padding(p, Dpi.Px(4), p, Dpi.Px(4));
            inner.Dock = DockStyle.Fill;
            Controls.Add(inner);
            inner.GotFocus += (s, e) => Invalidate();
            inner.LostFocus += (s, e) => Invalidate();
            inner.EnabledChanged += (s, e) => Invalidate();
            Cursor = Cursors.IBeam;
            Click += (s, e) => inner.Focus();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Shapes.PaintParentBack(this, e.Graphics);
            Color border = Inner.ContainsFocus ? Theme.Accent : Theme.InputBorder;
            Shapes.Fill(e.Graphics, ClientRectangle, Dpi.Px(6), Theme.Input, border);
            if (Inner.ContainsFocus)
            {
                // Windows 11 style: a thicker accent line along the bottom edge.
                using (var p = new Pen(Theme.Accent, Dpi.Px(2)))
                    e.Graphics.DrawLine(p, Dpi.Px(6), Height - Dpi.Px(1), Width - Dpi.Px(6), Height - Dpi.Px(1));
            }
        }
    }

    /// <summary>Modern slider: rounded track, accent fill, round thumb and a mark at "normal" (100 %).</summary>
    internal sealed class ModernSlider : Control
    {
        private int _value = 100, _min, _max = 200;
        private bool _dragging, _hover;

        public event EventHandler ValueChanged;

        /// <summary>Value marked with a small tick (normal colors).</summary>
        public int Mark { get; set; } = 100;

        public ModernSlider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint | ControlStyles.Selectable, true);
            TabStop = true;
            Height = Dpi.Px(30);
            Cursor = Cursors.Hand;
        }

        public int Minimum { get => _min; set { _min = value; Value = _value; Invalidate(); } }
        public int Maximum { get => _max; set { _max = Math.Max(_min + 1, value); Value = _value; Invalidate(); } }

        public int Value
        {
            get => _value;
            set
            {
                int v = Math.Max(_min, Math.Min(_max, value));
                if (v == _value) return;
                _value = v;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private int ThumbRadius => Dpi.Px(9);
        private float TrackLeft => ThumbRadius + Dpi.Px(1);
        private float TrackRight => Width - ThumbRadius - Dpi.Px(1);
        private float XFor(int v) => TrackLeft + (TrackRight - TrackLeft) * (v - _min) / (float)(_max - _min);
        private int ValueAt(int x) => (int)Math.Round(_min + (x - TrackLeft) / (TrackRight - TrackLeft) * (_max - _min));

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Shapes.PaintParentBack(this, g);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float cy = Height / 2f;
            float trackH = Dpi.Px(4);
            Color accent = Enabled ? Theme.Accent : Theme.DisabledText;

            using (GraphicsPath track = Shapes.Rounded(new RectangleF(TrackLeft, cy - trackH / 2, TrackRight - TrackLeft, trackH), trackH / 2))
            using (var b = new SolidBrush(Theme.Track)) g.FillPath(b, track);

            float x = XFor(_value);
            if (x > TrackLeft)
            {
                using (GraphicsPath fill = Shapes.Rounded(new RectangleF(TrackLeft, cy - trackH / 2, x - TrackLeft, trackH), trackH / 2))
                using (var b = new SolidBrush(accent)) g.FillPath(b, fill);
            }

            if (Mark > _min && Mark < _max)
            {
                float mx = XFor(Mark);
                using (var p = new Pen(Theme.SubtleText, Dpi.Px(2))) g.DrawLine(p, mx, cy + Dpi.Px(6), mx, cy + Dpi.Px(10));
            }

            // Thumb: outer disc with border, inner accent dot that grows on hover (like Windows 11).
            float r = ThumbRadius;
            using (var b = new SolidBrush(Theme.ThumbOuter)) g.FillEllipse(b, x - r, cy - r, r * 2, r * 2);
            using (var p = new Pen(Theme.InputBorder)) g.DrawEllipse(p, x - r, cy - r, r * 2, r * 2);
            float inner = (_hover || _dragging) && Enabled ? r * 0.62f : r * 0.48f;
            using (var b = new SolidBrush(accent)) g.FillEllipse(b, x - inner, cy - inner, inner * 2, inner * 2);

            if (Focused && ShowFocusCues)
                using (var p = new Pen(Theme.Accent) { DashStyle = DashStyle.Dot })
                    g.DrawEllipse(p, x - r - 2, cy - r - 2, r * 2 + 4, r * 2 + 4);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!Enabled || e.Button != MouseButtons.Left) return;
            Focus();
            _dragging = true;
            Capture = true;
            Value = ValueAt(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging) Value = ValueAt(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
            Capture = false;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left: case Keys.Right: case Keys.Up: case Keys.Down: return true;
                default: return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Left: case Keys.Down: Value -= 1; break;
                case Keys.Right: case Keys.Up: Value += 1; break;
                case Keys.PageDown: Value -= 10; break;
                case Keys.PageUp: Value += 10; break;
                case Keys.Home: Value = _min; break;
                case Keys.End: Value = _max; break;
            }
        }
    }
}
