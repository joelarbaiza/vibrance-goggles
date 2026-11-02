using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using VibranceGoggles.Gpu;
using VibranceGoggles.UI;

namespace VibranceGoggles
{
    internal static class Program
    {
        private const string MutexName = @"Local\VibranceGoggles.SingleInstance";
        private const string ShowEventName = @"Local\VibranceGoggles.Show";

        [STAThread]
        private static void Main(string[] args)
        {
            bool minimized = args.Any(a => string.Equals(a, Autostart.MinimizedArg, StringComparison.OrdinalIgnoreCase));

            using (var mutex = new Mutex(true, MutexName, out bool firstInstance))
            using (var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName))
            {
                if (!firstInstance)
                {
                    // Already running: ask the existing window to come forward, then quit.
                    if (!minimized) showEvent.Set();
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += (s, e) => Log.Write("UI error: " + e.Exception);

                AppSettings settings = AppSettings.Load();
                var gpu = new GpuManager();

                using (var form = new MainForm(settings, gpu))
                {
                    ThreadPool.RegisterWaitForSingleObject(showEvent,
                        (state, timedOut) => form.BeginInvoke(new Action(form.ShowCentered)),
                        null, Timeout.Infinite, false);

                    // With --minimized (Windows startup) it just stays in the tray.
                    if (!minimized) form.ShowCentered();

                    Application.Run();
                }

                GC.KeepAlive(mutex);
            }
        }
    }
}
