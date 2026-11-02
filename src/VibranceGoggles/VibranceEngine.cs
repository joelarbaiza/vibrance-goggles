using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using VibranceGoggles.Gpu;

namespace VibranceGoggles
{
    /// <summary>
    /// Watches which program is in front and applies the matching vibrance:
    /// the game's level while a listed game is active, the desktop level otherwise.
    /// </summary>
    internal sealed class VibranceEngine : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly GpuManager _gpu;
        private readonly Timer _timer = new Timer { Interval = 500 };
        private int? _applied;

        public event EventHandler StateChanged;

        /// <summary>The game currently driving the vibrance, or null when on the desktop.</summary>
        public GameEntry ActiveGame { get; private set; }

        public int AppliedVibrance => _applied ?? _settings.DesktopVibrance;

        public VibranceEngine(AppSettings settings, GpuManager gpu)
        {
            _settings = settings;
            _gpu = gpu;
            _timer.Tick += (s, e) => Update();
        }

        public void Start()
        {
            Update();
            _timer.Start();
        }

        /// <summary>Re-evaluate now and re-send the value even if it did not change.</summary>
        public void Refresh()
        {
            _applied = null;
            Update();
        }

        /// <summary>Put the desktop level back (used on exit).</summary>
        public void Restore()
        {
            _timer.Stop();
            _gpu.SetVibrance(_settings.DesktopVibrance);
            _applied = _settings.DesktopVibrance;
        }

        private void Update()
        {
            GameEntry game = _settings.Enabled ? FindActiveGame() : null;
            int target = game?.Vibrance ?? _settings.DesktopVibrance;

            bool changed = !ReferenceEquals(game, ActiveGame) || _applied != target;
            ActiveGame = game;

            if (_applied != target)
            {
                _gpu.SetVibrance(target);
                _applied = target;
            }

            if (changed) StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private GameEntry FindActiveGame()
        {
            if (!_settings.Games.Any(g => g.Enabled)) return null;

            GameEntry foreground = _settings.FindGame(ForegroundProcessName());
            if (foreground != null && !foreground.Enabled) foreground = null;
            if (foreground != null || _settings.OnlyWhenFocused) return foreground;

            // "Always" mode: any listed game that is running counts, in list order.
            var running = Process.GetProcesses().Select(p => p.ProcessName)
                .ToLookup(n => n, StringComparer.OrdinalIgnoreCase);
            return _settings.Games.FirstOrDefault(g => g.Enabled && running.Contains(g.ProcessName));
        }

        private static string ForegroundProcessName()
        {
            IntPtr window = GetForegroundWindow();
            if (window == IntPtr.Zero) return null;
            GetWindowThreadProcessId(window, out uint pid);
            try
            {
                using (Process p = Process.GetProcessById((int)pid)) return p.ProcessName;
            }
            catch
            {
                return null; // Process ended between the two calls.
            }
        }

        public void Dispose() => _timer.Dispose();

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}
