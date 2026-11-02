using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using VibranceGoggles.Gpu;

namespace VibranceGoggles
{
    /// <summary>
    /// Watches which program is in front and applies the matching vibrance:
    /// the game's level while a listed game is active, the desktop level otherwise.
    ///
    /// Event driven: Windows notifies us when the foreground window changes (no constant polling).
    /// A slow safety timer only covers what has no event (a game opening or closing in "always" mode).
    /// </summary>
    internal sealed class VibranceEngine : IDisposable
    {
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000, WINEVENT_SKIPOWNPROCESS = 0x0002;
        private const int FocusedModeInterval = 5000;  // Safety net only; foreground changes arrive as events.
        private const int AlwaysModeInterval = 2000;   // Detects games starting/stopping in the background.

        private readonly AppSettings _settings;
        private readonly GpuManager _gpu;
        private readonly Timer _timer = new Timer();
        private readonly WinEventProc _onForeground; // Kept in a field so the GC never frees the callback.
        private IntPtr _hook;
        private int? _applied;

        // Last foreground process, so repeated checks of the same window cost nothing.
        private uint _lastPid;
        private string _lastName;

        public event EventHandler StateChanged;

        /// <summary>The game currently driving the vibrance, or null when on the desktop.</summary>
        public GameEntry ActiveGame { get; private set; }

        public int AppliedVibrance => _applied ?? _settings.DesktopVibrance;

        public VibranceEngine(AppSettings settings, GpuManager gpu)
        {
            _settings = settings;
            _gpu = gpu;
            _timer.Tick += (s, e) => Update();
            _onForeground = (hook, ev, hwnd, idObject, idChild, thread, time) => Update();
        }

        public void Start()
        {
            // Out-of-context hook: callbacks arrive on this (UI) thread through its message loop.
            _hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                _onForeground, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            if (_hook == IntPtr.Zero) Log.Write("Foreground hook unavailable, using the timer only.");
            UpdateTimer();
            Update();
        }

        /// <summary>Re-evaluate now and re-send the value even if it did not change.</summary>
        public void Refresh()
        {
            _applied = null;
            UpdateTimer();
            Update();
        }

        /// <summary>Put the desktop level back (used on exit).</summary>
        public void Restore()
        {
            _timer.Stop();
            Unhook();
            _gpu.SetVibrance(_settings.DesktopVibrance);
            _applied = _settings.DesktopVibrance;
        }

        private void UpdateTimer()
        {
            bool needed = _settings.Enabled && _settings.Games.Any(g => g.Enabled);
            _timer.Interval = _settings.OnlyWhenFocused && _hook != IntPtr.Zero ? FocusedModeInterval : AlwaysModeInterval;
            _timer.Enabled = needed;
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
            Process[] processes = Process.GetProcesses();
            try
            {
                var running = processes.Select(p => p.ProcessName).ToLookup(n => n, StringComparer.OrdinalIgnoreCase);
                return _settings.Games.FirstOrDefault(g => g.Enabled && running.Contains(g.ProcessName));
            }
            finally
            {
                foreach (Process p in processes) p.Dispose();
            }
        }

        private string ForegroundProcessName()
        {
            IntPtr window = GetForegroundWindow();
            if (window == IntPtr.Zero) return null;
            GetWindowThreadProcessId(window, out uint pid);
            if (pid == 0) return null;
            if (pid == _lastPid) return _lastName;

            _lastPid = pid;
            _lastName = ProcessNameOf(pid);
            return _lastName;
        }

        /// <summary>Asks Windows for one process's image name (cheaper than a full process snapshot).</summary>
        private static string ProcessNameOf(uint pid)
        {
            IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (handle != IntPtr.Zero)
            {
                try
                {
                    var path = new StringBuilder(1024);
                    int size = path.Capacity;
                    if (QueryFullProcessImageName(handle, 0, path, ref size))
                        return Path.GetFileNameWithoutExtension(path.ToString());
                }
                finally
                {
                    CloseHandle(handle);
                }
            }

            // Fallback for the rare process we may not open.
            try { using (Process p = Process.GetProcessById((int)pid)) return p.ProcessName; }
            catch { return null; }
        }

        private void Unhook()
        {
            if (_hook == IntPtr.Zero) return;
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }

        public void Dispose()
        {
            Unhook();
            _timer.Dispose();
        }

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        private delegate void WinEventProc(IntPtr hook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);

        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")] private static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventProc callback, uint pid, uint thread, uint flags);
        [DllImport("user32.dll")] private static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryFullProcessImageName(IntPtr process, int flags, StringBuilder name, ref int size);
        [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    }
}
