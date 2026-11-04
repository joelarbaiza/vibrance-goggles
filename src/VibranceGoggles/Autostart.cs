using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Windows.ApplicationModel;

namespace VibranceGoggles
{
    /// <summary>
    /// "Start with Windows".
    /// Classic install (.msi / portable): the current user's Run key (no admin rights needed).
    /// Microsoft Store (MSIX): the package's startup task, which Windows also lists in
    /// Settings → Apps → Startup, where the user can turn it off.
    /// </summary>
    internal static class Autostart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "VibranceGoggles";
        public const string MinimizedArg = "--minimized";

        /// <summary>Turns autostart on or off; returns whether it is on afterwards.</summary>
        public static async Task<bool> SetAsync(bool enabled)
        {
            if (!PackagedApp.IsPackaged)
            {
                SetRunKey(enabled);
                return enabled;
            }

            StartupTask task = await StartupTask.GetAsync(PackagedApp.StartupTaskId);
            if (!enabled)
            {
                task.Disable();
                return false;
            }
            StartupTaskState state = await task.RequestEnableAsync();
            return IsOn(state);
        }

        /// <summary>Current state as Windows sees it (the user may change it in Settings), or null if unknown.</summary>
        public static async Task<bool?> IsEnabledAsync()
        {
            if (!PackagedApp.IsPackaged) return null; // The Run key is ours; settings.xml is the source of truth.
            try
            {
                StartupTask task = await StartupTask.GetAsync(PackagedApp.StartupTaskId);
                return IsOn(task.State);
            }
            catch (Exception ex)
            {
                Log.Write("Startup task state unavailable: " + ex.Message);
                return null;
            }
        }

        private static bool IsOn(StartupTaskState state) =>
            state == StartupTaskState.Enabled || state == StartupTaskState.EnabledByPolicy;

        private static void SetRunKey(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true) ?? Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (enabled)
                    key.SetValue(ValueName, $"\"{Application.ExecutablePath}\" {MinimizedArg}");
                else
                    key.DeleteValue(ValueName, false);
            }
        }
    }
}
