using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VibranceGoggles
{
    /// <summary>
    /// While the app sits hidden in the tray it needs almost no memory. After hiding, this frees
    /// unused .NET memory and returns the idle pages to Windows (they come back on demand, quickly,
    /// when the window is opened again).
    /// </summary>
    internal static class MemoryTrim
    {
        private static Timer _pending;

        /// <summary>Trims a moment later, after the UI has finished hiding.</summary>
        public static void Soon(int delayMs = 1500)
        {
            _pending?.Dispose();
            _pending = new Timer { Interval = delayMs };
            _pending.Tick += (s, e) =>
            {
                _pending.Dispose();
                _pending = null;
                Now();
            };
            _pending.Start();
        }

        public static void Now()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                using (Process self = Process.GetCurrentProcess())
                    SetProcessWorkingSetSize(self.Handle, (IntPtr)(-1), (IntPtr)(-1));
            }
            catch (Exception ex)
            {
                Log.Write("Memory trim skipped: " + ex.Message);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr min, IntPtr max);
    }
}
