using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace VibranceGoggles.UI
{
    /// <summary>
    /// Window and tray icons in a dark and a light variant, embedded in the .exe (no external .ico files).
    /// Each variant is loaded once and reused; call <see cref="DisposeAll"/> when the app exits.
    /// The .exe / installer / shortcut icon stays fixed (assets/icon.ico): Windows caches it.
    /// </summary>
    internal static class AppIcons
    {
        private const string DarkResource = "VibranceGoggles.Icons.Dark.ico";
        private const string LightResource = "VibranceGoggles.Icons.Light.ico";

        private static Icon _dark, _light, _fallback;

        /// <summary>Icon matching the current Windows app mode (see <see cref="Theme.IsDark"/>).</summary>
        public static Icon Current => Theme.IsDark ? Dark : Light;

        private static Icon Dark => _dark ?? (_dark = Load(DarkResource));
        private static Icon Light => _light ?? (_light = Load(LightResource));

        private static Icon Load(string resource)
        {
            try
            {
                using (Stream stream = typeof(AppIcons).Assembly.GetManifestResourceStream(resource))
                {
                    if (stream == null) throw new FileNotFoundException("Embedded resource not found", resource);
                    // Icon(Stream, Size) picks the best image for the system small/large icon sizes.
                    return new Icon(stream);
                }
            }
            catch (Exception ex)
            {
                Log.Write($"Icon '{resource}' could not be loaded, using the .exe icon: {ex.Message}");
                return Fallback;
            }
        }

        private static Icon Fallback => _fallback ?? (_fallback = Icon.ExtractAssociatedIcon(Application.ExecutablePath));

        public static void DisposeAll()
        {
            foreach (Icon icon in new[] { _dark, _light, _fallback })
                icon?.Dispose();
            _dark = _light = _fallback = null;
        }
    }
}
