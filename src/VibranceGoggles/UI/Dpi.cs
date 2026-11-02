using System;
using System.Drawing;

namespace VibranceGoggles.UI
{
    /// <summary>Converts 96-DPI design sizes to the screen's scaling (125 %, 150 %, ...).</summary>
    internal static class Dpi
    {
        private static readonly float Factor = ReadFactor();

        public static int Px(int logical) => (int)Math.Round(logical * Factor);

        private static float ReadFactor()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) return g.DpiX / 96f;
        }
    }
}
