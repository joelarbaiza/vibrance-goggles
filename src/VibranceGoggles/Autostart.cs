using System.Windows.Forms;
using Microsoft.Win32;

namespace VibranceGoggles
{
    /// <summary>"Start with Windows" through the current user's Run key (no admin rights needed).</summary>
    internal static class Autostart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "VibranceGoggles";
        public const string MinimizedArg = "--minimized";

        public static void Apply(bool enabled)
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
