using System;
using System.Diagnostics;
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
            bool restart = false;

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
                S.Validate();
                S.Init(settings.Language);
                var gpu = new GpuManager();

                using (var form = new MainForm(settings, gpu))
                {
                    ThreadPool.RegisterWaitForSingleObject(showEvent,
                        (state, timedOut) => form.BeginInvoke(new Action(form.ShowCentered)),
                        null, Timeout.Infinite, false);

                    // With --minimized (Windows startup) it just stays in the tray.
                    if (!minimized) form.ShowCentered();
                    else MemoryTrim.Soon(3000); // Started with Windows: stay small in the tray.

                    Application.Run();
                    restart = form.RestartRequested;
                }

                AppIcons.DisposeAll();
                mutex.ReleaseMutex();
            }

            // Started only after the mutex is released, so the new copy becomes the main instance.
            if (restart) Process.Start(Application.ExecutablePath);
        }
    }
}
