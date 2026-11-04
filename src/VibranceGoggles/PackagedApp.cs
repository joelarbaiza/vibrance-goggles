using System;
using System.Runtime.InteropServices;
using System.Text;

namespace VibranceGoggles
{
    /// <summary>
    /// Tells whether the app runs from the Microsoft Store (MSIX) package or as a classic install
    /// (.msi / portable .exe). Some features, like starting with Windows, work differently in each.
    /// </summary>
    internal static class PackagedApp
    {
        private const int APPMODEL_ERROR_NO_PACKAGE = 15700;

        /// <summary>The startup task id declared in packaging/msix/AppxManifest.xml.</summary>
        public const string StartupTaskId = "VibranceGogglesStartup";

        public static bool IsPackaged { get; } = DetectPackage();

        private static bool DetectPackage()
        {
            try
            {
                int length = 0;
                return GetCurrentPackageFullName(ref length, null) != APPMODEL_ERROR_NO_PACKAGE;
            }
            catch (EntryPointNotFoundException)
            {
                return false; // Very old Windows: never packaged.
            }
        }

        /// <summary>True when Windows started the packaged app at sign-in (startup task).</summary>
        public static bool LaunchedAtStartup()
        {
            if (!IsPackaged) return false;
            try
            {
                var args = Windows.ApplicationModel.AppInstance.GetActivatedEventArgs();
                return args != null && args.Kind == Windows.ApplicationModel.Activation.ActivationKind.StartupTask;
            }
            catch (Exception ex)
            {
                Log.Write("Could not read the activation kind: " + ex.Message);
                return false;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetCurrentPackageFullName(ref int length, StringBuilder name);
    }
}
